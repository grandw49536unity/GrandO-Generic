using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace GrandO.Generic.PathFinding {

    public class AStarPathGraph2D {
        // Public storage (immutable externally after ctor / Refresh)
        public float2[] nodes;
        public PathSegment[] pathSegments;

        // Results (kept as Lists to match your API — preallocated to avoid allocations during pathfinding)
        public List<int> resultNodeIndices { get; private set; }
        public List<int> resultPathSegmentIndices { get; private set; }
        public List<float2> resultPositions { get; private set; }

        // Adjacency representation (rebuilt in Refresh)
        // adjacencyOffsets: length = nodes.Length + 1
        // adjacencySegmentIndices: contiguous list of segment indices for outgoing segments
        int[] adjacencyOffsets;
        int[] adjacencySegmentIndices;

        // Working arrays (allocated once in ctor; reused without allocation during pathfinding)
        readonly int nodeCount;
        readonly int segmentCount;
        int[] cameFromNode; // parent node for reconstruct
        int[] cameFromSegment; // path-segment index used to reach node
        float[] gScore;
        float[] fScore;
        int[] heap; // binary heap storing node indices (open set)
        int heapCount;
        int[] heapIndex; // maps node -> index in heap or -1 if not in heap
        int[] visitVersion; // versioning to avoid clearing arrays every run
        int currentVisitVersion;

        // closed set via versioning (we can use visitVersion to mark visited nodes)
        const int INITIAL_VERSION = 1;

        public AStarPathGraph2D(float2[] _nodes, PathSegment[] _pathSegments) {
            if (_nodes == null) throw new ArgumentNullException(nameof(_nodes));
            if (_pathSegments == null) throw new ArgumentNullException(nameof(_pathSegments));

            nodes = _nodes;
            pathSegments = _pathSegments;

            nodeCount = nodes.Length;
            segmentCount = pathSegments.Length;

            // Results: reserve capacity to nodeCount to avoid growth allocations during pathfinding
            resultNodeIndices = new List<int>(math.max(16, nodeCount));
            resultPathSegmentIndices = new List<int>(math.max(16, nodeCount));
            resultPositions = new List<float2>(math.max(16, nodeCount));

            // Allocate working arrays (single-time allocations during initialization)
            cameFromNode = new int[nodeCount];
            cameFromSegment = new int[nodeCount];
            gScore = new float[nodeCount];
            fScore = new float[nodeCount];
            heap = new int[nodeCount]; // heap worst-case size = nodeCount
            heapIndex = new int[nodeCount];
            visitVersion = new int[nodeCount];

            // initialize visit versioning and heapIndex to -1
            for (int i = 0; i < nodeCount; i++) {
                visitVersion[i] = 0;
                heapIndex[i] = -1;
                cameFromNode[i] = -1;
                cameFromSegment[i] = -1;
                gScore[i] = float.MaxValue;
                fScore[i] = float.MaxValue;
            }

            currentVisitVersion = INITIAL_VERSION;

            // Build adjacency structures initially
            Refresh();
        }

        /// <summary>
        /// Rebuild adjacency arrays based on current pathSegments' isBlocked flags.
        /// This function may allocate (allowed). After this is called, pathfinding remains GC-free.
        /// </summary>
        public void Refresh() {
            // Count outgoing segments per node (for non-blocked segments)
            int[] outCount = new int[nodeCount];
            int total = 0;
            for (int i = 0; i < segmentCount; i++) {
                var s = pathSegments[i];
                if (s.isBlocked) continue;
                if (s.startIndex < 0 || s.startIndex >= nodeCount) continue;
                // valid one-way seg
                outCount[s.startIndex]++;
                total++;
            }

            adjacencyOffsets = new int[nodeCount + 1];
            adjacencySegmentIndices = new int[total];

            // prefix sums to compute offsets
            int acc = 0;
            for (int i = 0; i < nodeCount; i++) {
                adjacencyOffsets[i] = acc;
                acc += outCount[i];
            }
            adjacencyOffsets[nodeCount] = acc;

            // reuse outCount as a cursor to fill adjacencySegmentIndices
            for (int i = 0; i < nodeCount; i++) outCount[i] = 0;

            for (int i = 0; i < segmentCount; i++) {
                var s = pathSegments[i];
                if (s.isBlocked) continue;
                int start = s.startIndex;
                if (start < 0 || start >= nodeCount) continue;
                int writePos = adjacencyOffsets[start] + outCount[start];
                adjacencySegmentIndices[writePos] = i;
                outCount[start]++;
            }

            // Note: Refresh allocated adjacency arrays but pathfinding will reuse them without allocation.
        }

        // ---------- GC-FREE A* PATHFINDING OPERATION ----------
        // This method is GC-free: it does not create ANY managed objects or call Any methods that allocate.
        // Caller must ensure startNodeIndex and destinationNodeIndex are valid.
        public void CalculatePathFindingSequence(int startNodeIndex, int destinationNodeIndex) {
            // Clear result lists (no capacity shrink)
            resultNodeIndices.Clear();
            resultPathSegmentIndices.Clear();

            if (startNodeIndex < 0 || destinationNodeIndex < 0 || startNodeIndex >= nodeCount || destinationNodeIndex >= nodeCount) {
                return;
            }

            // If same node
            if (startNodeIndex == destinationNodeIndex) {
                resultNodeIndices.Add(startNodeIndex);
                // no segments
                return;
            }

            // bump visit version (overflow handling)
            currentVisitVersion++;
            if (currentVisitVersion == 0) {
                // wrap-around guard: reset visitVersion array (allowed, small cost)
                for (int i = 0; i < nodeCount; i++) visitVersion[i] = 0;
                currentVisitVersion = INITIAL_VERSION + 1;
            }

            // heap reset
            heapCount = 0;

            // Initialize start node
            gScore[startNodeIndex] = 0f;
            float hStart = math.distance(nodes[startNodeIndex], nodes[destinationNodeIndex]);
            fScore[startNodeIndex] = hStart;

            // mark visited (we use visitVersion to mark nodes we've touched this run)
            visitVersion[startNodeIndex] = currentVisitVersion;
            cameFromNode[startNodeIndex] = -1;
            cameFromSegment[startNodeIndex] = -1;

            // push start into open set
            PushHeap(startNodeIndex);

            bool found = false;

            // Main A* loop
            while (heapCount > 0) {
                int current = PopHeap(); // node index with smallest fScore

                // If current is destination: done
                if (current == destinationNodeIndex) {
                    found = true;
                    break;
                }

                // Iterate outgoing segments of current
                int startOff = adjacencyOffsets[current];
                int endOff = adjacencyOffsets[current + 1];
                for (int ai = startOff; ai < endOff; ai++) {
                    int segIndex = adjacencySegmentIndices[ai];
                    var seg = pathSegments[segIndex];

                    // segment may be blocked after Refresh? We built adjacency from non-blocked segments; still safe
                    int neighbor = seg.destinationIndex;
                    if (neighbor < 0 || neighbor >= nodeCount) continue;

                    // tentative g score
                    float tentativeG = gScore[current] + seg.cost;

                    // if neighbor hasn't been seen this run, initialize
                    if (visitVersion[neighbor] != currentVisitVersion) {
                        visitVersion[neighbor] = currentVisitVersion;
                        gScore[neighbor] = float.MaxValue;
                        fScore[neighbor] = float.MaxValue;
                        cameFromNode[neighbor] = -1;
                        cameFromSegment[neighbor] = -1;
                        heapIndex[neighbor] = -1; // ensure not in heap
                    }

                    if (tentativeG < gScore[neighbor]) {
                        cameFromNode[neighbor] = current;
                        cameFromSegment[neighbor] = segIndex;
                        gScore[neighbor] = tentativeG;
                        float h = math.distance(nodes[neighbor], nodes[destinationNodeIndex]);
                        fScore[neighbor] = tentativeG + h;

                        if (heapIndex[neighbor] == -1) {
                            // not in open set -> push
                            PushHeap(neighbor);
                        } else {
                            // in open set -> decrease-key: update position in heap
                            HeapifyUp(heapIndex[neighbor]);
                        }
                    }
                }
            }

            // Reconstruct path (if found)
            if (!found) {
                // no path
                return;
            }

            // Reconstruct by walking from destination to start.
            // We'll push into result lists then reverse (List.Reverse is in-place)
            int cursor = destinationNodeIndex;
            while (cursor != -1) {
                // Only nodes that were visited this run should have valid cameFrom
                resultNodeIndices.Add(cursor);
                int seg = cameFromSegment[cursor];
                if (seg >= 0) resultPathSegmentIndices.Add(seg);
                cursor = cameFromNode[cursor];
            }

            // currently sequences are reversed (dest -> start) so reverse to be start->dest
            resultNodeIndices.Reverse();
            resultPathSegmentIndices.Reverse();

            // If the first element in resultPathSegmentIndices corresponds to the edge from start -> next,
            // it's correct. resultPathSegmentIndices.Count == resultNodeIndices.Count - 1
        }

        // ---------- helpers: binary min-heap using fScore as priority ----------
        void PushHeap(int node) {
            int idx = heapCount++;
            heap[idx] = node;
            heapIndex[node] = idx;
            HeapifyUp(idx);
        }

        int PopHeap() {
            int top = heap[0];
            heapCount--;
            if (heapCount > 0) {
                heap[0] = heap[heapCount];
                heapIndex[heap[0]] = 0;
                HeapifyDown(0);
            }
            heapIndex[top] = -1;
            return top;
        }

        void SwapHeapNodes(int i, int j) {
            int ni = heap[i];
            int nj = heap[j];
            heap[i] = nj;
            heap[j] = ni;
            heapIndex[ni] = j;
            heapIndex[nj] = i;
        }

        void HeapifyUp(int i) {
            while (i > 0) {
                int parent = (i - 1) >> 1;
                if (CompareHeapNodes(i, parent) < 0) {
                    SwapHeapNodes(i, parent);
                    i = parent;
                } else
                    break;
            }
        }

        void HeapifyDown(int i) {
            while (true) {
                int left = (i << 1) + 1;
                int right = left + 1;
                int smallest = i;
                if (left < heapCount && CompareHeapNodes(left, smallest) < 0) smallest = left;
                if (right < heapCount && CompareHeapNodes(right, smallest) < 0) smallest = right;
                if (smallest != i) {
                    SwapHeapNodes(i, smallest);
                    i = smallest;
                } else
                    break;
            }
        }

        // compare by fScore, tie-break by gScore to prefer larger g (closer to goal) or smaller g depending on strategy
        int CompareHeapNodes(int aIndex, int bIndex) {
            int aNode = heap[aIndex];
            int bNode = heap[bIndex];
            float fa = fScore[aNode];
            float fb = fScore[bNode];
            if (fa < fb) return -1;
            if (fa > fb) return 1;
            // tie-breaker: smaller gScore preferred
            float ga = gScore[aNode];
            float gb = gScore[bNode];
            if (ga < gb) return -1;
            if (ga > gb) return 1;
            return 0;
        }

        /// <summary>
        /// PathFinding using world positions. Returns list of positions (start projection -> node positions -> destination projection).
        /// Uses FindNearestPointOnGraph to snap to graph; uses CalculateNodeSequence to find node path (GC-free).
        /// </summary>
        public List<float2> PathFinding(float2 startPosition, float2 destinationPosition) {
            resultPositions.Clear();

            float2 firstPosition = FindNearestPointOnGraph(startPosition, out int firstNodeIndex0, out int firstNodeIndex1, out float firstInterpolation);
            resultPositions.Add(firstPosition);

            float2 lastPosition = FindNearestPointOnGraph(destinationPosition, out int lastNodeIndex0, out int lastNodeIndex1, out float lastInterpolation);

            // If both projected to same segment (either orientation) -> straight segment
            if ((firstNodeIndex0 == lastNodeIndex0 && firstNodeIndex1 == lastNodeIndex1) ||
                (firstNodeIndex0 == lastNodeIndex1 && firstNodeIndex1 == lastNodeIndex0)) {
                resultPositions.Add(lastPosition);
                return resultPositions;
            }

            // Choose nearer node ends depending on interpolation (so we attach to closer endpoint)
            // If projection is closer to destination node of segment, swap to treat as from that node
            if (firstInterpolation > 0.5f) (firstNodeIndex0, firstNodeIndex1) = (firstNodeIndex1, firstNodeIndex0);
            if (lastInterpolation > 0.5f) (lastNodeIndex0, lastNodeIndex1) = (lastNodeIndex1, lastNodeIndex0);

            // If first and last share the same nearest node, simply go through that node
            if (firstNodeIndex0 == lastNodeIndex0) {
                resultPositions.Add(nodes[firstNodeIndex0]);
                resultPositions.Add(lastPosition);
                return resultPositions;
            }

            // Find node sequence between firstNodeIndex0 and lastNodeIndex0
            CalculatePathFindingSequence(firstNodeIndex0, lastNodeIndex0);
            int pathLength = resultNodeIndices.Count;
            if (pathLength == 0) {
                // unreachable
                resultPositions.Add(lastPosition);
                return resultPositions;
            }

            // Determine whether to remove first/last nodes if they are duplicate endpoints of the projected segments
            bool removeFirst = pathLength >= 2 && resultNodeIndices[1] == firstNodeIndex1;
            bool removeLast = pathLength >= 2 && resultNodeIndices[^2] == lastNodeIndex1;

            int firstDelta = removeFirst ? 1 : 0;
            int lastDelta = removeLast ? 1 : 0;
            int resultNodeCount = pathLength + 2 - firstDelta - lastDelta;

            for (int i = firstDelta; i < pathLength - lastDelta; i++) {
                resultPositions.Add(nodes[resultNodeIndices[i]]);
            }
            resultPositions.Add(lastPosition);
            return resultPositions;
        }

        /// <summary>
        /// Find nearest point on the graph line segments (unblocked only).
        /// Return: nearestPoint, and out: nodeIndex0, nodeIndex1, interpolation [0..1] from nodeIndex0->nodeIndex1
        /// This method is allocation-free.
        /// </summary>
        public float2 FindNearestPointOnGraph(float2 point, out int nodeIndex0, out int nodeIndex1, out float interpolation) {
            float minDistSq = float.MaxValue;
            float2 nearestPoint = default;
            nodeIndex0 = nodeIndex1 = -1;
            interpolation = 0;
            for (int i = 0; i < pathSegments.Length; i++) {
                var seg = pathSegments[i];
                if (seg.isBlocked) continue;
                float2 a = nodes[seg.startIndex];
                float2 b = nodes[seg.destinationIndex];
                float2 ab = b - a;
                float2 ap = point - a;
                float denom = math.dot(ab, ab);
                if (denom <= 1e-6f) {
                    // degenerate segment: treat as point a
                    float distSqPoint = math.lengthsq(point - a);
                    if (distSqPoint < minDistSq) {
                        minDistSq = distSqPoint;
                        nearestPoint = a;
                        nodeIndex0 = seg.startIndex;
                        nodeIndex1 = seg.destinationIndex;
                        interpolation = 0f;
                    }
                    continue;
                }
                float t = math.dot(ap, ab) / denom;
                t = math.clamp(t, 0f, 1f);
                float2 proj = a + t * ab;
                float distSq = math.lengthsq(point - proj);
                if (distSq < minDistSq) {
                    minDistSq = distSq;
                    nearestPoint = proj;
                    nodeIndex0 = seg.startIndex;
                    nodeIndex1 = seg.destinationIndex;
                    interpolation = t;
                }
            }
            return nearestPoint;
        }

    }
    
}

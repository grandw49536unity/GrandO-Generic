using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace GrandO.Generic.PathFinding {

    public class AStarPathGraph {
        // Public data you provided
        public float3[] nodes;
        public PathSegment[] pathSegments;

        // Preallocated results to avoid allocations at runtime
        public List<int> resultIndices { get; private set; }
        public List<float3> resultPositions { get; private set; }

        // --- Internal adjacency representation (built in Refresh)
        // adjacencySegmentIndices stores pathSegment indices; for node i
        // outgoing path segments are in adjacencySegmentIndices[nodeStartIndices[i] .. nodeStartIndices[i+1]-1]
        private int[] adjacencySegmentIndices;
        private int[] nodeStartIndices; // length = nodeCount + 1

        // --- Per-node arrays used by A* (preallocated)
        private float[] gScore;
        private float[] fScore;
        private int[] parent;           // parent node index in path
        private int[] heapIndex;        // position in heap (-1 if not in heap)
        private byte[] closedFlags;     // 0 = open/untouched, 1 = closed

        // Binary min-heap of node indices ordered by fScore
        private int[] openHeap;         // contains node indices
        private int openHeapSize;

        // Scratch arrays for reconstructing segment-index path
        // (no allocation during runtime; sized at construction)
        private int nodeCount;
        private int segmentCount;

        // Constructor: preallocates arrays used during pathfinding
        public AStarPathGraph(float3[] _nodes, PathSegment[] _pathSegments) {
            nodes = _nodes ?? throw new ArgumentNullException(nameof(_nodes));
            pathSegments = _pathSegments ?? throw new ArgumentNullException(nameof(_pathSegments));
            nodeCount = nodes.Length;
            segmentCount = pathSegments.Length;

            // Preallocate results with a reasonable default capacity (will grow only if user forces)
            resultIndices = new List<int>(64);
            resultPositions = new List<float3>(64);

            // Preallocate per-node arrays
            gScore = new float[nodeCount];
            fScore = new float[nodeCount];
            parent = new int[nodeCount];
            heapIndex = new int[nodeCount];
            closedFlags = new byte[nodeCount];

            // Initialize heap arrays
            openHeap = new int[nodeCount];
            openHeapSize = 0;

            // Initialize node counts etc. adjacency will be built on Refresh()
            adjacencySegmentIndices = Array.Empty<int>();
            nodeStartIndices = new int[nodeCount + 1];

            // initialize default sentinel values
            for (int i = 0; i < nodeCount; i++) {
                parent[i] = -1;
                heapIndex[i] = -1;
                closedFlags[i] = 0;
                gScore[i] = float.MaxValue;
                fScore[i] = float.MaxValue;
            }

            // Build adjacency representation initially
            Refresh();
        }

        /// <summary>
        /// Call after you change any PathSegment.isBlocked via SetBlocked.
        /// This will rebuild the adjacency lists. It is allowed to allocate here.
        /// </summary>
        public void Refresh() {
            // Count outgoing segments per node (only non-blocked segments included)
            int[] counts = new int[nodeCount];
            for (int i = 0; i < segmentCount; i++) {
                var seg = pathSegments[i];
                if (seg.isBlocked) continue;
                if (seg.startIndex < 0 || seg.startIndex >= nodeCount) continue;
                counts[seg.startIndex]++;
            }

            // Build nodeStartIndices via prefix-sum
            nodeStartIndices = new int[nodeCount + 1];
            int running = 0;
            for (int i = 0; i < nodeCount; i++) {
                nodeStartIndices[i] = running;
                running += counts[i];
            }
            nodeStartIndices[nodeCount] = running;

            // Allocate adjacencySegmentIndices with exact size
            adjacencySegmentIndices = new int[running];

            // Temp write cursor (reuse counts array for cursor)
            for (int i = 0; i < nodeCount; i++) counts[i] = 0;

            // Fill adjacencySegmentIndices with path segment indices
            for (int i = 0; i < segmentCount; i++) {
                var seg = pathSegments[i];
                if (seg.isBlocked) continue;
                int s = seg.startIndex;
                if (s < 0 || s >= nodeCount) continue;
                int writePos = nodeStartIndices[s] + counts[s];
                adjacencySegmentIndices[writePos] = i; // store segment index
                counts[s]++;
            }

            // Clear per-node search arrays (safe to do once here)
            for (int i = 0; i < nodeCount; i++) {
                parent[i] = -1;
                heapIndex[i] = -1;
                closedFlags[i] = 0;
                gScore[i] = float.MaxValue;
                fScore[i] = float.MaxValue;
            }
            openHeapSize = 0;
        }

        // ---------- Heap helpers (min-heap by fScore) ----------
        private void HeapPush(int nodeIdx) {
            int pos = openHeapSize;
            openHeap[openHeapSize++] = nodeIdx;
            heapIndex[nodeIdx] = pos;
            SiftUp(pos);
        }

        private int HeapPop() {
            if (openHeapSize == 0) return -1;
            int ret = openHeap[0];
            openHeapSize--;
            if (openHeapSize > 0) {
                openHeap[0] = openHeap[openHeapSize];
                heapIndex[openHeap[0]] = 0;
                SiftDown(0);
            }
            heapIndex[ret] = -1;
            return ret;
        }

        private void SiftUp(int pos) {
            int node = openHeap[pos];
            float nodeF = fScore[node];
            while (pos > 0) {
                int parentPos = (pos - 1) >> 1;
                int parentNode = openHeap[parentPos];
                if (fScore[parentNode] <= nodeF) break;
                // swap
                openHeap[pos] = parentNode;
                heapIndex[parentNode] = pos;
                pos = parentPos;
            }
            openHeap[pos] = node;
            heapIndex[node] = pos;
        }

        private void SiftDown(int pos) {
            int node = openHeap[pos];
            float nodeF = fScore[node];
            int half = openHeapSize >> 1;
            while (pos < half) {
                int left = (pos << 1) + 1;
                int right = left + 1;
                int smallest = left;
                int smallestNode = openHeap[left];
                if (right < openHeapSize) {
                    int rightNode = openHeap[right];
                    if (fScore[rightNode] < fScore[smallestNode]) {
                        smallest = right;
                        smallestNode = rightNode;
                    }
                }
                if (fScore[smallestNode] >= nodeF) break;
                openHeap[pos] = smallestNode;
                heapIndex[smallestNode] = pos;
                pos = smallest;
            }
            openHeap[pos] = node;
            heapIndex[node] = pos;
        }

        // ---------- A* (node indices) ----------
        /// <summary>
        /// Calculates node index sequence from startNodeIndex to destinationNodeIndex.
        /// Result stored in resultIndices (cleared and reused) and returned.
        /// This method is GC-free if resultIndices capacity is sufficient.
        /// </summary>
        public List<int> CalculateNodeSequence(int startNodeIndex, int destinationNodeIndex) {
            resultIndices.Clear();

            if (startNodeIndex < 0 || startNodeIndex >= nodeCount) return resultIndices;
            if (destinationNodeIndex < 0 || destinationNodeIndex >= nodeCount) return resultIndices;
            if (startNodeIndex == destinationNodeIndex) {
                resultIndices.Add(startNodeIndex);
                return resultIndices;
            }

            // Reset search state arrays for nodes touched
            // To keep GC-free we reset whole arrays (nodeCount is small 100-300)
            for (int i = 0; i < nodeCount; i++) {
                parent[i] = -1;
                heapIndex[i] = -1;
                closedFlags[i] = 0;
                gScore[i] = float.MaxValue;
                fScore[i] = float.MaxValue;
            }
            openHeapSize = 0;

            // initialize start
            gScore[startNodeIndex] = 0f;
            float h0 = Heuristic(nodes[startNodeIndex], nodes[destinationNodeIndex]);
            fScore[startNodeIndex] = h0;
            HeapPush(startNodeIndex);

            bool found = false;

            while (openHeapSize > 0) {
                int current = HeapPop();
                if (current == destinationNodeIndex) {
                    found = true;
                    break;
                }
                closedFlags[current] = 1;

                // For each outgoing path segment from current
                int start = nodeStartIndices[current];
                int end = nodeStartIndices[current + 1];
                for (int ai = start; ai < end; ai++) {
                    int segIdx = adjacencySegmentIndices[ai];
                    var seg = pathSegments[segIdx];
                    // seg.startIndex should equal current; if inconsistent, skip
                    int neighbor = seg.destinationIndex;
                    if (neighbor < 0 || neighbor >= nodeCount) continue;
                    if (closedFlags[neighbor] != 0) continue; // already evaluated

                    float tentativeG = gScore[current] + seg.cost;
                    if (tentativeG < gScore[neighbor]) {
                        parent[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        float h = Heuristic(nodes[neighbor], nodes[destinationNodeIndex]);
                        fScore[neighbor] = tentativeG + h;

                        int idxInHeap = heapIndex[neighbor];
                        if (idxInHeap == -1) {
                            HeapPush(neighbor);
                        } else {
                            // update position (sift up or down as needed)
                            int pos = idxInHeap;
                            // try sift up first (common)
                            SiftUp(pos);
                            SiftDown(pos);
                        }
                    }
                }
            }

            if (!found) {
                // unreachable -> return empty list
                return resultIndices;
            }

            // reconstruct path from destination to start
            int cur = destinationNodeIndex;
            // push into resultIndices reversed
            while (cur != -1) {
                resultIndices.Add(cur);
                if (cur == startNodeIndex) break;
                cur = parent[cur];
            }
            // currently reversed (destination..start), reverse in-place
            int li = 0;
            int ri = resultIndices.Count - 1;
            while (li < ri) {
                int tmp = resultIndices[li];
                resultIndices[li] = resultIndices[ri];
                resultIndices[ri] = tmp;
                li++; ri--;
            }
            return resultIndices;
        }

        /// <summary>
        /// Returns path as a sequence of path segment indices (matching your spec).
        /// Uses CalculateNodeSequence internally (GC-free).
        /// </summary>
        public List<int> CalculatePathSequence(int startNodeIndex, int destinationNodeIndex) {
            // Reuse resultIndices list to get node path, then convert to segments
            // We'll store the final path segment indices in resultIndices as well (clearing first)
            List<int> nodePath = CalculateNodeSequence(startNodeIndex, destinationNodeIndex);
            // If unreachable or trivial
            if (nodePath == null || nodePath.Count < 2) {
                resultIndices.Clear(); // final result segments empty
                return resultIndices;
            }

            // Convert node path to segment indices.
            // We'll fill resultIndices with segment indices. Ensure capacity.
            resultIndices.Clear();
            for (int i = 0; i < nodePath.Count - 1; i++) {
                int a = nodePath[i];
                int b = nodePath[i + 1];

                // find the outgoing pathSegment from a to b (there should be one unblocked).
                // check adjacency for node a
                int start = nodeStartIndices[a];
                int end = nodeStartIndices[a + 1];
                int foundSeg = -1;
                for (int ai = start; ai < end; ai++) {
                    int segIdx = adjacencySegmentIndices[ai];
                    var seg = pathSegments[segIdx];
                    if (seg.destinationIndex == b && !seg.isBlocked) {
                        // Use the first matching (cost preference already enforced by A*)
                        foundSeg = segIdx;
                        break;
                    }
                }
                if (foundSeg == -1) {
                    // This should not happen (A* found the node sequence), but fail-safe:
                    resultIndices.Clear();
                    return resultIndices;
                }
                resultIndices.Add(foundSeg);
            }
            return resultIndices;
        }

        /// <summary>
        /// PathFinding using world positions. Returns list of positions (start projection -> node positions -> destination projection).
        /// Uses FindNearestPointOnGraph to snap to graph; uses CalculateNodeSequence to find node path (GC-free).
        /// </summary>
        public List<float3> PathFinding(float3 startPosition, float3 destinationPosition) {
            resultPositions.Clear();

            float3 firstPosition = FindNearestPointOnGraph(startPosition, out int firstNodeIndex0, out int firstNodeIndex1, out float firstInterpolation);
            resultPositions.Add(firstPosition);

            float3 lastPosition = FindNearestPointOnGraph(destinationPosition, out int lastNodeIndex0, out int lastNodeIndex1, out float lastInterpolation);

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
            CalculateNodeSequence(firstNodeIndex0, lastNodeIndex0);
            int pathLength = resultIndices.Count;
            if (pathLength == 0) {
                // unreachable
                resultPositions.Add(lastPosition);
                return resultPositions;
            }

            // Determine whether to remove first/last nodes if they are duplicate endpoints of the projected segments
            bool removeFirst = pathLength >= 2 && resultIndices[1] == firstNodeIndex1;
            bool removeLast = pathLength >= 2 && resultIndices[^2] == lastNodeIndex1;

            int firstDelta = removeFirst ? 1 : 0;
            int lastDelta = removeLast ? 1 : 0;
            int resultNodeCount = pathLength + 2 - firstDelta - lastDelta;

            for (int i = firstDelta; i < pathLength - lastDelta; i++) {
                resultPositions.Add(nodes[resultIndices[i]]);
            }
            resultPositions.Add(lastPosition);
            return resultPositions;
        }

        /// <summary>
        /// Find nearest point on the graph line segments (unblocked only).
        /// Return: nearestPoint, and out: nodeIndex0, nodeIndex1, interpolation [0..1] from nodeIndex0->nodeIndex1
        /// This method is allocation-free.
        /// </summary>
        public float3 FindNearestPointOnGraph(float3 point, out int nodeIndex0, out int nodeIndex1, out float interpolation) {
            float minDistSq = float.MaxValue;
            float3 nearestPoint = default;
            nodeIndex0 = nodeIndex1 = -1;
            interpolation = 0;
            for (int i = 0; i < pathSegments.Length; i++) {
                var seg = pathSegments[i];
                if (seg.isBlocked) continue;
                float3 a = nodes[seg.startIndex];
                float3 b = nodes[seg.destinationIndex];
                float3 ab = b - a;
                float3 ap = point - a;
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
                float3 proj = a + t * ab;
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

        // Simple Euclidean heuristic (admissible since segment costs are distances)
        private static float Heuristic(float3 a, float3 b) {
            return math.length(a - b);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace GrandO.Generic.PathFinding {

    public class DijkstraPathGraph {
        // Input
        public PathSegment[] pathSegments;

        // Public results (preallocated in ctor)
        public List<int> resultNodeIndices { get; private set; }
        public List<int> resultPathSegmentIndices { get; private set; }

        // Internal adjacency representation (rebuilt in Refresh)
        // For each node i, outgoing edges are stored in edgeSegmentIndices from edgeOffsets[i] (count = edgeCounts[i])
        int[] edgeOffsets;              // offset into edgeSegmentIndices
        int[] edgeCounts;               // number of outgoing edges per node
        int[] edgeSegmentIndices;       // values are indices into pathSegments[]

        int nodeCount;                  // number of nodes (max index + 1)

        // Working arrays used at runtime by CalculatePathFindingSequence
        float[] distances;
        int[] prevNode;
        int[] prevSegment;
        bool[] visited;

        // Binary heap for nodes (min-heap keyed by distances[])
        int[] heap;            // heap[1..heapSize] (1-based simplifies parent/child math)
        int[] heapPos;         // heapPos[node] = position in heap array (1..heapSize) or -1 if not in heap
        int heapSize;

        const int HEAP_BASE = 1;

        public DijkstraPathGraph(PathSegment[] _pathSegments) {
            pathSegments = _pathSegments ?? throw new ArgumentNullException(nameof(_pathSegments));
            resultNodeIndices = new List<int>(512);         // capacity large enough to avoid growth (adjust if needed)
            resultPathSegmentIndices = new List<int>(512);

            // Build initial internal graph (computes nodeCount and adjacency)
            Refresh(); // this will allocate internal arrays and working arrays
        }

        /// <summary>
        /// Rebuild adjacency lists from pathSegments, skipping segments that are currently blocked.
        /// Call this after you've finished adjusting isBlocked flags on segments.
        /// </summary>
        public void Refresh() {
            // Determine nodeCount (max index + 1)
            int maxIndex = -1;
            for (int i = 0; i < pathSegments.Length; ++i) {
                var s = pathSegments[i];
                if (s.startIndex > maxIndex) maxIndex = s.startIndex;
                if (s.destinationIndex > maxIndex) maxIndex = s.destinationIndex;
            }
            nodeCount = maxIndex + 1;
            if (nodeCount <= 0) nodeCount = 0;

            // Count outgoing edges per node (skip blocked)
            edgeCounts = new int[nodeCount];
            for (int i = 0; i < pathSegments.Length; ++i) {
                if (pathSegments[i].isBlocked) continue;
                int s = pathSegments[i].startIndex;
                if (s >= 0 && s < nodeCount) edgeCounts[s]++;
            }

            // Build offsets (prefix sums)
            edgeOffsets = new int[nodeCount];
            int totalEdges = 0;
            for (int n = 0; n < nodeCount; ++n) {
                edgeOffsets[n] = totalEdges;
                totalEdges += edgeCounts[n];
            }

            // Fill edgeSegmentIndices
            edgeSegmentIndices = new int[totalEdges];
            // We will reuse edgeCounts[] as a cursor to fill entries
            int[] cursor = new int[nodeCount];
            Array.Copy(edgeCounts, cursor, nodeCount);

            for (int n = 0; n < nodeCount; ++n) {
                // convert counts to zero-based cursor
                cursor[n] = 0;
            }

            for (int i = 0; i < pathSegments.Length; ++i) {
                if (pathSegments[i].isBlocked) continue;
                int s = pathSegments[i].startIndex;
                if (s < 0 || s >= nodeCount) continue; // defensive
                int pos = edgeOffsets[s] + cursor[s];
                edgeSegmentIndices[pos] = i; // store segment index
                cursor[s]++; // advance
            }

            // Prepare runtime arrays (allocated or re-sized)
            distances = new float[nodeCount];
            prevNode = new int[nodeCount];
            prevSegment = new int[nodeCount];
            visited = new bool[nodeCount];

            heap = new int[nodeCount + 2];    // +2 to safely use 1-based indexing
            heapPos = new int[nodeCount];
            for (int i = 0; i < nodeCount; ++i) heapPos[i] = -1;

            // Pre-size result lists to avoid growth during runtime; use nodeCount as safe upper bound
            resultNodeIndices.Capacity = Math.Max(resultNodeIndices.Capacity, nodeCount);
            resultPathSegmentIndices.Capacity = Math.Max(resultPathSegmentIndices.Capacity, nodeCount);
        }

        /// <summary>
        /// Calculate path. This method performs NO heap allocations (GC-free).
        /// After call, resultNodeIndices and resultPathSegmentIndices contain the path from start->destination (in order).
        /// If no path found, both lists will be empty.
        /// </summary>
        public void CalculatePathFindingSequence(int startNodeIndex, int destinationNodeIndex) {
            resultNodeIndices.Clear();
            resultPathSegmentIndices.Clear();

            if (nodeCount == 0) return;
            if (startNodeIndex < 0 || startNodeIndex >= nodeCount) return;
            if (destinationNodeIndex < 0 || destinationNodeIndex >= nodeCount) return;

            // Initialize arrays
            for (int i = 0; i < nodeCount; ++i) {
                distances[i] = float.PositiveInfinity;
                prevNode[i] = -1;
                prevSegment[i] = -1;
                visited[i] = false;
                heapPos[i] = -1;
            }

            heapSize = 0;

            // Setup start
            distances[startNodeIndex] = 0f;
            HeapInsert(startNodeIndex);

            bool found = false;

            while (heapSize > 0) {
                int u = HeapPopMin();
                if (visited[u]) continue; // defensive (shouldn't normally occur)
                visited[u] = true;

                if (u == destinationNodeIndex) {
                    found = true;
                    break;
                }

                // For each outgoing edge of u
                int offset = edgeOffsets[u];
                int count = edgeCounts[u];
                for (int j = 0; j < count; ++j) {
                    int segIndex = edgeSegmentIndices[offset + j];
                    // Note: because Refresh filtered blocked segments out, we don't need to check isBlocked here.
                    var seg = pathSegments[segIndex];
                    int v = seg.destinationIndex;
                    if (v < 0 || v >= nodeCount) continue; // defensive
                    if (visited[v]) continue;

                    float nd = distances[u] + seg.cost;
                    if (nd < distances[v]) {
                        distances[v] = nd;
                        prevNode[v] = u;
                        prevSegment[v] = segIndex;
                        if (heapPos[v] == -1) HeapInsert(v);
                        else HeapDecreaseKey(v);
                    }
                }
            }

            if (!found) {
                // no path found -> return empty lists
                return;
            }

            // Reconstruct path (backwards)
            int curNode = destinationNodeIndex;
            while (curNode != -1) {
                resultNodeIndices.Add(curNode);
                int seg = prevSegment[curNode];
                if (seg != -1) resultPathSegmentIndices.Add(seg);
                curNode = prevNode[curNode];
            }

            // Currently reversed (destination->start), reverse in-place.
            resultNodeIndices.Reverse();
            resultPathSegmentIndices.Reverse();

            // Note: resultPathSegmentIndices.Count will be resultNodeIndices.Count - 1 (if path length >=1)
        }

        #region Binary heap (min-heap based on distances[])
        // 1-based heap for simpler arithmetic. heapSize = number of elements.

        void HeapInsert(int node) {
            heapSize++;
            int pos = heapSize;
            heap[pos] = node;
            heapPos[node] = pos;
            HeapBubbleUp(pos);
        }

        void HeapDecreaseKey(int node) {
            int pos = heapPos[node];
            if (pos <= 0) return;
            HeapBubbleUp(pos);
        }

        int HeapPopMin() {
            if (heapSize == 0) return -1;
            int minNode = heap[HEAP_BASE];
            heapPos[minNode] = -1;

            if (heapSize == 1) {
                heapSize = 0;
                return minNode;
            }

            // Move last to root and sift down
            int last = heap[heapSize];
            heap[HEAP_BASE] = last;
            heapPos[last] = HEAP_BASE;
            heapSize--;
            HeapSiftDown(HEAP_BASE);
            return minNode;
        }

        void HeapBubbleUp(int pos) {
            int node = heap[pos];
            float nodeDist = distances[node];
            while (pos > HEAP_BASE) {
                int parentPos = pos >> 1;
                int parentNode = heap[parentPos];
                if (distances[parentNode] <= nodeDist) break;
                // swap
                heap[pos] = parentNode;
                heapPos[parentNode] = pos;
                pos = parentPos;
            }
            heap[pos] = node;
            heapPos[node] = pos;
        }

        void HeapSiftDown(int pos) {
            int node = heap[pos];
            float nodeDist = distances[node];
            int half = heapSize >> 1; // nodes with children
            while (pos <= half) {
                int left = pos << 1;
                int right = left + 1;
                int smallestChildPos = left;
                int childNode = heap[left];
                float childDist = distances[childNode];

                if (right <= heapSize) {
                    int rightNode = heap[right];
                    float rightDist = distances[rightNode];
                    if (rightDist < childDist) {
                        smallestChildPos = right;
                        childNode = rightNode;
                        childDist = rightDist;
                    }
                }

                if (childDist >= nodeDist) break;

                // move child up
                heap[pos] = childNode;
                heapPos[childNode] = pos;
                pos = smallestChildPos;
            }
            heap[pos] = node;
            heapPos[node] = pos;
        }
        #endregion
    }
}

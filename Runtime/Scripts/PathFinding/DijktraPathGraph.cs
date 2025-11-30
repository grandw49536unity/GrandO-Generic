using System;
using System.Collections.Generic;
using UnityEngine;

namespace GrandO.Generic.PathFinding {

    /// <summary>
    /// Dijkstra graph implementation optimized for runtime GC-free path queries.
    /// - Call Refresh() after you change any PathSegment.isBlocked.
    /// - Constructor does initialization (can allocate).
    /// - CalculateNodeSequence and CalculatePathSequence are GC-free (no allocations).
    /// </summary>
    public class DijkstraPathGraph {

        // Public storage containing the edge definitions (you can modify PathSegment.isBlocked and then call Refresh)
        public PathSegment[] pathSegments;

        // Result container (reused, preallocated)
        public List<int> resultIndices { get; private set; }

        // Internal graph representation (adjacency using arrays)
        // For each node: head[node] -> index of edge (in pathSegments) or -1 if none
        int[] head;                 // length = nodeCount
        int[] next;                 // length = pathSegments.Length, next edge index in adjacency linked-list
        // The actual arrays reference pathSegments by index; we will iterate edges by index.
        int nodeCount;
        int edgeCount; // number of pathSegments

        // Working arrays used during Dijkstra (preallocated to nodeCount)
        float[] dist;               // distances from source
        int[] prevNode;             // previous node in path
        int[] prevEdge;             // which edge index connects prevNode -> node
        byte[] visited;             // 0 or 1 visited flag
        int[] heapNodes;            // heap array storing node indices
        float[] heapKeys;           // heap array storing keys (distances)
        int[] heapPos;              // heapPos[node] = position in heapNodes or -1 if not present
        int heapSize;

        /// <summary>
        /// Constructor. You must supply the pathSegments array.
        /// This will scan them to compute nodeCount automatically.
        /// </summary>
        public DijkstraPathGraph(PathSegment[] _pathSegments) {
            if (_pathSegments == null) throw new ArgumentNullException(nameof(_pathSegments));

            pathSegments = _pathSegments;
            edgeCount = pathSegments.Length;

            // discover nodeCount (max index + 1)
            int maxIndex = -1;
            for (int i = 0; i < edgeCount; ++i) {
                var e = pathSegments[i];
                if (e.startIndex > maxIndex) maxIndex = e.startIndex;
                if (e.destinationIndex > maxIndex) maxIndex = e.destinationIndex;
            }
            nodeCount = maxIndex + 1;
            if (nodeCount <= 0) nodeCount = 1;

            // allocate adjacency arrays
            head = new int[nodeCount];
            next = new int[edgeCount];

            // preallocate working arrays sized to nodeCount
            dist = new float[nodeCount];
            prevNode = new int[nodeCount];
            prevEdge = new int[nodeCount];
            visited = new byte[nodeCount];
            heapNodes = new int[nodeCount];
            heapKeys = new float[nodeCount];
            heapPos = new int[nodeCount];

            // prepare result list and ensure capacity to avoid reallocation (GC-free during use)
            resultIndices = new List<int>(Math.Max(16, nodeCount));

            // initial Refresh to build adjacency
            Refresh();
        }

        /// <summary>
        /// Call this method after you change PathSegment.isBlocked values to rebuild the internal adjacency lists.
        /// This may allocate only if you change pathSegments length or nodeCount (not in typical use).
        /// </summary>
        public void Refresh() {
            // initialize heads to -1
            for (int n = 0; n < nodeCount; ++n) head[n] = -1;

            // build adjacency lists using pathSegments indexes that are NOT blocked
            // we use linked-list style storage: for each edge i, next[i] is previous head, and head[start] = i
            for (int i = 0; i < edgeCount; ++i) {
                next[i] = -1; // default
                ref var seg = ref pathSegments[i];
                if (seg.isBlocked) continue;
                int s = seg.startIndex;
                // guard in case pathSegments contain node indices beyond detected nodeCount
                if (s < 0 || s >= nodeCount) continue;
                next[i] = head[s];
                head[s] = i;
            }

            // ensure resultIndices has enough capacity to hold full node path (avoid growth later)
            if (resultIndices.Capacity < nodeCount) resultIndices.Capacity = nodeCount;
        }

        // -----------------------
        // Heap helpers (min-heap)
        // -----------------------
        void HeapInit() {
            heapSize = 0;
            for (int i = 0; i < nodeCount; ++i) heapPos[i] = -1;
        }

        void HeapSwap(int a, int b) {
            int na = heapNodes[a];
            int nb = heapNodes[b];
            float ka = heapKeys[a];
            float kb = heapKeys[b];

            heapNodes[a] = nb;
            heapKeys[a] = kb;
            heapNodes[b] = na;
            heapKeys[b] = ka;

            heapPos[nb] = a;
            heapPos[na] = b;
        }

        void HeapSiftUp(int idx) {
            int cur = idx;
            while (cur > 0) {
                int parent = (cur - 1) >> 1;
                if (heapKeys[cur] >= heapKeys[parent]) break;
                HeapSwap(cur, parent);
                cur = parent;
            }
        }

        void HeapSiftDown(int idx) {
            int cur = idx;
            int half = heapSize >> 1;
            while (cur < half) { // while has at least one child
                int left = (cur << 1) + 1;
                int right = left + 1;
                int smallest = left;
                if (right < heapSize && heapKeys[right] < heapKeys[left]) smallest = right;
                if (heapKeys[cur] <= heapKeys[smallest]) break;
                HeapSwap(cur, smallest);
                cur = smallest;
            }
        }

        /// <summary>
        /// Insert new node into heap or decrease its key (both GC-free).
        /// </summary>
        void HeapPushOrDecrease(int node, float key) {
            int pos = heapPos[node];
            if (pos == -1) {
                int idx = heapSize++;
                heapNodes[idx] = node;
                heapKeys[idx] = key;
                heapPos[node] = idx;
                HeapSiftUp(idx);
            } else {
                if (key < heapKeys[pos]) {
                    heapKeys[pos] = key;
                    HeapSiftUp(pos);
                }
            }
        }

        /// <summary>
        /// Pop min entry from heap. Assumes heapSize > 0.
        /// </summary>
        int HeapPopMin(out float key) {
            int node = heapNodes[0];
            key = heapKeys[0];
            heapSize--;
            if (heapSize > 0) {
                heapNodes[0] = heapNodes[heapSize];
                heapKeys[0] = heapKeys[heapSize];
                heapPos[heapNodes[0]] = 0;
                HeapSiftDown(0);
            }
            heapPos[node] = -1;
            return node;
        }

        // -----------------------
        // Dijkstra main algorithm
        // -----------------------
        void PrepareRun(int startNode) {
            // initialize dist and visited
            for (int i = 0; i < nodeCount; ++i) {
                dist[i] = float.PositiveInfinity;
                prevNode[i] = -1;
                prevEdge[i] = -1;
                visited[i] = 0;
            }

            dist[startNode] = 0f;
            HeapInit();
            HeapPushOrDecrease(startNode, 0f);
        }

        /// <summary>
        /// Main Dijkstra loop. Returns true if destination reached, false if not reachable.
        /// Precondition: startNode/destinationNode within [0, nodeCount)
        /// This method does not allocate.
        /// </summary>
        bool RunDijkstra(int startNode, int destinationNode) {
            PrepareRun(startNode);

            while (heapSize > 0) {
                float curDist;
                int u = HeapPopMin(out curDist);

                // If we've popped a node with visited flag (because duplicates can occur if using different heap approach),
                // but our implementation avoids duplicates by heapPos mapping. Still check visited to be safe.
                if (visited[u] != 0) continue;
                visited[u] = 1;

                if (u == destinationNode) return true;

                // iterate edges outgoing from u
                int e = head[u];
                while (e != -1) {
                    ref var seg = ref pathSegments[e];
                    int v = seg.destinationIndex;
                    if (v >= 0 && v < nodeCount && visited[v] == 0) {
                        float alt = curDist + seg.cost;
                        if (alt < dist[v]) {
                            dist[v] = alt;
                            prevNode[v] = u;
                            prevEdge[v] = e;
                            HeapPushOrDecrease(v, alt);
                        }
                    }
                    e = next[e];
                }
            }

            return false; // destination not reached
        }

        /// <summary>
        /// Calculate path as node index sequence from startNodeIndex to destinationNodeIndex.
        /// Returns reference to internal resultIndices List (cleared and filled).
        /// This method is GC-free at runtime (no allocations).
        /// </summary>
        public List<int> CalculateNodeSequence(int startNodeIndex, int destinationNodeIndex) {
            // validate
            if (startNodeIndex < 0 || startNodeIndex >= nodeCount ||
                destinationNodeIndex < 0 || destinationNodeIndex >= nodeCount) {
                resultIndices.Clear();
                return resultIndices;
            }

            resultIndices.Clear();

            bool reached = RunDijkstra(startNodeIndex, destinationNodeIndex);
            if (!reached) {
                // unreachable -> return empty resultIndices
                return resultIndices;
            }

            // reconstruct path nodes (destination -> start) using prevNode
            int cur = destinationNodeIndex;
            while (cur != -1) {
                resultIndices.Add(cur);
                if (cur == startNodeIndex) break;
                cur = prevNode[cur];
            }

            // reverse in place (we have capacity preallocated)
            int i = 0;
            int j = resultIndices.Count - 1;
            while (i < j) {
                int tmp = resultIndices[i];
                resultIndices[i] = resultIndices[j];
                resultIndices[j] = tmp;
                i++; j--;
            }

            return resultIndices;
        }

        /// <summary>
        /// Calculate path as path-segment (edge) index sequence from startNodeIndex to destinationNodeIndex.
        /// Returns reference to internal resultIndices List (cleared and filled).
        /// This method is GC-free at runtime (no allocations).
        /// </summary>
        public List<int> CalculatePathSequence(int startNodeIndex, int destinationNodeIndex) {
            // validate
            if (startNodeIndex < 0 || startNodeIndex >= nodeCount ||
                destinationNodeIndex < 0 || destinationNodeIndex >= nodeCount) {
                resultIndices.Clear();
                return resultIndices;
            }

            resultIndices.Clear();

            bool reached = RunDijkstra(startNodeIndex, destinationNodeIndex);
            if (!reached) {
                return resultIndices;
            }

            // reconstruct path edges (destination -> start) using prevEdge
            int cur = destinationNodeIndex;
            while (cur != -1) {
                int e = prevEdge[cur];
                if (e == -1) {
                    // reached start node (no incoming edge)
                    break;
                }
                resultIndices.Add(e);
                cur = prevNode[cur];
            }

            // the edges are from destination->start; reverse them to be start->destination
            int p = 0;
            int q = resultIndices.Count - 1;
            while (p < q) {
                int tmp = resultIndices[p];
                resultIndices[p] = resultIndices[q];
                resultIndices[q] = tmp;
                p++; q--;
            }

            return resultIndices;
        }

        // -----------------------
        // Optional helpers
        // -----------------------

        /// <summary>
        /// If you need to query nodeCount externally.
        /// </summary>
        public int NodeCount => nodeCount;

        /// <summary>
        /// If you want to change the graph topology (e.g. new pathSegments array), re-create the DijkstraPathGraph
        /// or call this to reinitialize. Note: this will reallocate internal arrays.
        /// </summary>
        public void Reinitialize(PathSegment[] newSegments) {
            pathSegments = newSegments ?? throw new ArgumentNullException(nameof(newSegments));
            edgeCount = pathSegments.Length;

            // discover nodeCount again
            int maxIndex = -1;
            for (int i = 0; i < edgeCount; ++i) {
                var e = pathSegments[i];
                if (e.startIndex > maxIndex) maxIndex = e.startIndex;
                if (e.destinationIndex > maxIndex) maxIndex = e.destinationIndex;
            }
            nodeCount = maxIndex + 1;
            if (nodeCount <= 0) nodeCount = 1;

            head = new int[nodeCount];
            next = new int[edgeCount];

            dist = new float[nodeCount];
            prevNode = new int[nodeCount];
            prevEdge = new int[nodeCount];
            visited = new byte[nodeCount];
            heapNodes = new int[nodeCount];
            heapKeys = new float[nodeCount];
            heapPos = new int[nodeCount];

            if (resultIndices.Capacity < nodeCount) resultIndices.Capacity = nodeCount;

            Refresh();
        }
    }
}

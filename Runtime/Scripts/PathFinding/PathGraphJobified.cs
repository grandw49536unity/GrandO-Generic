using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Jobs;

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace GrandO.Generic.PathFinding {

    public class PathGraphJobified : IDisposable {

        // input (managed)
        private readonly float2[] m_waypoints; // may be null
        private readonly PathSegment[] m_pathSegments;

        // CSR adjacency (native, persistent)
        private readonly NativeArray<int> _neighborOffsets; // length nodeCount+1
        private readonly NativeArray<int> _neighborIndices; // length totalNeighbors
        private readonly NativeArray<float> _neighborCosts; // length totalNeighbors
        private readonly int _nodeCount;
        private readonly NativeArray<float2> _waypointsNative; // empty if null
        private readonly NativeArray<PathSegment> _segmentsNative;

        // Reusable scratch (persistent) for pathfinding to be GC-free
        private readonly NativeArray<float> _gScore; // length nodeCount
        private readonly NativeArray<float> _fScore; // length nodeCount
        private readonly NativeArray<int> _cameFrom; // length nodeCount, -1 default
        private readonly NativeArray<byte> _visited; // length nodeCount (0/1)
        private readonly NativeArray<int> _heap; // length nodeCount (binary heap store indices)
        private readonly NativeArray<int> _heapCount; // length 1 (to store heap count)
        private readonly NativeArray<int> _outPathBuffer; // length nodeCount (reusable)

        private bool _disposed;

        public PathGraphJobified(float2[] waypoints, PathSegment[] pathSegments) {

            m_waypoints = waypoints;
            m_pathSegments = pathSegments ?? Array.Empty<PathSegment>();

            // Build neighbor dictionary like your working code, but then flatten to CSR.
            // Use same logic as your version but produce nodeCount = maxIndex+1
            var neighbors = new Dictionary<int, List<(int neighbor, float cost)>>();
            for (int i = 0; i < m_pathSegments.Length; i++) {
                var s = m_pathSegments[i];
                if (!neighbors.TryGetValue(s.index0, out var l0)) {
                    l0 = new List<(int, float)>();
                    neighbors[s.index0] = l0;
                }
                if (!neighbors.TryGetValue(s.index1, out var l1)) {
                    l1 = new List<(int, float)>();
                    neighbors[s.index1] = l1;
                }

                float cost = s.cost;
                if (m_waypoints != null && cost <= 0f) cost = math.distance(m_waypoints[s.index0], m_waypoints[s.index1]);

                neighbors[s.index0].Add((s.index1, cost));
                neighbors[s.index1].Add((s.index0, cost));
            }

            // Determine nodeCount (max index + 1)
            int maxIndex = -1;
            foreach (var k in neighbors.Keys)
                if (k > maxIndex)
                    maxIndex = k;
            _nodeCount = math.max(0, maxIndex + 1);

            // compute totalNeighbors and offsets
            int totalNeighbors = 0;
            for (int n = 0; n < _nodeCount; n++) {
                if (neighbors.TryGetValue(n, out var list)) totalNeighbors += list.Count;
            }

            _neighborOffsets = new NativeArray<int>(_nodeCount + 1, Allocator.Persistent);
            _neighborIndices = new NativeArray<int>(totalNeighbors, Allocator.Persistent);
            _neighborCosts = new NativeArray<float>(totalNeighbors, Allocator.Persistent);

            int idx = 0;
            for (int n = 0; n < _nodeCount; n++) {
                _neighborOffsets[n] = idx;
                if (neighbors.TryGetValue(n, out var list)) {
                    for (int j = 0; j < list.Count; j++) {
                        _neighborIndices[idx] = list[j].neighbor;
                        _neighborCosts[idx] = list[j].cost;
                        idx++;
                    }
                }
            }
            _neighborOffsets[_nodeCount] = idx;

            // Native copies of input arrays
            _segmentsNative = new NativeArray<PathSegment>(m_pathSegments, Allocator.Persistent);
            _waypointsNative = m_waypoints != null ? new NativeArray<float2>(m_waypoints, Allocator.Persistent) : new NativeArray<float2>(0, Allocator.Persistent);

            // Preallocate scratch arrays sized by nodeCount
            _gScore = new NativeArray<float>(_nodeCount, Allocator.Persistent);
            _fScore = new NativeArray<float>(_nodeCount, Allocator.Persistent);
            _cameFrom = new NativeArray<int>(_nodeCount, Allocator.Persistent);
            _visited = new NativeArray<byte>(_nodeCount, Allocator.Persistent);
            _heap = new NativeArray<int>(_nodeCount, Allocator.Persistent);
            _heapCount = new NativeArray<int>(1, Allocator.Persistent);
            _outPathBuffer = new NativeArray<int>(_nodeCount, Allocator.Persistent);

            // init cameFrom to -1
            for (int i = 0; i < _nodeCount; i++) _cameFrom[i] = -1;

            _disposed = false;

        }

        // Public GC-free API: caller provides out buffer (managed or NativeArray) and we fill it.
        // outPathNative must have length >= _nodeCount (or at least max expected)
        public void PathFindingInto(int startIndex, int destinationIndex, NativeArray<int> outPathNative, out int outLen) {

            if (_nodeCount == 0) {
                outLen = 0;
                return;
            }

            var job = new PathfindingReusableJob {
                nodeCount = _nodeCount, neighborOffsets = _neighborOffsets, neighborIndices = _neighborIndices, neighborCosts = _neighborCosts,
                waypoints = _waypointsNative, useAStar = _waypointsNative.Length > 0, start = startIndex, dest = destinationIndex,

                // re-used scratch / outputs
                gScore = _gScore, fScore = _fScore, cameFrom = _cameFrom, visited = _visited,
                heap = _heap, heapCount = _heapCount, outPath = _outPathBuffer, outPathLen = new NativeArray<int>(1, Allocator.Persistent) // per-call tiny temp for length (1 int) - small and short-lived
            };

            job.Run();

            outLen = job.outPathLen[0];
            // copy result to caller buffer (managed copy is inevitable if caller uses managed array; here uses NativeArray)
            for (int i = 0; i < outLen; i++) outPathNative[i] = _outPathBuffer[i];

            job.outPathLen.Dispose();

        }

        // Position-based pathfinding into NativeArray<float2> outPositionsNative; outLen count of positions written.
        // Caller must allocate outPositionsNative length >= (_nodeCount + 2) to be safe.
        public void PathFindingPositionsInto(float2 startPosition, float2 destPosition, NativeArray<float2> outPositionsNative, out int outLen) {

            if (_nodeCount == 0) {
                outLen = 0;
                return;
            }

            // Find nearest points (we'll call FindNearestPointJob with persistent outputs)
            var nearestOut = new NativeArray<float2>(1, Allocator.TempJob);
            var index0Out = new NativeArray<int>(1, Allocator.TempJob);
            var index1Out = new NativeArray<int>(1, Allocator.TempJob);
            var interpOut = new NativeArray<float>(1, Allocator.TempJob);

            var nearestJob = new FindNearestJobReusable {
                waypoints = _waypointsNative, segments = _segmentsNative, point = startPosition, outPoint = nearestOut,
                outIndex0 = index0Out, outIndex1 = index1Out, outT = interpOut
            };
            nearestJob.Run();
            float2 firstPos = nearestOut[0];
            int firstIndex0 = index0Out[0], firstIndex1 = index1Out[0];
            float firstT = interpOut[0];

            nearestOut[0] = default; // reuse
            index0Out[0] = -1;
            index1Out[0] = -1;
            interpOut[0] = 0f;

            nearestJob.point = destPosition;
            nearestJob.Run();
            float2 lastPos = nearestOut[0];
            int lastIndex0 = index0Out[0], lastIndex1 = index1Out[0];
            float lastT = interpOut[0];

            nearestOut.Dispose();
            index0Out.Dispose();
            index1Out.Dispose();
            interpOut.Dispose();

            if (firstIndex0 == lastIndex0 && firstIndex1 == lastIndex1) {
                outPositionsNative[0] = firstPos;
                outPositionsNative[1] = lastPos;
                outLen = 2;
                return;
            }

            if (firstT > 0.5f) (firstIndex0, firstIndex1) = (firstIndex1, firstIndex0);
            if (lastT > 0.5f) (lastIndex0, lastIndex1) = (lastIndex1, lastIndex0);

            // get node index path into a native buffer
            var tmpPath = new NativeArray<int>(_nodeCount, Allocator.TempJob);
            int pathLen;
            PathFindingInto(firstIndex0, lastIndex0, tmpPath, out pathLen);

            if (pathLen == 0) {
                // no path: return just start & end
                if (outPositionsNative.Length < 2) throw new ArgumentException("outPositionsNative too small");
                outPositionsNative[0] = firstPos;
                outPositionsNative[1] = lastPos;
                outLen = 2;
                tmpPath.Dispose();
                return;
            }

            bool removeFirst = pathLen >= 2 && tmpPath[1] == firstIndex1;
            bool removeLast = pathLen >= 2 && tmpPath[pathLen - 2] == lastIndex1;
            int firstDelta = removeFirst ? 1 : 0;
            int lastDelta = removeLast ? 1 : 0;
            int resultLen = pathLen + 2 - firstDelta - lastDelta;

            if (outPositionsNative.Length < resultLen) throw new ArgumentException("outPositionsNative too small");

            outPositionsNative[0] = firstPos;
            outPositionsNative[resultLen - 1] = lastPos;
            int dst = 1;
            for (int i = firstDelta; i < pathLen - lastDelta; i++) {
                outPositionsNative[dst++] = m_waypoints[tmpPath[i]];
            }

            outLen = resultLen;
            tmpPath.Dispose();

        }

        // GC-free FindNearest helper job (Burst). Writes outputs into provided NativeArray slots (we used Temp above).
        [BurstCompile]
        private struct FindNearestJobReusable : IJob {

            [ReadOnly] public NativeArray<float2> waypoints;
            [ReadOnly] public NativeArray<PathSegment> segments;
            public float2 point;
            public NativeArray<float2> outPoint; // length 1
            public NativeArray<int> outIndex0; // length 1
            public NativeArray<int> outIndex1; // length 1
            public NativeArray<float> outT; // length 1

            public void Execute() {
                float minDistSq = float.MaxValue;
                float2 best = new float2(0f, 0f);
                int bi0 = -1, bi1 = -1;
                float bt = 0f;

                for (int i = 0; i < segments.Length; i++) {
                    var seg = segments[i];
                    float2 a = waypoints[seg.index0];
                    float2 b = waypoints[seg.index1];

                    float2 ab = b - a;
                    float denom = math.dot(ab, ab);
                    if (denom <= 1e-9f) {
                        float dsq = math.lengthsq(point - a);
                        if (dsq < minDistSq) {
                            minDistSq = dsq;
                            best = a;
                            bi0 = seg.index0;
                            bi1 = seg.index1;
                            bt = 0f;
                        }
                        continue;
                    }

                    float t = math.clamp(math.dot(point - a, ab) / denom, 0f, 1f);
                    float2 proj = a + ab * t;
                    float ds = math.lengthsq(point - proj);
                    if (ds < minDistSq) {
                        minDistSq = ds;
                        best = proj;
                        bi0 = seg.index0;
                        bi1 = seg.index1;
                        bt = t;
                    }
                }

                outPoint[0] = best;
                outIndex1[0] = bi1;
                outIndex0[0] = bi0;
                outT[0] = bt;

            }

        }

        // Reusable pathfinding job that DOES NOT allocate per-run (uses preallocated arrays).
        [BurstCompile]
        private struct PathfindingReusableJob : IJob {

            public int nodeCount;
            [ReadOnly] public NativeArray<int> neighborOffsets;
            [ReadOnly] public NativeArray<int> neighborIndices;
            [ReadOnly] public NativeArray<float> neighborCosts;
            [ReadOnly] public NativeArray<float2> waypoints;
            public bool useAStar;
            public int start;
            public int dest;

            // scratch / outputs (preallocated by owner)
            public NativeArray<float> gScore; // len=nodeCount
            public NativeArray<float> fScore; // len=nodeCount
            public NativeArray<int> cameFrom; // len=nodeCount
            public NativeArray<byte> visited; // len=nodeCount
            public NativeArray<int> heap; // len=nodeCount
            public NativeArray<int> heapCount; // len=1
            public NativeArray<int> outPath; // len=nodeCount
            public NativeArray<int> outPathLen; // len=1 - temp managed by caller (we used Temp for it in wrapper)

            public void Execute() {

                // Guard
                if (start < 0 || start >= nodeCount || dest < 0 || dest >= nodeCount) {
                    outPathLen[0] = 0;
                    return;
                }

                // initialize arrays
                for (int i = 0; i < nodeCount; i++) {
                    gScore[i] = float.PositiveInfinity;
                    fScore[i] = float.PositiveInfinity;
                    cameFrom[i] = -1;
                    visited[i] = 0;
                }
                heapCount[0] = 0;

                gScore[start] = 0f;
                fScore[start] = Heuristic(start, dest);

                HeapPush(start);

                bool found = false;

                while (heapCount[0] > 0) {
                    int current = HeapPop();
                    if (visited[current] != 0) continue;
                    visited[current] = 1;

                    if (current == dest) {
                        found = true;
                        break;
                    }

                    int s = neighborOffsets[current];
                    int e = neighborOffsets[current + 1];
                    for (int p = s; p < e; p++) {
                        int nei = neighborIndices[p];
                        float cost = neighborCosts[p];
                        float tentativeG = gScore[current] + cost;
                        if (tentativeG < gScore[nei]) {
                            cameFrom[nei] = current;
                            gScore[nei] = tentativeG;
                            fScore[nei] = tentativeG + Heuristic(nei, dest);
                            HeapPush(nei);
                        }
                    }
                }

                if (!found) {
                    outPathLen[0] = 0;
                    return;
                }

                // reconstruct path into outPath (reverse, then place in correct order)
                int cur = dest;
                int writeIndex = 0;
                // we'll fill outPath starting at 0..len-1 but reversed, then reverse in-place
                while (cur != -1) {
                    outPath[writeIndex++] = cur;
                    cur = cameFrom[cur];
                }
                // reverse
                for (int i = 0; i < writeIndex / 2; i++) {
                    (outPath[i], outPath[writeIndex - 1 - i]) = (outPath[writeIndex - 1 - i], outPath[i]);
                }
                outPathLen[0] = writeIndex;

            }

            private float Heuristic(int a, int b) {

                if (!useAStar || waypoints.Length == 0) return 0f;
                return math.distance(waypoints[a], waypoints[b]);

            }

            // heap helpers (min-heap by fScore). Uses heapCount[0] as count.
            private void HeapPush(int val) {

                int hc = heapCount[0];
                heap[hc] = val;
                heapCount[0] = hc + 1;
                int i = hc;
                while (i > 0) {
                    int parent = (i - 1) >> 1;
                    if (fScore[heap[parent]] <= fScore[heap[i]]) break;
                    (heap[parent], heap[i]) = (heap[i], heap[parent]);
                    i = parent;
                }

            }

            private int HeapPop() {

                int hc = heapCount[0];
                int res = heap[0];
                hc--;
                heapCount[0] = hc;
                heap[0] = heap[hc];
                int i = 0;
                while (true) {
                    int l = (i << 1) + 1;
                    int r = l + 1;
                    int smallest = i;
                    if (l < hc && fScore[heap[l]] < fScore[heap[smallest]]) smallest = l;
                    if (r < hc && fScore[heap[r]] < fScore[heap[smallest]]) smallest = r;
                    if (smallest == i) break;
                    (heap[i], heap[smallest]) = (heap[smallest], heap[i]);
                    i = smallest;
                }
                return res;

            }

        }

        // Dispose
        public void Dispose() {

            if (_disposed) return;
            if (_neighborOffsets.IsCreated) _neighborOffsets.Dispose();
            if (_neighborIndices.IsCreated) _neighborIndices.Dispose();
            if (_neighborCosts.IsCreated) _neighborCosts.Dispose();
            if (_segmentsNative.IsCreated) _segmentsNative.Dispose();
            if (_waypointsNative.IsCreated) _waypointsNative.Dispose();

            if (_gScore.IsCreated) _gScore.Dispose();
            if (_fScore.IsCreated) _fScore.Dispose();
            if (_cameFrom.IsCreated) _cameFrom.Dispose();
            if (_visited.IsCreated) _visited.Dispose();
            if (_heap.IsCreated) _heap.Dispose();
            if (_heapCount.IsCreated) _heapCount.Dispose();
            if (_outPathBuffer.IsCreated) _outPathBuffer.Dispose();

            _disposed = true;

        }

    }

}
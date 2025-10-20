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

    public class PathGraph {

        private readonly float2[] m_waypoints;
        private readonly PathSegment[] m_pathSegments;

        // adjacency list for faster lookup
        private readonly Dictionary<int, List<(int neighbor, float cost)>> m_neighbors;

        public PathGraph(float2[] waypoints, PathSegment[] pathSegments) {

            m_waypoints = waypoints;
            m_pathSegments = pathSegments;

            m_neighbors = new Dictionary<int, List<(int, float)>>();

            // Build adjacency list
            for (int i = 0; i < pathSegments.Length; i++) {
                var seg = pathSegments[i];
                if (!m_neighbors.ContainsKey(seg.index0)) m_neighbors[seg.index0] = new List<(int, float)>();
                if (!m_neighbors.ContainsKey(seg.index1)) m_neighbors[seg.index1] = new List<(int, float)>();

                float cost = seg.cost;
                if (m_waypoints != null && cost <= 0) {
                    // auto-calc cost from distance if not provided
                    cost = math.distance(m_waypoints[seg.index0], m_waypoints[seg.index1]);
                }

                m_neighbors[seg.index0].Add((seg.index1, cost));
                m_neighbors[seg.index1].Add((seg.index0, cost));
            }

        }

        // ------------------------------
        // 1️⃣ PathFinding by indices
        // ------------------------------
        public int[] PathFinding(int startIndex, int destinationIndex) {

            if (startIndex == destinationIndex) return new int[] { startIndex };

            bool useAStar = m_waypoints != null;

            int count = m_neighbors.Count;
            var openSet = new List<int>() { startIndex };
            var cameFrom = new Dictionary<int, int>();

            var gScore = new Dictionary<int, float>();
            var fScore = new Dictionary<int, float>();

            foreach (var kvp in m_neighbors) {
                gScore[kvp.Key] = float.PositiveInfinity;
                fScore[kvp.Key] = float.PositiveInfinity;
            }

            gScore[startIndex] = 0;
            fScore[startIndex] = useAStar ? HeuristicCost(startIndex, destinationIndex) : 0;

            while (openSet.Count > 0) {
                // manually find node with lowest fScore
                int current = openSet[0];
                float bestF = fScore[current];
                for (int i = 1; i < openSet.Count; i++) {
                    int node = openSet[i];
                    float score = fScore[node];
                    if (score < bestF) {
                        bestF = score;
                        current = node;
                    }
                }

                if (current == destinationIndex) return ReconstructPath(cameFrom, current);

                openSet.Remove(current);

                if (!m_neighbors.ContainsKey(current)) continue;

                foreach (var (neighbor, cost) in m_neighbors[current]) {
                    float tentativeG = gScore[current] + cost;
                    if (tentativeG < gScore[neighbor]) {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        fScore[neighbor] = tentativeG + (useAStar ? HeuristicCost(neighbor, destinationIndex) : 0);

                        if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                    }
                }
            }

            // no path found
            return Array.Empty<int>();

        }

        private int[] ReconstructPath(Dictionary<int, int> cameFrom, int current) {

            var path = new List<int> { current };
            while (cameFrom.ContainsKey(current)) {
                current = cameFrom[current];
                path.Add(current);
            }
            path.Reverse();
            return path.ToArray();

        }

        private float HeuristicCost(int a, int b) { return math.distance(m_waypoints[a], m_waypoints[b]); }

        // ------------------------------
        // 2️⃣ FindNearestPointOnGraph
        // ------------------------------
        public float2 FindNearestPointOnGraph(float2 point, out int index0, out int index1, out float interpolation) {

            float minDistSq = float.MaxValue;
            float2 nearestPoint = default;
            index0 = index1 = -1;
            interpolation = 0;

            for (int i = 0; i < m_pathSegments.Length; i++) {
                var seg = m_pathSegments[i];
                float2 a = m_waypoints[seg.index0];
                float2 b = m_waypoints[seg.index1];

                float2 ab = b - a;
                float2 ap = point - a;

                float t = math.dot(ap, ab) / math.dot(ab, ab);
                t = math.clamp(t, 0, 1);

                float2 proj = a + t * ab;
                float distSq = math.lengthsq(point - proj);

                if (distSq < minDistSq) {
                    minDistSq = distSq;
                    nearestPoint = proj;
                    index0 = seg.index0;
                    index1 = seg.index1;
                    interpolation = t;
                }
            }

            return nearestPoint;

        }

        // ------------------------------
        // 3️⃣ PathFinding by positions
        // ------------------------------

        public float2[] PathFindingIntoPosition(int startIndex, int destinationIndex) { 
            int[] path = PathFinding(startIndex, destinationIndex);
            float2[] result = new float2[path.Length];
            for (int i = 0; i < path.Length; i++) {
                result[i] = m_waypoints[path[i]];
            }
            return result;
        }

        public float2[] PathFindingIntoPosition(float2 startPosition, int destinationIndex) { 
            float2 firstPosition = FindNearestPointOnGraph(startPosition, out int firstIndex0, out int firstIndex1, out float firstInterpolation);
            if (firstInterpolation > 0.5f) (firstIndex0, firstIndex1) = (firstIndex1, firstIndex0);
            int[] path = PathFinding(firstIndex0, destinationIndex);
            int pathLength = path.Length;
            bool removeFirst = pathLength >= 2 && path[1] == firstIndex1;
            int firstDelta = removeFirst ? 1 : 0;
            int resultLength = pathLength + 1 - firstDelta;
            float2[] result = new float2[resultLength];
            result[0] = firstPosition;
            for (int i = firstDelta; i < path.Length; i++) {
                result[i + 1 - firstDelta] = m_waypoints[path[i]];
            }
            return result;
        }

        public float2[] PathFindingIntoPosition(float2 startPosition, float2 destinationPosition) {

            float2 firstPosition = FindNearestPointOnGraph(startPosition, out int firstIndex0, out int firstIndex1, out float firstInterpolation);
            float2 lastPosition = FindNearestPointOnGraph(destinationPosition, out int lastIndex0, out int lastIndex1, out float lastInterpolation);
            
            if (firstIndex0 == lastIndex0 && firstIndex1 == lastIndex1) { return new float2[] { firstPosition, lastPosition }; }

            if (firstInterpolation > 0.5f) (firstIndex0, firstIndex1) = (firstIndex1, firstIndex0);
            if (lastInterpolation > 0.5f) (lastIndex0, lastIndex1) = (lastIndex1, lastIndex0);

            int[] path = PathFinding(firstIndex0, lastIndex0);
            int pathLength = path.Length;

            bool removeFirst = pathLength >= 2 && path[1] == firstIndex1;
            bool removeLast = pathLength >= 2 && path[^2] == lastIndex1;

            int firstDelta = removeFirst ? 1 : 0;
            int lastDelta = removeLast ? 1 : 0;
            int resultLength = pathLength + 2 - firstDelta - lastDelta;

            float2[] result = new float2[resultLength];
            result[0] = firstPosition;
            result[resultLength - 1] = lastPosition;

            for (int i = firstDelta; i < path.Length - lastDelta; i++) {
                result[i + 1 - firstDelta] = m_waypoints[path[i]];
            }

            return result;

        }

    }

}
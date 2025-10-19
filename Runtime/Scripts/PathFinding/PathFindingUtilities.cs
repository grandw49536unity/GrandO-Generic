using System;
using UnityEngine;

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace GrandO.Generic.PathFinding {

    public static class PathFindingUtilities {

        public static Vector2 ToVector2(this Vector3 vector) { return new Vector2(vector.x, vector.z); }
        public static float2 ToFloat2(this float3 vector) { return new float2(vector.x, vector.z); }
        public static Vector3 ToVector3(this Vector2 vector) { return new Vector3(vector.x, 0f, vector.y); }
        public static float3 ToFloat3(this float2 vector) { return new float3(vector.x, 0f, vector.y); }

        public static Vector2 FindNearestPointOnPaths(Vector3 playerPosition, in Vector2[] nodes, in Vector2Int[] paths) => FindNearestPointOnPaths(playerPosition.ToVector2(), nodes, paths);
        public static Vector2 FindNearestPointOnPaths(Vector2 playerPosition, in Vector2[] nodes, in Vector2Int[] paths) {
            float minDistSqr = float.MaxValue;
            Vector2 nearestPoint = playerPosition;
            for (int i = 0; i < paths.Length; i++) {
                Vector2 a = nodes[paths[i].x];
                Vector2 b = nodes[paths[i].y];
                Vector2 ab = b - a;
                Vector2 ap = playerPosition - a;
                float t = Vector2.Dot(ap, ab) / ab.sqrMagnitude;
                t = Mathf.Clamp01(t);
                Vector2 closest = a + ab * t;
                float distSqr = (playerPosition - closest).sqrMagnitude;
                if (distSqr < minDistSqr) {
                    minDistSqr = distSqr;
                    nearestPoint = closest;
                }
            }
            return nearestPoint;
        }
        
        public static float2 FindNearestPointOnPaths(float3 playerPosition, in float2[] nodes, in int2[] paths) =>FindNearestPointOnPaths(playerPosition.ToFloat2(), nodes, paths);
        public static float2 FindNearestPointOnPaths(float2 playerPosition, in float2[] nodes, in int2[] paths) {
            float minDistSqr = float.MaxValue;
            float2 nearestPoint = playerPosition;
            for (int i = 0; i < paths.Length; i++) {
                float2 a = nodes[paths[i].x];
                float2 b = nodes[paths[i].y];
                float2 ab = b - a;
                float2 ap = playerPosition - a;
                float t = math.dot(ap, ab) / math.lengthsq(ab);
                t = math.saturate(t);
                float2 closest = a + ab * t;
                float distSqr = math.lengthsq(playerPosition - closest);
                if (distSqr < minDistSqr) {
                    minDistSqr = distSqr;
                    nearestPoint = closest;
                }
            }
            return nearestPoint;
        }

    }

}
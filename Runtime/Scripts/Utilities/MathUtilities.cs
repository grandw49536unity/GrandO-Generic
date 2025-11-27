using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Unity.Mathematics;

namespace GrandO.Generic {

	public static class MathUtilities {

		public static float2 To2D(this float3 f3) { 
			return new float2(f3.x, f3.z);
		}
		
		public static float3 To3D(this float2 f2, float y = 0f) { 
			return new float3(f2.x, y, f2.y);
		}

		public static float3 RotateTowards(this float3 current, float3 target, float maxRadiansDelta) {
			float dot = math.dot(current, target);
			dot = math.clamp(dot, -1f, 1f);
			float angle = math.acos(dot);
			if (angle <= maxRadiansDelta || angle <= 1e-6f) return target;
			float t = maxRadiansDelta / angle;
			return math.normalize(math.lerp(current, target, t));
		}

		public static float3 RotateTowardsFast(this float3 current, float3 target, float maxRadiansDelta) {
			float3 result = math.lerp(current, target, maxRadiansDelta);
			return math.normalize(result);
		}

		public static float3 ProjectionPointToLine(float3 point, float3 linePointA, float3 linePointB) {
			float3 ab = linePointB - linePointA;
			float3 ap = point - linePointA;
			float t = math.dot(ap, ab) / math.dot(ab, ab);
			t = math.clamp(t, 0f, 1f); // Remove this to calculate infinite line
			return linePointA + t * ab;
		}

		public static float3 DisplacementOfPointToLine(float3 point, float3 linePointA, float3 linePointB) {
			return ProjectionPointToLine(point, linePointA, linePointB) - point;
		}

		public static float2 ProjectionPointToLine(float2 point, float2 linePointA, float2 linePointB) {
			float2 ab = linePointB - linePointA;
			float2 ap = point - linePointA;
			float t = math.dot(ap, ab) / math.dot(ab, ab);
			t = math.clamp(t, 0f, 1f); // Remove this to calculate infinite line
			return linePointA + t * ab;
		}
		
		public static float2 DisplacementOfPointToLine(float2 point, float2 linePointA, float2 linePointB) {
			return ProjectionPointToLine(point, linePointA, linePointB) - point;
		}

	}

}
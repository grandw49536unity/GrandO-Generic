// using System.Collections.Generic;
//
// using UnityEngine;
// using UnityEngine.Jobs;
//
// using Unity.Mathematics;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Jobs;
//
// namespace GrandO.Generic {
//
// 	public partial class JobSystemManager {
//
// 		public enum TransformUpdateRule {
// 			NoUpdate = 0,
// 			Local,
// 			WorldSpace,
// 			LocalPositionOnly,
// 			LocalRotationOnly,
// 			WorldSpacePositionOnly,
// 			WorldSpaceRotationOnly
// 		}
//
// 		[BurstCompile]
// 		public struct TransformJobData {
// 			public TransformUpdateRule rule;
// 			public float3 position;
// 			public quaternion rotation;
// 		}
//
// 		public bool ValidateTransformJob() {
// 			int transformCount = transformJobDataList.Count;
// 			if (transformCount == 0) return false;
// 			if (!transformJobDataArray.IsCreated) {
// 				transformJobDataArray = new NativeArray<TransformJobData>(transformJobDataList.ToArray(), Allocator.Persistent);
// 				transformAccessArray = new TransformAccessArray(transformList.ToArray(), 8);
// 				isTransformListUpdated = false;
// 			}
// 			if (isTransformListUpdated) {
// 				if (transformJobDataArray.Length == transformCount) {
// 					for (int i = 0; i < transformCount; i++) transformJobDataArray[i] = transformJobDataList[i];
// 				} else {
// 					transformJobDataArray.Dispose();
// 					transformJobDataArray = new NativeArray<TransformJobData>(transformJobDataList.ToArray(), Allocator.Persistent);
// 				}
// 				transformAccessArray.SetTransforms(transformList.ToArray());
// 			}
// 			return true;
// 		}
//
// 		// int GetDesiredJobCount(int transformCount) {
// 		// 	if (transformCount <= 128) return 1;
// 		// 	if (transformCount <= 512) return 2;
// 		// 	if (transformCount <= 2048) return 4;
// 		// 	if (transformCount <= 4096) return 8;
// 		// 	if (transformCount <= 8192) return 16;
// 		// 	return 24; // cap around 10k+
// 		// }
//
// 		public void DestroyTransformJobData() {
// 			if (transformJobDataArray.IsCreated) transformJobDataArray.Dispose();
// 			if (transformAccessArray.isCreated) transformAccessArray.Dispose();
// 		}
//
// 		[BurstCompile]
// 		public struct TransformJob : IJobParallelForTransform {
// 			[ReadOnly] public NativeArray<TransformJobData> transformJobDataArray;
// 			public void Execute(int index, TransformAccess transformAccess) {
// 				TransformJobData data = transformJobDataArray[index];
// 				if (data.rule != TransformUpdateRule.NoUpdate) {
// 					switch (data.rule) {
// 						case TransformUpdateRule.Local:
// 							transformAccess.SetLocalPositionAndRotation(data.position, data.rotation);
// 							break;
// 						case TransformUpdateRule.WorldSpace:
// 							transformAccess.SetPositionAndRotation(data.position, data.rotation);
// 							break;
// 						case TransformUpdateRule.LocalPositionOnly:
// 							transformAccess.localPosition = data.position;
// 							break;
// 						case TransformUpdateRule.LocalRotationOnly:
// 							transformAccess.localRotation = data.rotation;
// 							break;
// 						case TransformUpdateRule.WorldSpacePositionOnly:
// 							transformAccess.position = data.position;
// 							break;
// 						case TransformUpdateRule.WorldSpaceRotationOnly:
// 							transformAccess.rotation = data.rotation;
// 							break;
// 					}
// 				}
// 			}
// 		}
//
// 	}
//
// }
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Jobs;

using Unity.Collections;
using Unity.Jobs;

namespace GrandO.Generic {

	[DefaultExecutionOrder(1000)]
	public partial class JobSystemManager : MonoBehaviour {
		
		private static JobSystemManager singleton;
		public static JobSystemManager GetSingleton() { 
			return singleton;
		}
		public static JobSystemManager CreateSingleton(int _jobCapacity = 256) {
			if (!singleton) {
				GeneralUtilities.CreateGameObjectWithComponent<JobSystemManager>("JobSystemManager").Initialize(_jobCapacity);
			}
			return singleton;
		}

		private int jobCapacity = 256;
		
		private List<JobHandle> jobHandles;
		private List<JobHandle> afterJobHandles;
		private List<JobHandle> lateJobHandles;
		
		private System.Action onJobComplete;
		private System.Action onAfterJobComplete;
		private System.Action onLateJobComplete;

		private bool isTransformListUpdated;
		private List<Transform> transformList;
		private List<TransformJobData> transformJobDataList;
		private NativeArray<TransformJobData> transformJobDataArray;
		private TransformAccessArray transformAccessArray;
		
		public void ScheduleJobComplete(JobHandle _jobHandle) { 
			jobHandles.Add(_jobHandle);
		}
		
		public void RegisterJobCompleteEvent(System.Action _action) {
			onJobComplete += _action;
		}
		
		public void RegisterAfterJobCompleteEvent(System.Action _action) {
			onAfterJobComplete += _action;
		}
		
		public void RegisterLateJobCompleteEvent(System.Action _action) {
			onLateJobComplete += _action;
		}
		
		public void ScheduleAfterJobComplete(JobHandle _jobHandle) { 
			afterJobHandles.Add(_jobHandle);
		}

		public void ScheduleLateJobComplete(JobHandle _jobHandle) { 
			lateJobHandles.Add(_jobHandle);
		}

		private void Initialize(int _jobCapacity = 256) {
			singleton = this;
			jobCapacity = _jobCapacity;
			jobHandles = new List<JobHandle>(jobCapacity);
			afterJobHandles = new List<JobHandle>(jobCapacity);
			lateJobHandles = new List<JobHandle>(jobCapacity);
			transformList = new List<Transform>(jobCapacity*2);
			transformJobDataList = new List<TransformJobData>(jobCapacity*2);
		}

		private void Update() { 
			
			for (int i = 0; i < jobHandles.Count; i++) jobHandles[i].Complete();
			jobHandles.Clear();
			onJobComplete?.Invoke();
			onJobComplete = null;
			
			for (int i = 0; i < afterJobHandles.Count; i++) afterJobHandles[i].Complete();
			afterJobHandles.Clear();
			onAfterJobComplete?.Invoke();
			onAfterJobComplete = null;
			
		}

		private void LateUpdate() {

			if (ValidateTransformJob()) {
				TransformJob transformJob = new TransformJob() { transformJobDataArray = transformJobDataArray};
				JobHandle transformJobHandle = transformJob.Schedule(transformAccessArray);
				ScheduleLateJobComplete(transformJobHandle);
			}

			for (int i = 0; i < lateJobHandles.Count; i++) lateJobHandles[i].Complete();
			lateJobHandles.Clear();
			onLateJobComplete?.Invoke();
			onLateJobComplete = null;
			
		}

	}

}
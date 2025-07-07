using System.Collections.Generic;

using UnityEngine;

using Unity.Jobs;

namespace GrandO.Generic {

	[DefaultExecutionOrder(1000)]
	public class JobSystemManager : MonoBehaviour {
		
		private static JobSystemManager singleton;
		public static JobSystemManager GetSingleton() { 
			return singleton;
		}
		public static JobSystemManager CreateSingleton() {
			if (!singleton) {
				singleton = GeneralUtilities.CreateGameObjectWithComponent<JobSystemManager>("JobSystemManager");
				singleton.Initialize();
			}
			return singleton;
		}
		
		private List<JobHandle> jobHandles;
		private List<JobHandle> afterJobHandles;
		private List<JobHandle> lateJobHandles;
		
		private System.Action onJobComplete;
		private System.Action onAfterJobComplete;
		private System.Action onLateJobComplete;
		
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

		private void Initialize() {
			jobHandles = new List<JobHandle>();
			afterJobHandles = new List<JobHandle>();
			lateJobHandles = new List<JobHandle>();
		}

		private void Update() { 
			for (int i = 0; i < jobHandles.Count; ++i) {
				jobHandles[i].Complete();
			}
			jobHandles.Clear();
			onJobComplete?.Invoke();
			onJobComplete = null;
			for (int i = 0; i < afterJobHandles.Count; ++i) {
				afterJobHandles[i].Complete();
			}
			afterJobHandles.Clear();
			onAfterJobComplete?.Invoke();
			onAfterJobComplete = null;
		}

		private void LateUpdate() {
			for (int i = 0; i < lateJobHandles.Count; ++i) {
				lateJobHandles[i].Complete();
			}
			lateJobHandles.Clear();
			onLateJobComplete?.Invoke();
			onLateJobComplete = null;
		}

	}

}
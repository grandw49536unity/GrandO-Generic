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
		private List<JobHandle> lateJobHandles;
		
		public void ScheduleJobComplete(JobHandle _jobHandle) { 
			jobHandles.Add(_jobHandle);
		}

		public void ScheduleLateJobComplete(JobHandle _jobHandle) { 
			lateJobHandles.Add(_jobHandle);
		}

		private void Initialize() {
			jobHandles = new List<JobHandle>();
			lateJobHandles = new List<JobHandle>();
		}

		private void Update() { 
			for (int i = 0; i < jobHandles.Count; ++i) {
				jobHandles[i].Complete();
			}
			jobHandles.Clear();
		}

		private void LateUpdate() {
			for (int i = 0; i < lateJobHandles.Count; ++i) {
				lateJobHandles[i].Complete();
			}
			lateJobHandles.Clear();
		}

	}

}
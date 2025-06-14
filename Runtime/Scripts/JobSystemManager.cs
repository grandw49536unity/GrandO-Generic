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

		public void ScheduleJobComplete(JobHandle _jobHandle) { 
			jobHandles.Add(_jobHandle);
		}

		private void Initialize() {
			jobHandles = new List<JobHandle>();
		}
		
		private void LateUpdate() {
			for (int i = 0; i < jobHandles.Count; ++i) {
				jobHandles[i].Complete();
			}
			jobHandles.Clear();
		}

	}

}
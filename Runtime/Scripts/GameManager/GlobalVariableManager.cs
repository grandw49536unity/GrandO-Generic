using System;
using UnityEngine;
using UnityEngine.Events;

namespace GrandO.Generic {

	public static class GlobalVariable {
		
		public static float gameSpeed = 1f;
		
	}

	// [DefaultExecutionOrder(-1)]
	// public class GlobalVariableManager : MonoBehaviour {
	//
	// 	[Range(0f, 3f)] public float gameSpeed = 1f;
	// 	private float m_gameSpeed = 1f;
	// 	
	// 	public void Start() {
	// 		m_gameSpeed = gameSpeed;
	// 	}
	// 	
	// 	public void Update() { 
	// 		
	// 		if (!Mathf.Approximately(m_gameSpeed, gameSpeed)) {
	// 			m_gameSpeed = gameSpeed;
	// 			GlobalVariable.gameSpeed = Mathf.Max(0f, m_gameSpeed);
	// 		}
	// 		
	// 	}
	// 	
	// }
	
}
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Jobs;

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace GrandO.Generic.VerletRope {

    public class VerletRopeNodeController : MonoBehaviour {

        public bool isKinematic;
        public VerletRopeNodeController[] connections;

        [NonSerialized]
        public float3 position, prevPosition;
        
        private Transform m_transform;
        
        public void Initialize() {
            m_transform = transform;
            prevPosition = position = m_transform.position;
        }
        
        public void SetPosition(float3 position) => m_transform.position = position;
        public void SetPosition() => SetPosition(position);
        public void UpdateKinematicPosition() => position = m_transform.position;

    }
    
}
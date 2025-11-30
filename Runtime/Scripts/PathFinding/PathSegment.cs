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

    [Serializable]
    public struct PathSegment {

        public int startIndex;
        
        public int destinationIndex;

        public float cost;

        public bool isBlocked;

        public void SetBlocked(bool _isBlocked) { 
            isBlocked = _isBlocked;
        }

    }
    
}
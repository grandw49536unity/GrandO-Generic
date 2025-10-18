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

        public int index0;
        
        public int index1;

        public float cost;

    }
    
}
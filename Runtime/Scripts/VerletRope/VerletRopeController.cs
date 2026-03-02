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

    public partial class VerletRopeController : MonoBehaviour {

        [Serializable]
        public struct RopeNode {
            public float3 position;
            public float3 prevPosition;
            public bool isKinematic;
            public int kinematicNodeIndex;
            public RopeNode(float3 _position, int _kinematicNodeIndex = -1) {
                position = prevPosition = _position;
                kinematicNodeIndex = _kinematicNodeIndex;
                isKinematic = kinematicNodeIndex >= 0;
            }
            public void Initialize() {
                prevPosition = position;
            }
        }

        [Serializable]
        public struct RopeEdge {
            
            public int nodeIndex0, nodeIndex1;
            public float length;
            
            public RopeEdge(int _nodeIndex0, int _nodeIndex1, float _length) {
                nodeIndex0 = _nodeIndex0;
                nodeIndex1 = _nodeIndex1;
                length = _length;
            }
            
        }

        [Header("Rope Parameters")]
        [Range(0.25f, 4f)] public float ropeTightness = 1f;
        [Range(1, 30)] public int iterationCount = 4;
        public float3 globalForce = new float3(0f, -9.81f, 0f);
        public float3 localForce = new float3(0f, 0f, 0f);

        [Header("Rope Setup")]
        public Transform[] kinematicNodes;
        public RopeNode[] nodes;
        public RopeEdge[] edges;

        private Transform m_transform;
        
        private void Start() {
            m_transform = transform;
            for (int i = 0; i < nodes.Length; i++) nodes[i].Initialize();
        }
        
        public void SetupRope(Transform[] _kinematicNodes, RopeNode[] _nodes, RopeEdge[] _edges) {
            kinematicNodes = _kinematicNodes;
            nodes = _nodes;
            edges = _edges;
        }

        public void Update() {
            quaternion rotation = m_transform.rotation;
            for (int i = 0; i < nodes.Length; i++) {
                RopeNode node = nodes[i];
                if (node.isKinematic) {
                    node.position = kinematicNodes[node.kinematicNodeIndex].position;
                    nodes[i] = node;
                    continue;
                }
                float3 positionBuffer = node.position;
                float3 inertia = node.position - node.prevPosition;
                float3 forceVelocity = (globalForce + math.mul(rotation, localForce)) * (Time.deltaTime * Time.deltaTime * 0.5f);
                node.position += inertia + forceVelocity;
                node.prevPosition = positionBuffer;
                nodes[i] = node;
            }
            for (int iteration = 0; iteration < iterationCount; iteration++) {
                for (int i = 0; i < edges.Length; i++) {
                    RopeEdge edge = edges[i];
                    int nodeIndex0 = edge.nodeIndex0, nodeIndex1 = edge.nodeIndex1;
                    RopeNode node0 = nodes[nodeIndex0], node1 = nodes[nodeIndex1];
                    float3 edgePosition = (node0.position + node1.position) * 0.5f;
                    float3 edgeDirection = math.normalize(node0.position - node1.position);
                    if (!node0.isKinematic) {
                        node0.position = edgePosition + edgeDirection * edge.length * 0.5f * ropeTightness;
                        nodes[nodeIndex0] = node0;
                    }
                    if (!node1.isKinematic) {
                        node1.position = edgePosition - edgeDirection * edge.length * 0.5f * ropeTightness;
                        nodes[nodeIndex1] = node1;
                    }
                }
            }
        }

// #if UNITY_EDITOR
//         private List<Vector3> m_vertices;
//         public void OnDrawGizmos() {
//             m_vertices ??= new List<Vector3>(256);
//             m_vertices.Clear();
//             if (edges == null) return;
//             for (int i = 0; i < edges.Length; i++) {
//                 RopeEdge edge = edges[i];
//                 int nodeIndex0 = edge.nodeIndex0, nodeIndex1 = edge.nodeIndex1;
//                 RopeNode node0 = nodes[nodeIndex0], node1 = nodes[nodeIndex1];
//                 Gizmos.DrawLine(node0.position, node1.position);
//             }
//         }
// #endif

    }

}
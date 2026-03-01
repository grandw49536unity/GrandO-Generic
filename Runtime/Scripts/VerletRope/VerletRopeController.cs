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

        public class RopeEdge {
            public VerletRopeNodeController node0, node1;
            public float length;
            public RopeEdge(VerletRopeNodeController _node0, VerletRopeNodeController _node1) {
                node0 = _node0;
                node1 = _node1;
                length = math.distance(node0.position, node1.position);
            }
        }

        public VerletRopeNodeController[] ropeNodes;

        [Header("Rope Parameters")]
        [Range(0.25f, 4f)]
        public float ropeTightness = 1f;
        public int iterationCount = 4;
        public float3 globalForce = new float3(0f, -9.81f, 0f);
        public float3 localForce = new float3(0f, 0f, 0f);

        private Transform m_transform;
        private RopeEdge[] m_edges;
        
        public void Start() { 
            Initialize();
        }
        
        public void Initialize() {
            for (int i = 0; i < ropeNodes.Length; i++) ropeNodes[i].Initialize();
            List<RopeEdge> edgeList = new List<RopeEdge>(24);
            for (int i = 0; i < ropeNodes.Length; i++) {
                VerletRopeNodeController ropeNodeController = ropeNodes[i];
                for (int j = 0; j < ropeNodeController.connections.Length; j++) {
                    VerletRopeNodeController connectedRopeNodeController = ropeNodeController.connections[j];
                    edgeList.Add(new RopeEdge(ropeNodeController, connectedRopeNodeController));
                }
            }
            m_edges = edgeList.ToArray();
            m_transform = transform;
        }

        public void Update() {
            quaternion rotation = m_transform.rotation;
            for (int i = 0; i < ropeNodes.Length; i++) {
                VerletRopeNodeController ropeNodeController = ropeNodes[i];
                if (ropeNodeController.isKinematic) {
                    ropeNodeController.UpdateKinematicPosition();
                    continue;
                }
                float3 positionBuffer = ropeNodeController.position;
                float3 inertia = ropeNodeController.position - ropeNodeController.prevPosition;
                float3 forceVelocity = (globalForce + math.mul(rotation, localForce)) * (Time.deltaTime * Time.deltaTime * 0.5f);
                ropeNodeController.position += inertia + forceVelocity;
                ropeNodeController.prevPosition = positionBuffer;
            }
            for (int iteration = 0; iteration < iterationCount; iteration++) {
                for (int i = 0; i < m_edges.Length; i++) {
                    RopeEdge edge = m_edges[i];
                    VerletRopeNodeController node0 = edge.node0, node1 = edge.node1;
                    float3 edgePosition = (node0.position + node1.position) * 0.5f;
                    float3 edgeDirection = math.normalize(node0.position - node1.position);
                    if (!node0.isKinematic) {
                        node0.position = edgePosition + edgeDirection * edge.length * 0.5f * ropeTightness;
                        node0.SetPosition();
                    }
                    if (!node1.isKinematic) {
                        node1.position = edgePosition - edgeDirection * edge.length * 0.5f * ropeTightness;
                        node1.SetPosition();
                    }
                }
            }
        }

#if UNITY_EDITOR
        private List<Vector3> m_vertices;
        public void OnDrawGizmos() {
            m_vertices ??= new List<Vector3>(256);
            m_vertices.Clear();
            if (ropeNodes == null) return;
            for (int i = 0; i < ropeNodes.Length; i++) {
                VerletRopeNodeController ropeNode = ropeNodes[i];
                if (!ropeNode) return;
                if (ropeNode.connections == null) return;
                for (int j = 0; j < ropeNode.connections.Length; j++) {
                    VerletRopeNodeController connectionRopeNode = ropeNode.connections[j];
                    if (!connectionRopeNode) return;
                    m_vertices.Add(ropeNode.position);
                    m_vertices.Add(connectionRopeNode.position);
                }
            }
            for (int i = 0; i < m_vertices.Count - 1; i += 2) {
                Gizmos.DrawLine(m_vertices[i], m_vertices[i + 1]);
            }
        }
#endif

    }

}
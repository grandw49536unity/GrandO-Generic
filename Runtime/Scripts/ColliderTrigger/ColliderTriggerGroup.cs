using System;

using UnityEngine;
using UnityEngine.Events;

namespace GrandO.Generic {
    
    public class ColliderTriggerGroup : MonoBehaviour {
        
        public event Action onTriggerGroupEnter;
        public event Action onTriggerGroupExit;

        private ColliderTrigger[] m_colliderTriggers;
        
        private int m_triggeredCount = 0;

        // this method cannot be called before RegisterTriggerGroup
        public bool IsIntersect(Collider other) { 
            for (int i = 0; i < m_colliderTriggers.Length; i++) {
                if (m_colliderTriggers[i].IsIntersect(other)) return true;
            }
            return false;
        }

        public void AddEnterListener(Action _onTriggerEnterAction) { 
            onTriggerGroupEnter += _onTriggerEnterAction;
        }
        
        public void AddExitListener(Action _onTriggerExitAction) { 
            onTriggerGroupExit += _onTriggerExitAction;
        }

        public void RegisterTriggerGroup(Action _onTriggerEnterAction, Action _onTriggerExitAction) {
            m_colliderTriggers = GetComponentsInChildren<ColliderTrigger>(true);
            for (int i = 0; i < m_colliderTriggers.Length; i++) {
                m_colliderTriggers[i].RegisterTrigger(OnTriggerGroupEnter, OnTriggerGroupExit);
            }
            AddEnterListener(_onTriggerEnterAction);
            AddExitListener(_onTriggerExitAction);
        }

        private void OnTriggerGroupEnter(Collider other) {
            m_triggeredCount++;
            if (m_triggeredCount == 1) onTriggerGroupEnter?.Invoke();
        }

        private void OnTriggerGroupExit(Collider other) { 
            m_triggeredCount--;
            if (m_triggeredCount == 0) onTriggerGroupExit?.Invoke();
        }
        
    }
    
}
using System;

using UnityEngine;
using UnityEngine.Events;

namespace GrandO.Generic {
    
    public class ColliderTriggerGroup : MonoBehaviour {
        
        public event Action onTriggerGroupEnter;
        public event Action onTriggerGroupExit;
        
        private int m_triggeredCount = 0;

        public void AddEnterListener(Action _onTriggerEnterAction) { 
            onTriggerGroupEnter += _onTriggerEnterAction;
        }
        
        public void AddExitListener(Action _onTriggerExitAction) { 
            onTriggerGroupExit += _onTriggerExitAction;
        }

        public void RegisterTriggerGroup(Action _onTriggerEnterAction, Action _onTriggerExitAction) {
            AddEnterListener(_onTriggerEnterAction);
            AddExitListener(_onTriggerExitAction);
        }

        private void Start() { 
            ColliderTrigger[] colliderTriggers = GetComponentsInChildren<ColliderTrigger>();
            for (int i = 0; i < colliderTriggers.Length; i++) {
                colliderTriggers[i].RegisterTrigger(OnTriggerGroupEnter, OnTriggerGroupExit);
            }
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
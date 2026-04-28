using System;

using UnityEngine;
using UnityEngine.Events;

namespace GrandO.Generic {
    
    public class ColliderTrigger : MonoBehaviour {
        
        private event Action<Collider> m_onTriggerEnter;
        private event Action<Collider> m_onTriggerExit;

        public void AddEnterListener(Action<Collider> _onTriggerEnterAction) { 
            m_onTriggerEnter += _onTriggerEnterAction;
        }
        
        public void AddExitListener(Action<Collider> _onTriggerExitAction) { 
            m_onTriggerExit += _onTriggerExitAction;
        }

        public void RegisterTrigger(Action<Collider> _onTriggerEnterAction, Action<Collider> _onTriggerExitAction) {
            AddEnterListener(_onTriggerEnterAction);
            AddExitListener(_onTriggerExitAction);
        }

        private void OnTriggerEnter(Collider other) => m_onTriggerEnter?.Invoke(other);
        private void OnTriggerExit(Collider other) => m_onTriggerExit?.Invoke(other);
        
    }
    
}
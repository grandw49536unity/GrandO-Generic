using System;

using UnityEngine;
using UnityEngine.Events;

namespace GrandO.Generic {
    
    public class ColliderTrigger : MonoBehaviour {
        
        public event Action<Collider> onTriggerEnter;
        public event Action<Collider> onTriggerExit;

        public void AddEnterListener(Action<Collider> _onTriggerEnterAction) { 
            onTriggerEnter += _onTriggerEnterAction;
        }
        
        public void AddExitListener(Action<Collider> _onTriggerExitAction) { 
            onTriggerExit += _onTriggerExitAction;
        }

        public void RegisterTrigger(Action<Collider> _onTriggerEnterAction, Action<Collider> _onTriggerExitAction) {
            AddEnterListener(_onTriggerEnterAction);
            AddExitListener(_onTriggerExitAction);
        }

        private void OnTriggerEnter(Collider other) => onTriggerEnter?.Invoke(other);
        private void OnTriggerExit(Collider other) => onTriggerExit?.Invoke(other);
        
    }
    
}
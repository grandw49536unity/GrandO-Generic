using UnityEngine;
using UnityEngine.Events;

namespace GrandO.Generic {
    
    public class ColliderTrigger : MonoBehaviour {
        
        public UnityEvent<Collider> onTriggerEnter;
        public UnityEvent<Collider> onTriggerExit;
        
        public void RegisterTrigger(UnityAction<Collider> _onTriggerEnterAction, UnityAction<Collider> _onTriggerExitAction) {
            onTriggerEnter.AddListener(_onTriggerEnterAction);
            onTriggerExit.AddListener(_onTriggerExitAction);
        }
        
        public void OnTriggerEnter(Collider other) {
            onTriggerEnter?.Invoke(other);
        }
        
        public void OnTriggerExit(Collider other) {
            onTriggerExit?.Invoke(other);
        }
        
    }
    
}
using System;
using System.Collections.Generic;

using UnityEngine;

namespace GrandO.Generic {

	[Serializable]
	public class SerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver {

		[SerializeField] protected List<TKey> m_keys = new List<TKey>();
		[SerializeField] protected List<TValue> m_values = new List<TValue>();

		public void OnBeforeSerialize() {
			m_keys.Clear();
			m_values.Clear();
			foreach (KeyValuePair<TKey, TValue> i in this) {
				m_keys.Add(i.Key);
				m_values.Add(i.Value);
			}
		}

		public void OnAfterDeserialize() {
			Clear();
			for (int i = 0; i < m_keys.Count; i++) {
				Add(m_keys[i], m_values[i]);
			}
		}

	}

}
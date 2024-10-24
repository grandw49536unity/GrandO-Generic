using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GrandO.Generic {

	[Serializable]
	public class MapList<T> : IEnumerable<T> {
		
		private bool m_isInit = false;
		public bool isInit { get { return m_isInit; } }
		
		[SerializeField] private List<T> m_data;
		public List<T> data => m_data;

		private HashSet<T> m_hashSet;
		
		public MapList() {
			m_data = new List<T>();
			Initialize();
		}
		
		public MapList(List<T> _data) {
			m_data = _data;
			Initialize();
		}
		
		public MapList(HashSet<T> _data) {
			m_hashSet = _data;
			InternalUpload();
			m_isInit = true;
		}
		
		public void Initialize() {
			m_hashSet = new HashSet<T>();
			foreach (T i in m_data) {
				m_hashSet.Add(i);
			}
			m_isInit = true;
		}

		public void InternalUpload() {
			m_data = new List<T>();
			foreach (T i in m_hashSet) {
				m_data.Add(i);
			}
		}

		public bool Add(T _value) => m_hashSet.Add(_value);

		public void Remove(T _value) => m_hashSet.Remove(_value);
		
		public bool Contains(T _value) => m_hashSet.Contains(_value);
		
		public int Count => m_hashSet.Count;

		public void Clear() => m_hashSet.Clear();

		public void UnionWith(IEnumerable<T> _other) => m_hashSet.UnionWith(_other);

		public IEnumerator<T> GetEnumerator() => m_hashSet.GetEnumerator();
		
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
		
	}

}
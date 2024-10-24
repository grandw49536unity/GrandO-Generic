using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GrandO.Generic {

	public static class GeneralUtilities {

		public static void Destroy(UnityEngine.Object _object, bool _allowDestroyingAssets = false) {
#if UNITY_EDITOR
			if (!Application.isPlaying) {
				UnityEngine.Object.DestroyImmediate(_object, _allowDestroyingAssets);
			} else {
#endif
				UnityEngine.Object.Destroy(_object);
#if UNITY_EDITOR
			}
#endif
		}

		public static bool IsDiff(this float _a, float _b) { return Mathf.Abs(_a - _b) >= float.Epsilon; }

		public static bool IsEquals(this float _a, float _b) { return Mathf.Abs(_a - _b) < float.Epsilon; }

		public static Vector2 Clamp01(this Vector2 _vec) { return new Vector2(Mathf.Clamp01(_vec.x), Mathf.Clamp01(_vec.y)); }

		public static Vector3 Clamp01(this Vector3 _vec) { return new Vector3(Mathf.Clamp01(_vec.x), Mathf.Clamp01(_vec.y), Mathf.Clamp01(_vec.z)); }

		public static Vector4 Clamp01(this Vector4 _vec) { return new Vector4(Mathf.Clamp01(_vec.x), Mathf.Clamp01(_vec.y), Mathf.Clamp01(_vec.z), Mathf.Clamp01(_vec.w)); }

		public static Vector3 Multiple(this Vector3 _a, Vector3 _b) { return new Vector3(_a.x * _b.x, _a.y * _b.y, _a.z * _b.z); }
		
		public static Vector3 Divide(this Vector3 _a, Vector3 _b) { return new Vector3(_a.x / _b.x, _a.y / _b.y, _a.z / _b.z); }
		
		public static float Remap(float _value, float _fromMin, float _fromMax, float _toMin, float _toMax) {
			return (_value - _fromMin) / (_fromMax - _fromMin) * (_toMax - _toMin) + _toMin;
		}
		
		public static Vector2 Remap(float _value, float _fromMin, float _fromMax, Vector2 _toMin, Vector2 _toMax) {
			return (_value - _fromMin) / (_fromMax - _fromMin) * (_toMax - _toMin) + _toMin;
		}
		
		public static Vector3 Remap(float _value, float _fromMin, float _fromMax, Vector3 _toMin, Vector3 _toMax) {
			return (_value - _fromMin) / (_fromMax - _fromMin) * (_toMax - _toMin) + _toMin;
		}
		
		public static Vector4 Remap(float _value, float _fromMin, float _fromMax, Vector4 _toMin, Vector4 _toMax) {
			return (_value - _fromMin) / (_fromMax - _fromMin) * (_toMax - _toMin) + _toMin;
		}

		public static Vector2 Remap(Vector2 _value, Vector2 _fromMin, Vector2 _fromMax, Vector2 _toMin, Vector2 _toMax) { 
			return new Vector2(Remap(_value.x, _fromMin.x, _fromMax.x, _toMin.x, _toMax.x), Remap(_value.y, _fromMin.y, _fromMax.y, _toMin.y, _toMax.y)); 
		}
		
		public static Vector3 Remap(Vector3 _value, Vector3 _fromMin, Vector3 _fromMax, Vector3 _toMin, Vector3 _toMax) {
			return new Vector3(Remap(_value.x, _fromMin.x, _fromMax.x, _toMin.x, _toMax.x), 
				Remap(_value.y, _fromMin.y, _fromMax.y, _toMin.y, _toMax.y), 
				Remap(_value.z, _fromMin.z, _fromMax.z, _toMin.z, _toMax.z));
		}
		
		public static Vector4 Remap(Vector4 _value, Vector4 _fromMin, Vector4 _fromMax, Vector4 _toMin, Vector4 _toMax) {
			return new Vector4(Remap(_value.x, _fromMin.x, _fromMax.x, _toMin.x, _toMax.x),
				Remap(_value.y, _fromMin.y, _fromMax.y, _toMin.y, _toMax.y), 
				Remap(_value.z, _fromMin.z, _fromMax.z, _toMin.z, _toMax.z), 
				Remap(_value.w, _fromMin.w, _fromMax.w, _toMin.w, _toMax.w));
		}
		
		public static float DistributedRandom() {
			float random = Random.value * 2f - 1f;
			return random * random * random;
		}

		public static GameObject CreateGameObject(string _name, Transform _parent, Vector3 _position, bool _is_global_position, Quaternion _rotation, bool _is_global_rotation) {
			GameObject obj_new = new GameObject(_name);
			Transform node_obj_new = obj_new.transform;
			node_obj_new.SetParent(_parent);
			if (_is_global_position && _is_global_rotation)
				node_obj_new.SetPositionAndRotation(_position, _rotation);
			else if (!_is_global_position && !_is_global_rotation)
				node_obj_new.SetLocalPositionAndRotation(_position, _rotation);
			else {
				if (_is_global_position)
					node_obj_new.position = _position;
				else
					node_obj_new.localPosition = _position;
				if (_is_global_rotation)
					node_obj_new.rotation = _rotation;
				else
					node_obj_new.localRotation = _rotation;
			}
			return obj_new;
		}

		public static GameObject CreateGameObject(string _name, Transform _parent = null) { return CreateGameObject(_name, _parent, Vector3.zero, false, Quaternion.identity, false); }

		public static T CreateGameObjectWithComponent<T>(string _name, Transform _parent, Vector3 _position, bool _is_global_position, Quaternion _rotation, bool _is_global_rotation) where T : UnityEngine.Component {
			GameObject newObj = CreateGameObject(_name, _parent, _position, _is_global_position, _rotation, _is_global_rotation);
			return newObj.AddComponent<T>();
		}

		public static T CreateGameObjectWithComponent<T>(string _name, Transform _parent = null) where T : UnityEngine.Component {
			GameObject newObj = CreateGameObject(_name, _parent, Vector3.zero, false, Quaternion.identity, false);
			return newObj.AddComponent<T>();
		}

		public static T GetRandomElement<T>(this List<T> _list) {
			return _list[Mathf.FloorToInt(Random.value * _list.Count)];
		}
		
		public static T SelectElement<T>(this List<T> _list, float _normalizedValue) {
			int count = _list.Count;
			return _list[Mathf.Clamp(Mathf.FloorToInt(_normalizedValue * count), 0, count - 1)];
		}
		
		public static T GetRandomElement<T>(this HashSet<T> _set) {
			return _set.ElementAt(Mathf.FloorToInt(Random.value * _set.Count));
		}
		
		public static T SelectElement<T>(this HashSet<T> _set, float _normalizedValue) {
			int count = _set.Count;
			return _set.ElementAt(Mathf.Clamp(Mathf.FloorToInt(_normalizedValue * count), 0, count - 1));
		}

		public static KeyValuePair<TKey, TValue> GetRandomElement<TKey, TValue>(this Dictionary<TKey, TValue> _dict) {
			return _dict.ElementAt(Mathf.FloorToInt(Random.value * _dict.Count));
		}
		
		public static KeyValuePair<TKey, TValue> SelectElement<TKey, TValue>(this Dictionary<TKey, TValue> _dict, float _normalizedValue) {
			int count = _dict.Count;
			return _dict.ElementAt(Mathf.Clamp(Mathf.FloorToInt(_normalizedValue * count), 0, count - 1));
		}

	}

}
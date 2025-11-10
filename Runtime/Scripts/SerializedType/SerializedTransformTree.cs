using System;
using UnityEngine;

namespace GrandO.Generic {

	[Serializable]
	public class SerializedTransformTree : SerializedTree<string, Transform> {

		public SerializedTransformTree(Transform _rootTransform) { SetupTransformTree(_rootTransform); }

		public void SetupTransformTree(Transform _rootTransform) {
			Clear();
			rootKey = _rootTransform.name;
			AddTransformRelationship(_rootTransform);
		}

		void AddTransformRelationship(Transform _childTransform, string _parentKey = "") {
			SerializedTreeNode<string, Transform> newNode = new SerializedTreeNode<string, Transform>(_childTransform.name, _childTransform) { parentKey = _parentKey };
			if (_parentKey != "") {
				this[_parentKey].childKeys.Add(newNode.key);
			}
			Add(newNode.key, newNode);
			for (int i = 0; i < _childTransform.childCount; i++) {
				AddTransformRelationship(_childTransform.GetChild(i), newNode.key);
			}
		}

	}

}
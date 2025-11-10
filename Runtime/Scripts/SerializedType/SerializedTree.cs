using System;
using System.Collections.Generic;

using UnityEngine;

namespace GrandO.Generic {
	
	[Serializable]
	public class SerializedTreeNode<TKey, TValue> {
		public TKey key;
		public TValue value;
		public TKey parentKey;
		public List<TKey> childKeys;
		public SerializedTreeNode(TKey _key, TValue _value) {
			key = _key;
			value = _value;
			childKeys = new List<TKey>();
		}
	}

	[Serializable]
	public class SerializedTree<TKey, TValue> : SerializedDictionary<TKey, SerializedTreeNode<TKey, TValue>> {
		
		public TKey rootKey;
		
		public new SerializedTreeNode<TKey, TValue> this[TKey _key] {
			get => base[_key]; 
			set => base[_key] = value;
		}

		public void DefineParentChild(TKey _parentKey, TKey _childKey) {
			this[_childKey].parentKey = _parentKey;
			this[_parentKey].childKeys.Add(_childKey);
		}
		
		public void InsertTreeAt(SerializedTree<TKey, TValue> _insertTree, TKey _insertKey, TKey _parentKey) {
			SearchAndAdd(_insertTree[_insertKey]);
			this[_insertKey].parentKey = _parentKey;
			this[_parentKey].childKeys.Add(_insertKey);
			void SearchAndAdd(SerializedTreeNode<TKey, TValue> _node) {
				Add(_node.key, _node);
				foreach (TKey k in _node.childKeys) {
					SearchAndAdd(_insertTree[k]);
				}
			}
		}
		
		public void RemoveTreeAt(TKey _removeKey) {
			SerializedTreeNode<TKey, TValue> removeTreeNode = this[_removeKey];
			SearchAndRemove(this[_removeKey]);
			void SearchAndRemove(SerializedTreeNode<TKey, TValue> _node) {
				Remove(_node.key);
				foreach (TKey i in _node.childKeys) {
					SearchAndRemove(this[i]);
				}
			}
			SerializedTreeNode<TKey, TValue> parentTreeNode = this[removeTreeNode.parentKey];
			parentTreeNode.childKeys.Remove(_removeKey);
		}
		
		public SerializedTree<TKey, TValue> SeparateTreeAt(TKey _separateKey) {
			SerializedTree<TKey, TValue> separateTree = new SerializedTree<TKey, TValue>();
			SearchAndAdd(this[_separateKey]);
			void SearchAndAdd(SerializedTreeNode<TKey, TValue> _node) {
				separateTree.Add(_node.key, _node);
				foreach (TKey i in _node.childKeys) {
					SearchAndAdd(this[i]);
				}
			}
			RemoveTreeAt(_separateKey);
			return separateTree;
		}

	}

}
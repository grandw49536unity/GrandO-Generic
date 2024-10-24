using System;
using System.Collections.Generic;
using UnityEngine;

namespace GrandO.Generic {
	
	[Serializable]
	public class TreeNode<TKey, TValue> {
		public TKey key;
		public TValue value;
		public TKey parentKey;
		public List<TKey> childKeys;
		public TreeNode(TKey _key, TValue _value) {
			key = _key;
			value = _value;
			childKeys = new List<TKey>();
		}
	}

	[Serializable]
	public class TreeStructure<TKey, TValue> : Dictionary<TKey, TreeNode<TKey, TValue>> {

		private bool m_isInit = false;
		public bool isInit { get { return m_isInit; } }
		
		public TKey rootKey;
		
		[SerializeField] private List<TreeNode<TKey, TValue>> m_nodes;
		public List<TreeNode<TKey, TValue>> nodes => m_nodes;
		
		public TreeStructure() {
			m_nodes = new List<TreeNode<TKey, TValue>>();
			Initialize();
		}

		public void Initialize() {
			Clear();
			foreach (TreeNode<TKey, TValue> i in m_nodes) {
				Add(i.key, i);
			}
			m_isInit = true;
		}
		
		public void InternalUpload() {
			m_nodes = new List<TreeNode<TKey, TValue>>();
			foreach (KeyValuePair<TKey, TreeNode<TKey, TValue>> i in this) {
				m_nodes.Add(i.Value);
			}
		}
		
		public new TreeNode<TKey, TValue> this[TKey _key] {
			get {
#if UNITY_EDITOR
				if (!isInit) {
					Initialize();
				}
#endif
				return base[_key]; 
			}
			set {
#if UNITY_EDITOR
				if (!isInit) {
					Initialize();
				}
#endif
				base[_key] = value; 
			}
		}

		public void DefineParentChild(TKey _parentKey, TKey _childKey) {
			this[_childKey].parentKey = _parentKey;
			this[_parentKey].childKeys.Add(_childKey);
		}
		
		public void InsertTreeAt(TreeStructure<TKey, TValue> _insertTree, TKey _insertKey, TKey _parentKey) {
			SearchAndAdd(_insertTree[_insertKey]);
			this[_insertKey].parentKey = _parentKey;
			this[_parentKey].childKeys.Add(_insertKey);
			void SearchAndAdd(TreeNode<TKey, TValue> _node) {
				Add(_node.key, _node);
				foreach (TKey k in _node.childKeys) {
					SearchAndAdd(_insertTree[k]);
				}
			}
		}
		
		public void RemoveTreeAt(TKey _removeKey) {
			TreeNode<TKey, TValue> removeTreeNode = this[_removeKey];
			SearchAndRemove(this[_removeKey]);
			void SearchAndRemove(TreeNode<TKey, TValue> _node) {
				Remove(_node.key);
				foreach (TKey i in _node.childKeys) {
					SearchAndRemove(this[i]);
				}
			}
			TreeNode<TKey, TValue> parentTreeNode = this[removeTreeNode.parentKey];
			parentTreeNode.childKeys.Remove(_removeKey);
		}
		
		public TreeStructure<TKey, TValue> SeparateTreeAt(TKey _separateKey) {
			TreeStructure<TKey, TValue> separateTree = new TreeStructure<TKey, TValue>();
			SearchAndAdd(this[_separateKey]);
			void SearchAndAdd(TreeNode<TKey, TValue> _node) {
				separateTree.Add(_node.key, _node);
				foreach (TKey i in _node.childKeys) {
					SearchAndAdd(this[i]);
				}
			}
			RemoveTreeAt(_separateKey);
			InternalUpload();
			separateTree.InternalUpload();
			return separateTree;
		}

	}

}
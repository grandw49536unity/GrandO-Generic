// using System;
// using System.Collections.Generic;
// using UnityEngine;
//
// namespace GrandO.Generic {
//
//     [Serializable]
//     public class SerializedHashSet<T> : List<T> {
//
//         private IEqualityComparer<T> comparer;
//
//         public SerializedHashSet() : base() { comparer = EqualityComparer<T>.Default; }
//         
//         public SerializedHashSet(IEnumerable<T> collection) : base(collection) { comparer = EqualityComparer<T>.Default; }
//
//         public SerializedHashSet(IEqualityComparer<T> customComparer) : base() { comparer = customComparer ?? EqualityComparer<T>.Default; }
//         public new bool Add(T item) {
//             if (Contains(item)) return false;
//
//             base.Add(item);
//             return true;
//         }
//
//         private bool ContainsIn(IEnumerable<T> set, T item) {
//             foreach (var element in set)
//                 if (comparer.Equals(element, item))
//                     return true;
//             return false;
//         }
//
//         private int CountUnique(IEnumerable<T> set) {
//             List<T> temp = new List<T>();
//             foreach (var x in set)
//                 if (!ContainsIn(temp, x))
//                     temp.Add(x);
//             return temp.Count;
//         }
//
//         public void ExceptWith(IEnumerable<T> other) {
//             if (other == null) return;
//
//             for (int i = Count - 1; i >= 0; i--) {
//                 if (ContainsIn(other, this[i])) RemoveAt(i);
//             }
//         }
//
//         public void IntersectWith(IEnumerable<T> other) {
//             if (other == null) {
//                 Clear();
//                 return;
//             }
//
//             for (int i = Count - 1; i >= 0; i--) {
//                 if (!ContainsIn(other, this[i])) RemoveAt(i);
//             }
//         }
//
//         public void UnionWith(IEnumerable<T> other) {
//             if (other == null) return;
//
//             foreach (var x in other) Add(x); // our Add ensures no duplicates
//         }
//
//         public void SymmetricExceptWith(IEnumerable<T> other) {
//             if (other == null) return;
//
//             foreach (var x in other) {
//                 if (!Remove(x)) // Remove(x) returns true if existed
//                     Add(x);
//             }
//         }
//
//         public bool Overlaps(IEnumerable<T> other) {
//             if (other == null) return false;
//
//             foreach (var x in other)
//                 if (Contains(x))
//                     return true;
//
//             return false;
//         }
//
//         public bool SetEquals(IEnumerable<T> other) {
//             if (other == null) return Count == 0;
//
//             // same unique count?
//             int otherCount = CountUnique(other);
//             if (otherCount != Count) return false;
//
//             foreach (var x in this)
//                 if (!ContainsIn(other, x))
//                     return false;
//
//             return true;
//         }
//
//         public bool IsSubsetOf(IEnumerable<T> other) {
//             if (other == null) return Count == 0;
//
//             foreach (var x in this)
//                 if (!ContainsIn(other, x))
//                     return false;
//
//             return true;
//         }
//
//         public bool IsSupersetOf(IEnumerable<T> other) {
//             if (other == null) return true;
//
//             foreach (var x in other)
//                 if (!Contains(x))
//                     return false;
//
//             return true;
//         }
//
//         public bool IsProperSubsetOf(IEnumerable<T> other) {
//             if (other == null) return false;
//
//             int otherCount = CountUnique(other);
//             if (Count >= otherCount) return false;
//
//             return IsSubsetOf(other);
//         }
//
//         public bool IsProperSupersetOf(IEnumerable<T> other) {
//             if (other == null) return Count > 0;
//
//             int otherCount = CountUnique(other);
//             if (Count <= otherCount) return false;
//
//             return IsSupersetOf(other);
//         }
//         
//     }
//     
// }
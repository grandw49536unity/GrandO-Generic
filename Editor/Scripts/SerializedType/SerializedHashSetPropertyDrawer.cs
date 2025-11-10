// using System;
// using UnityEditor;
// using UnityEditorInternal;
// using UnityEngine;
//
// namespace GrandO.Generic.Editor {
//
//     [CustomPropertyDrawer(typeof(SerializedHashSet<>))]
//     public class SerializedHashSetDrawer : PropertyDrawer {
//
//         private ReorderableList reorderableList;
//
//         public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
//             InitializeList(property);
//             return EditorGUIUtility.singleLineHeight + (property.isExpanded ? reorderableList.GetHeight() : 0);
//         }
//
//         public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
//             EditorGUI.BeginProperty(position, label, property);
//
//             var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
//             property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label);
//
//             if (property.isExpanded) {
//                 var listRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, reorderableList.GetHeight());
//                 reorderableList.DoList(listRect);
//             }
//
//             EditorGUI.EndProperty();
//         }
//
//         private void InitializeList(SerializedProperty property) {
//             if (reorderableList != null) return;
//
//             var itemsProp = property.FindPropertyRelative("m_items");
//
//             Type itemType = fieldInfo.FieldType.GetGenericArguments()[0];
//             bool showAdd = itemType == typeof(string) || itemType == typeof(int);
//
//             reorderableList = new ReorderableList(property.serializedObject, itemsProp, true, true, showAdd, true);
//
//             reorderableList.drawHeaderCallback = (Rect rect) => {
//                 EditorGUI.LabelField(rect, "Items");
//             };
//
//             reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
//                 var itemElement = itemsProp.GetArrayElementAtIndex(index);
//
//                 rect.y += 2;
//                 EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), itemElement, GUIContent.none);
//             };
//
//             reorderableList.elementHeight = EditorGUIUtility.singleLineHeight + 4;
//
//             reorderableList.onAddCallback = (ReorderableList l) => {
//                 var oldSize = l.serializedProperty.arraySize;
//
//                 int startKey = 0;
//                 string startString = "(New)";
//                 if (oldSize > 0) {
//                     var lastItemElement = itemsProp.GetArrayElementAtIndex(oldSize - 1);
//                     if (lastItemElement.propertyType == SerializedPropertyType.Integer) {
//                         startKey = lastItemElement.intValue + 1;
//                     } else if (lastItemElement.propertyType == SerializedPropertyType.String) {
//                         startString = $"{lastItemElement.stringValue}(New)";
//                     }
//                 }
//
//                 itemsProp.arraySize++;
//                 l.index = oldSize;
//
//                 var itemElement = itemsProp.GetArrayElementAtIndex(oldSize);
//
//                 if (itemElement.propertyType == SerializedPropertyType.String) {
//                     itemElement.stringValue = startString;
//                 } else if (itemElement.propertyType == SerializedPropertyType.Integer) {
//                     itemElement.intValue = startKey;
//                     while (IsDuplicate(itemsProp, oldSize, itemElement.intValue)) {
//                         itemElement.intValue++;
//                     }
//                 }
//
//                 l.serializedProperty.serializedObject.ApplyModifiedProperties();
//             };
//         }
//
//         private bool IsDuplicate(SerializedProperty itemsProp, int oldSize, int value) {
//             for (int i = 0; i < oldSize; i++) {
//                 var itemElement = itemsProp.GetArrayElementAtIndex(i);
//                 if (itemElement.intValue == value) {
//                     return true;
//                 }
//             }
//             return false;
//         }
//
//     }
//
// }
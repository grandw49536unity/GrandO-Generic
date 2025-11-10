using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GrandO.Generic.Editor {

    [CustomPropertyDrawer(typeof(SerializedDictionary<,>))]
    public class SerializedDictionaryPropertyDrawer : PropertyDrawer {

        private ReorderableList reorderableList;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            InitializeList(property);
            return EditorGUIUtility.singleLineHeight + (property.isExpanded ? reorderableList.GetHeight() : 0);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label);

            if (property.isExpanded) {
                var listRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, reorderableList.GetHeight());
                reorderableList.DoList(listRect);
            }

            EditorGUI.EndProperty();
        }

        private void InitializeList(SerializedProperty property) {
            if (reorderableList != null) return;

            var keysProp = property.FindPropertyRelative("m_keys");
            var valuesProp = property.FindPropertyRelative("m_values");

            Type keyType = fieldInfo.FieldType.GetGenericArguments()[0];
            bool showAdd = keyType == typeof(string) || keyType == typeof(int);

            reorderableList = new ReorderableList(property.serializedObject, keysProp, true, true, showAdd, true);

            reorderableList.drawHeaderCallback = (Rect rect) => {
                EditorGUI.LabelField(rect, "Keys / Values");
            };

            reorderableList.elementHeightCallback = (int index) => {
                var keyElement = keysProp.GetArrayElementAtIndex(index);
                var valueElement = valuesProp.GetArrayElementAtIndex(index);

                float keyHeight = EditorGUI.GetPropertyHeight(keyElement, true);
                float valueHeight = EditorGUI.GetPropertyHeight(valueElement, true);

                return Mathf.Max(keyHeight, valueHeight) + 4f; // Padding
            };

            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                var keyElement = keysProp.GetArrayElementAtIndex(index);
                var valueElement = valuesProp.GetArrayElementAtIndex(index);

                rect.y += 2f;
                var halfWidth = rect.width * 0.25f;
                var elementHeight = rect.height - 4f;

                EditorGUI.PropertyField(new Rect(rect.x, rect.y, halfWidth, elementHeight), keyElement, GUIContent.none, true);
                EditorGUI.indentLevel++;
                EditorGUI.PropertyField(new Rect(rect.x + halfWidth + 5, rect.y, rect.width - halfWidth - 5, elementHeight), valueElement, GUIContent.none, true);
                EditorGUI.indentLevel--;
            };

            reorderableList.onAddCallback = (ReorderableList l) => {
                var oldSize = l.serializedProperty.arraySize;

                int startKey = 0;
                string startString = "(New)";
                if (oldSize > 0) {
                    var lastKeyElement = keysProp.GetArrayElementAtIndex(oldSize - 1);
                    if (lastKeyElement.propertyType == SerializedPropertyType.Integer) {
                        startKey = lastKeyElement.intValue + 1;
                    } else if (lastKeyElement.propertyType == SerializedPropertyType.String) {
                        startString = $"{lastKeyElement.stringValue}(New)";
                    }
                }

                keysProp.arraySize++;
                valuesProp.arraySize++;
                l.index = oldSize;

                var keyElement = keysProp.GetArrayElementAtIndex(oldSize);

                if (keyElement.propertyType == SerializedPropertyType.String) {
                    keyElement.stringValue = startString;
                } else if (keyElement.propertyType == SerializedPropertyType.Integer) {
                    keyElement.intValue = startKey;
                    while (IsDuplicate(keysProp, oldSize, keyElement.intValue)) {
                        keyElement.intValue++;
                    }
                }

                // Value remains default

                l.serializedProperty.serializedObject.ApplyModifiedProperties();
            };

            reorderableList.onRemoveCallback = (ReorderableList l) => {
                if (l.index >= 0 && l.index < keysProp.arraySize) {
                    keysProp.DeleteArrayElementAtIndex(l.index);
                    valuesProp.DeleteArrayElementAtIndex(l.index);
                    if (l.index >= keysProp.arraySize) {
                        l.index = keysProp.arraySize - 1;
                    }
                    l.serializedProperty.serializedObject.ApplyModifiedProperties();
                }
            };

            reorderableList.onReorderCallbackWithDetails = (ReorderableList l, int oldIndex, int newIndex) => {
                // Sync reorder to valuesProp
                valuesProp.MoveArrayElement(oldIndex, newIndex);
                l.serializedProperty.serializedObject.ApplyModifiedProperties();
            };
        }

        private bool IsDuplicate(SerializedProperty keysProp, int oldSize, int value) {
            for (int i = 0; i < oldSize; i++) {
                var keyElement = keysProp.GetArrayElementAtIndex(i);
                if (keyElement.intValue == value) {
                    return true;
                }
            }
            return false;
        }

    }

}
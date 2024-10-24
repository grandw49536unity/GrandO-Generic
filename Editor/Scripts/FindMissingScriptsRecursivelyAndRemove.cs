using UnityEditor;
using UnityEngine;

namespace GrandO.Generic.Editor {

    public class FindMissingScriptsRecursivelyAndRemove : EditorWindow {
        private static int _goCount;
        private static int _missingCount;

        private static bool _bHaveRun;

        [MenuItem("Window/FindMissingScriptsRecursivelyAndRemove")]
        public static void ShowWindow() { GetWindow(typeof(FindMissingScriptsRecursivelyAndRemove)); }

        public void OnGUI() {
            if (GUILayout.Button("Remove Missing Scripts in selected GameObjects")) {
                FindInSelected();
            }

            if (!_bHaveRun) return;

            EditorGUILayout.TextField($"{_goCount} GameObjects Selected");
            if (_goCount > 0) EditorGUILayout.TextField($"{_missingCount} Deleted");
        }

        private static void FindInSelected() {
            var go = Selection.gameObjects;
            _goCount = 0;
            _missingCount = 0;
            foreach (var g in go) {
                FindInGo(g);
            }

            _bHaveRun = true;
            Debug.Log($"Searched {_goCount} GameObjects, found {_missingCount} missing");

            AssetDatabase.SaveAssets();
        }

        private static void FindInGo(GameObject g) {
            _goCount++;
            int c = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(g);
            _missingCount += c;
            foreach (Transform childT in g.transform) {
                FindInGo(childT.gameObject);
            }
        }
    }

}
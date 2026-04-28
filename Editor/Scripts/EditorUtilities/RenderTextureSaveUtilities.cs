using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using UnityEditor;
using GrandO.Generic;
using System.IO;

namespace GrandO.Generic.Editor {

    /// <summary>
    /// RenderTexture → PNG saver with correct color space (linear/sRGB fixed)
    /// Works in BOTH Edit Mode and Play Mode
    /// </summary>
    public static class RenderTextureSaver {
        /// <summary>
        /// Saves any RenderTexture as PNG with proper linear → sRGB conversion.
        /// </summary>
        public static void SaveToPNG(RenderTexture rt, string fullFilePath) {
            if (rt == null) {
                Debug.LogError("RenderTexture is null!");
                return;
            }

            // === COLOR SPACE FIX: Blit to sRGB target so GPU does linear→sRGB ===
            RenderTexture tempRT = new RenderTexture(rt.width, rt.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);

            Graphics.Blit(rt, tempRT); // ← This is the magic line

            // Create Texture2D and read from the sRGB version
            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);

            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = tempRT;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = previousActive;

            // Encode and save
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(fullFilePath, bytes);

            // Cleanup
            tempRT.Release();
            Object.DestroyImmediate(tempRT);

            if (Application.isPlaying)
                Object.Destroy(tex);
            else
                Object.DestroyImmediate(tex);

            Debug.Log($"✅ RenderTexture saved as PNG (correct colors) → {fullFilePath}");
        }

        // Right-click any RenderTexture → "Save Selected RenderTexture as PNG"
        [MenuItem("Assets/Save Selected RenderTexture as PNG", false, 20)]
        private static void MenuSaveSelected() {
            if (Selection.activeObject is RenderTexture rt) {
                string defaultName = rt.name + ".png";
                string path = EditorUtility.SaveFilePanel("Save RenderTexture as PNG", "", defaultName, "png");

                if (!string.IsNullOrEmpty(path)) {
                    SaveToPNG(rt, path);
                    if (path.StartsWith(Application.dataPath)) AssetDatabase.Refresh();
                }
            } else {
                EditorUtility.DisplayDialog("No RenderTexture selected", "Please select a RenderTexture first.", "OK");
            }
        }

        [MenuItem("Tools/RenderTexture Saver (Window)")]
        private static void OpenWindow() { RenderTextureSaverWindow.ShowWindow(); }

    }

    public class RenderTextureSaverWindow : EditorWindow {
        private RenderTexture rt;

        public static void ShowWindow() { GetWindow<RenderTextureSaverWindow>("RT → PNG Saver"); }

        private void OnGUI() {
            GUILayout.Label("Drag & Drop RenderTexture here", EditorStyles.boldLabel);
            rt = EditorGUILayout.ObjectField("Render Texture", rt, typeof(RenderTexture), true) as RenderTexture;

            GUILayout.Space(10);
            if (GUILayout.Button("Save as PNG", GUILayout.Height(40))) {
                if (rt != null) {
                    string path = EditorUtility.SaveFilePanel("Save PNG", "", $"_{rt.name}.png", "png");
                    if (!string.IsNullOrEmpty(path)) RenderTextureSaver.SaveToPNG(rt, path);
                } else {
                    EditorUtility.DisplayDialog("Error", "No RenderTexture assigned!", "OK");
                }
            }
        }
    }

}
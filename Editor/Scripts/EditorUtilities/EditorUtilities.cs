using System.IO;
using UnityEngine;
using UnityEditor;

namespace GrandO.Generic.Editor {
	
	public static class EditorUtilities {
		
		public static void CreateFolder(string _folderPath, bool _shouldRefresh = true) {
			if (_shouldRefresh) {
				CreateFolderWithRefreshing(_folderPath);
			} else {
				CreateFolderWithoutRefreshing(_folderPath);
			}
		}
		
		static void CreateFolderWithRefreshing(string _folderPath) {
			string[] subPaths = GetFolderSubPaths(_folderPath);
			string path = subPaths[0];
			for (int i = 1; i < subPaths.Length; i++) {
				path += $"/{subPaths[i]}";
				if (!AssetDatabase.IsValidFolder(path)) {
					string parentFolder = Path.GetDirectoryName(path);
					string newFolderName = Path.GetFileName(path);
					AssetDatabase.CreateFolder(parentFolder, newFolderName);
				}
			}
		}
		
		static void CreateFolderWithoutRefreshing(string _folderPath) {
			string[] subPaths = GetFolderSubPaths(_folderPath);
			string path = GetRealPath(subPaths[0]);
			for (int i = 1; i < subPaths.Length; i++) {
				path += $"/{subPaths[i]}";
				if (!Directory.Exists(path)) {
					Directory.CreateDirectory(path);
				}
			}
		}

		public static void DeleteFolder(string _folderPath, bool _shouldRefresh = true) { 
			if (_shouldRefresh) {
				DeleteFolderWithRefreshing(_folderPath);
			} else {
				DeleteFolderWithoutRefreshing(_folderPath);	
			}
		}

		static void DeleteFolderWithRefreshing(string _folderPath) { 
			if (AssetDatabase.IsValidFolder(_folderPath)) {
				AssetDatabase.DeleteAsset(_folderPath);
			}
		}
		
		static void DeleteFolderWithoutRefreshing(string _folderPath) {
			string path = GetRealPath(_folderPath);
			if (Directory.Exists(path)) {
				DirectoryInfo directoryInfo = new DirectoryInfo(path);
				foreach (FileInfo file in directoryInfo.GetFiles()) file.Delete(); 
				foreach (DirectoryInfo directory in directoryInfo.GetDirectories()) directory.Delete(true); 
			}
		}
		
		public static void CleanFolder(string _folderPath, bool _shouldRefresh = true) {
			DeleteFolder(_folderPath, _shouldRefresh);
			CreateFolder(_folderPath, _shouldRefresh);
		}

		public static string[] GetFolderSubPaths(string _folderPath) {
			return _folderPath.Split("/");
		}

		public static string GetAssetPath(string _realPath) {
			if (_realPath.StartsWith(Application.dataPath)) return $"Assets{_realPath.Substring(Application.dataPath.Length)}";
			return _realPath;
		}

		public static string GetRealPath(string _assetPath) {
			if (_assetPath.StartsWith("Assets")) return $"{Application.dataPath}{_assetPath.Substring(6)}";
			return _assetPath;
		}

		public static T[] LoadAllDataFromFolder<T>(string _folderPath, string _filter = "") where T : UnityEngine.Object {
			string[] identities = AssetDatabase.FindAssets(_filter, new[] { _folderPath });
			int count = identities.Length;
			for (int i = 0; i < count; i++) identities[i] = AssetDatabase.GUIDToAssetPath(identities[i]);
			T[] assets = new T [count];
			for (int i = 0; i < count; i++) assets[i] = AssetDatabase.LoadAssetAtPath<T>(identities[i]);
			return assets;
		}
		
		public static Texture2D[] LoadAllTexture2DsFromFolder(string _folderPath) {
			return LoadAllDataFromFolder<Texture2D>(_folderPath, "t:Texture2D");
		}
		
		public static Material[] LoadAllMaterialsFromFolder(string _folderPath) {
			return LoadAllDataFromFolder<Material>(_folderPath, "t:Material");
		}
		
		public static GameObject[] LoadAllModelsFromFolder(string _folderPath) {
			return LoadAllDataFromFolder<GameObject>(_folderPath, "t:model");
		}
		
	}

}
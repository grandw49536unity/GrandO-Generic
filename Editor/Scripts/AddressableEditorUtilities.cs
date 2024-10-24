using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using Object = UnityEngine.Object;

namespace GrandO.Generic.Editor {

	public static class AddressableEditorUtilities {

		public static void DeleteAddressableGroup(string _groupName) {
			AddressableAssetSettings addressableSetting = AddressableAssetSettingsDefaultObject.Settings;
			AddressableAssetGroup addressableGroup = addressableSetting.FindGroup(_groupName);
			if (addressableGroup) {
				addressableGroup.ClearSchemas(true);
				addressableSetting.RemoveGroup(addressableGroup);
			}
		}

		public static List<AddressableAssetGroup> GetAddressableGroups(string _groupNameSearch) { 
			AddressableAssetSettings addressableSetting = AddressableAssetSettingsDefaultObject.Settings;
			List<AddressableAssetGroup> searchedGroups = addressableSetting.groups.Where(val => val.name.Contains(_groupNameSearch, StringComparison.Ordinal)).ToList();
			return searchedGroups;
		}
		
		public static void DeleteAddressableGroups(string _groupNameSearch) { 
			AddressableAssetSettings addressableSetting = AddressableAssetSettingsDefaultObject.Settings;
			List<AddressableAssetGroup> searchedGroups = addressableSetting.groups.Where(val => val.name.Contains(_groupNameSearch, StringComparison.Ordinal)).ToList();
			foreach (AddressableAssetGroup addressableGroup in searchedGroups) {
				if (addressableGroup) {
					addressableGroup.ClearSchemas(true);
					addressableSetting.RemoveGroup(addressableGroup);
				}
			}
		}

		public static AddressableAssetGroup AddAddressableGroup(string _groupName) {
			AddressableAssetSettings addressableSetting = AddressableAssetSettingsDefaultObject.Settings;
			AddressableAssetGroup addressableGroup = addressableSetting.FindGroup(_groupName);
			if (!addressableGroup) {
				addressableGroup = addressableSetting.CreateGroup(_groupName, false, false, true, null, typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema));
			}
			return addressableGroup;
		}

		public static AddressableAssetGroup ClearAddressableGroup(string _groupName) {
			DeleteAddressableGroup(_groupName);
			return AddAddressableGroup(_groupName);
		}

		public static void AddAssetToAddressableGroup(Object _object, string _assetKey, AddressableAssetGroup _group) {
			AddressableAssetSettings addressableSetting = AddressableAssetSettingsDefaultObject.Settings;
			string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_object));
			AddressableAssetEntry entry = addressableSetting.CreateOrMoveEntry(guid, _group);
			entry.address = _assetKey;
		}

	}

}
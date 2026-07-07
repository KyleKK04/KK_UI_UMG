using System;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KK.UI.UMG.Editor.Sample
{
    public static class KkSampleInventorySampleMenu
    {
        private const string ScenePath = "Packages/com.kk.ui-umg/Sample/InventoryPanelSample/Scene/KkSampleInventorySample.unity";
        private const string PrefabPath = "Packages/com.kk.ui-umg/Sample/InventoryPanelSample/Generated/KkSampleInventoryPanel/Prefabs/KkSampleInventoryPanelView.prefab";
        private const string AddressablesKey = "UI/KkSampleInventoryPanel/KkSampleInventoryPanelView";
        private const string AddressablesGroup = "UI";

        [MenuItem("KK_UI_UMG/Sample/Open Inventory Panel Sample", priority = 30)]
        public static void OpenSample()
        {
            RegisterAddressable();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log($"[KK_UI_UMG] Opened sample scene: {ScenePath}");
        }

        [MenuItem("KK_UI_UMG/Sample/Register Inventory Panel Sample Addressable", priority = 31)]
        public static void RegisterAddressable()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                throw new InvalidOperationException("Addressables settings are not available.");
            }

            var guid = AssetDatabase.AssetPathToGUID(PrefabPath);
            if (string.IsNullOrWhiteSpace(guid))
            {
                throw new InvalidOperationException($"Cannot register Addressable. Prefab has no guid: {PrefabPath}");
            }

            var group = GetOrCreateGroup(settings, AddressablesGroup);
            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.SetAddress(AddressablesKey, false);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            AssetDatabase.SaveAssets();
            Debug.Log($"[KK_UI_UMG] Registered sample Addressable: {AddressablesKey} -> {PrefabPath}");
        }

        private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                return settings.DefaultGroup;
            }

            var group = settings.FindGroup(groupName);
            if (group != null)
            {
                return group;
            }

            if (settings.DefaultGroup == null)
            {
                throw new InvalidOperationException($"Cannot create Addressables group '{groupName}' because DefaultGroup is not available.");
            }

            return settings.CreateGroup(groupName, false, false, true, settings.DefaultGroup.Schemas);
        }
    }
}

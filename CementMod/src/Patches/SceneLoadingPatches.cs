using CementGB.Mod.Utilities;
using Il2CppGB.Core.Loading;
using Il2CppGB.Data.Loading;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace CementGB.Mod.Patches;

[HarmonyLib.HarmonyPatch(typeof(SceneLoader), nameof(SceneLoader.OnSceneListComplete))]
internal static class OnSceneListCompletePatch
{
    private static void Postfix(ref Il2CppSystem.Object data)
    {
        var sceneList = data.Cast<AddressableDataCache>();

        foreach (var sceneInstance in AssetUtilities.GetAllModdedResourceLocationsOfType<SceneInstance>())
        {
            var sceneRef = new AssetReference(sceneInstance.PrimaryKey);

            sceneList._assets.Add(new()
            {
                Asset = sceneRef,
                Key = sceneInstance.PrimaryKey
            });

            Il2CppGB.Core.Resources._assetList.Add(new Il2CppGB.Core.Resources.LoadLoadedItem(new AssetReference($"{sceneInstance.PrimaryKey}-Data"))
            {
                Key = sceneInstance.PrimaryKey + "-Data"
            });

            Mod.Logger.Msg(System.ConsoleColor.DarkGreen, $"New custom stage registered : Key: {sceneInstance.PrimaryKey}");
        }

        data = sceneList;
    }
}

[HarmonyLib.HarmonyPatch(typeof(Il2CppGB.Core.Resources.LoadLoadedItem), nameof(Il2CppGB.Core.Resources.LoadLoadedItem.Load))]
internal static class LoadLoadedItemPatch
{
    private static bool Prefix(Il2CppGB.Core.Resources.LoadLoadedItem __instance, ref AsyncOperationHandle __result)
    {
        if (AssetUtilities.IsModdedKey(__instance.Key))
        {
            __instance._finishedLoading = AsyncOperationStatus.None;
            __instance._loadHandle = Addressables.LoadAssetAsync<ScriptableObject>(__instance.Key);

            __result = __instance._loadHandle;
            return false;
        }

        return true;
    }
}

[HarmonyLib.HarmonyPatch(typeof(LoadScreenDisplayHandler), nameof(LoadScreenDisplayHandler.SetSubTitle))]
internal static class SetSubTitlePatch
{
    private static bool Prefix(LoadScreenDisplayHandler __instance, ref string name)
    {
        if (AssetUtilities.IsModdedKey(name))
        {
            __instance._subTitle.GetComponent<TextMeshProUGUI>().text = name;
            return false;
        }

        return true;
    }
}
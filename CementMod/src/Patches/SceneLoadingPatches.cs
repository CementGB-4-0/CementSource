using System.Linq;
using CementGB.Mod.Utilities;
using Il2CppGB.Core.Loading;
using Il2CppGB.Data.Loading;
using Il2CppTMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace CementGB.Mod.Patches;

[HarmonyLib.HarmonyPatch(typeof(SceneLoader), nameof(SceneLoader.OnSceneListComplete))]
internal static class OnSceneListCompletePatch
{
    public static readonly string[] BlacklistedSceneNames = new string[]
    {
        "_bootScene",
        "Menu"
    };

    private static void Postfix(ref Il2CppSystem.Object data)
    {
        var sceneList = data.Cast<AddressableDataCache>();

        foreach (var sceneInstance in AssetUtilities.GetAllModdedResourceLocationsOfType<SceneInstance>())
        {
            if (sceneInstance.PrimaryKey.StartsWith("_") || BlacklistedSceneNames.Contains(sceneInstance.PrimaryKey))
            {
                LoggingUtilities.VerboseLog(System.ConsoleColor.DarkRed, $"Skipped over scene with key {sceneInstance.PrimaryKey} because it contains characters that are blacklisted.");
                continue;
            }

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
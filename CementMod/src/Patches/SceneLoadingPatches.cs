using System;
using System.Linq;
using CementGB.Mod.CustomContent;
using CementGB.Mod.Utilities;
using GBMDK;
using HarmonyLib;
using Il2CppGB.Core;
using Il2CppGB.Core.Loading;
using Il2CppGB.Data.Loading;
using Il2CppGB.Game.Data;
using Il2CppGB.Gamemodes;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using ConsoleColor = System.ConsoleColor;
using Object = Il2CppSystem.Object;
using Resources = Il2CppGB.Core.Resources;

namespace CementGB.Mod.Patches;

[HarmonyPatch(typeof(SceneData), nameof(SceneData.OnInternalLoad))]
internal static class LoadDataPatch
{
    private static void Postfix(SceneData __instance)
    {
        var key = __instance.name.Split("-").First();
        if (!CustomAddressableRegistration.IsModdedKey(key))
        {
            return;
        }

        var infoHandle = Addressables.LoadAsset<Object>($"{key}-Info").Acquire();
        infoHandle.WaitForCompletion();

        if (infoHandle.Status != AsyncOperationStatus.Succeeded)
        {
            LoggingUtilities.VerboseLog(ConsoleColor.DarkRed,
                $"Failed to load CustomMapInfo from key \"{key}-Info\" : OperationException \"{infoHandle.OperationException.ToString()}\"");
        }

        if (infoHandle.Result == null)
        {
            LoggingUtilities.VerboseLog(ConsoleColor.DarkRed,
                $"Failed to load CustomMapInfo from key \"{key}-Info\" : Result returned null");
            infoHandle.Release();
            return;
        }

        var info = infoHandle.Result.Cast<CustomMapInfo>();
        infoHandle.Release();

        if (info == null || !info.allowedGamemodes.Get().HasFlag(GameModeEnum.Waves) ||
            __instance._wavesData != null) return;
        
        string[] wavesMaps =
        {
            "Grind",
            "Incinerator",
            "Rooftop",
            "Subway"
        };
        foreach (string wavesMap in wavesMaps)
        {
            var sceneDataReference = Global.Instance.SceneLoader._sceneList[wavesMap];
            Resources.LoadLoadedAsset(sceneDataReference, wavesMap + "-Data", out var _,
                new Action<AsyncOperationStatus, AssetReference, Object>(Action));
        }
    }

    private static void Action(AsyncOperationStatus status, AssetReference reference, Object data)
    {
        throw new NotImplementedException();
    }
}

[HarmonyPatch(typeof(SceneLoader), nameof(SceneLoader.OnSceneListComplete))]
internal static class OnSceneListCompletePatch
{
    private static readonly string[] BlacklistedSceneNames =
    [
        "_bootScene",
        "Menu"
    ];

    private static void Postfix(ref Object data)
    {
        var sceneList = data.TryCast<AddressableDataCache>();

        if (!sceneList)
            return;

        foreach (var mapRef in CustomAddressableRegistration.CustomMaps)
        {
            if (mapRef.SceneName.StartsWith("_") || BlacklistedSceneNames.Contains(mapRef.SceneName))
            {
                LoggingUtilities.VerboseLog(ConsoleColor.DarkRed,
                    $"Skipped over scene with key {mapRef.SceneName} because it contains characters that are blacklisted.");
                continue;
            }

            sceneList._assets.Add(new AddressableDataCache.AssetData
            {
                Asset = mapRef.SceneData._sceneRef,
                Key = mapRef.SceneName
            });

            Resources._assetList.Add(
                new Resources.LoadLoadedItem(new AssetReference(mapRef.SceneData.name))
                {
                    Key = mapRef.SceneData.name
                });

            Mod.Logger.Msg(ConsoleColor.DarkGreen, $"New custom stage registered : Key: {mapRef.SceneName}");
        }

        data = sceneList;
    }
}

[HarmonyPatch(typeof(LoadScreenDisplayHandler), nameof(LoadScreenDisplayHandler.SetSubTitle))]
internal static class SetSubTitlePatch
{
    private static bool Prefix(LoadScreenDisplayHandler __instance, ref string name)
    {
        if (!CustomAddressableRegistration.IsModdedKey(name)) return true;
        __instance._subTitle.GetComponent<TextMeshProUGUI>().text = name;
        
        return false;
    }
}
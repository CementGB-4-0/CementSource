using CementGB.CustomContent;
using CementGB.Modules.CustomContent.CustomMaps;
using CementGB.Modules.CustomContent.Utilities;
using HarmonyLib;
using Il2CppAudio;
using Il2CppGB.Core.Loading;
using Il2CppGB.Data.Loading;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using ConsoleColor = System.ConsoleColor;
using Resources = Il2CppGB.Core.Resources;

namespace CementGB.Patches;

[HarmonyPatch(typeof(SceneLoader), nameof(SceneLoader.OnSceneListComplete))]
internal static class OnSceneListCompletePatch
{
    private static void Postfix(SceneLoader __instance)
    {
        var sceneList = __instance._sceneList.TryCast<AddressableDataCache>();

        if (!sceneList || sceneList == null)
        {
            return;
        }

        foreach (var mapRef in CustomAddressableRegistration.CustomMaps)
        {
            if (!mapRef.IsValid || mapRef.SceneData == null)
                continue;

            if (!mapRef.SceneData._audioConfig)
            {
                mapRef.SceneData._audioConfig = ScriptableObject.CreateInstance<SceneAudioConfig>();
                mapRef.SceneData._audioConfig.MakePersistent();
            }

            mapRef.SceneData._audioConfig.audioMixer = MixerFinder.MainMixer;
            if (Mathf.Approximately(mapRef.SceneData._audioConfig.musicData.maxVolume, 1f))
                mapRef.SceneData._audioConfig.musicData.maxVolume = 0.15f;

            var sceneDataRef = new AssetReference(mapRef.SceneData.name);

            Resources._assetList.Add(new Resources.LoadLoadedItem(sceneDataRef) { Key = mapRef.SceneData.name });
            sceneList._assets.Add(new AddressableDataCache.AssetData { Asset = sceneDataRef, Key = mapRef.SceneName });

            Mod.Logger.Msg(
                ConsoleColor.DarkGreen,
                $"New custom stage registered in SceneLoader : Key: {mapRef.SceneName}");
        }
    }
}

[HarmonyPatch(typeof(LoadScreenDisplayHandler), nameof(LoadScreenDisplayHandler.SetSubTitle))]
internal static class SetSubTitlePatch
{
    private static bool Prefix(LoadScreenDisplayHandler __instance, ref string name)
    {
        if (!CustomAddressableRegistration.IsModdedKey(name))
        {
            return true;
        }

        __instance._subTitle.GetComponent<TextMeshProUGUI>().text = name;

        return false;
    }
}
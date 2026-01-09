using CementGB.Modules.CustomContent.Utilities;
using HarmonyLib;
using Il2CppAudio;
using Il2CppGB.Core.Loading;
using Il2CppGB.Data.Loading;
using Il2CppTMPro;
using UnityEngine;
using ConsoleColor = System.ConsoleColor;
using Resources = Il2CppGB.Core.Resources;

namespace CementGB.Modules.CustomContent.Patches;

[HarmonyPatch(typeof(SceneLoadTask), nameof(SceneLoadTask.OnDataLoaded))]
internal static class SceneLoadTask_OnDataLoadedPatch
{
    private static void Postfix(SceneLoadTask __instance)
    {
        var sceneData = __instance._sceneData;
        if (!sceneData._audioConfig)
        {
            sceneData._audioConfig = ScriptableObject.CreateInstance<SceneAudioConfig>();
            sceneData._audioConfig.MakePersistent();
        }

        sceneData._audioConfig.audioMixer = MixerFinder.MainMixer;
        if (Mathf.Approximately(sceneData._audioConfig.musicData.maxVolume, 1f))
            sceneData._audioConfig.musicData.maxVolume = 0.15f;
    }
}

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
            if (!mapRef.IsValid)
                continue;
            var sceneDataRef = mapRef.RetrieveReferenceOfKey($"{mapRef.SceneName}-Data");
            Resources._assetList.Add(new Resources.LoadObject<SceneData>(sceneDataRef));
            sceneList._assets.Add(new AddressableDataCache.AssetData { Asset = sceneDataRef, Key = mapRef.SceneName });

            CustomContentModule.Logger?.Msg(
                ConsoleColor.Green,
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
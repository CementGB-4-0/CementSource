using HarmonyLib;
using Il2CppAudio;
using Il2CppCoreNet;
using Il2CppGB.Core;
using Il2CppGB.Core.Loading;
using Il2CppGB.Data.Loading;
using Il2CppGB.Setup;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.Audio;
using ConsoleColor = System.ConsoleColor;
using NetworkManager = UnityEngine.Networking.NetworkManager;
using Object = Il2CppSystem.Object;
using Resources = Il2CppGB.Core.Resources;

namespace CementGB.Modules.CustomContent.Patches;

[HarmonyPatch(typeof(GlobalSceneLoader), nameof(GlobalSceneLoader.DisplaySplashScreen))]
internal static class GlobalSceneLoader_DisplaySplashScreen
{
    private static bool Prefix(GlobalSceneLoader __instance)
    {
        return CementPreferences.SkipSplashes;
    }
}

[HarmonyPatch(typeof(SceneLoader.NetworkLoading), nameof(SceneLoader.NetworkLoading.ActivateScene))]
internal static class ActivateScenePatch
{
    private static bool Prefix(SceneLoader.NetworkLoading __instance)
    {
        if (__instance._loadingLevel?._sceneInstance?.m_Operation == null)
        {
            __instance.CompleteLoad();
            var bundles = UnityEngine.Resources.FindObjectsOfTypeAll<AssetBundle>();
            foreach (var bundle in bundles)
            {
                if (bundle.name.Contains("unitybuiltinshaders")) bundle.Unload(false);
            }

            /*CustomContentModule.Logger?.BigError(
                $"UNCAUGHT BUNDLE LOAD ERROR OCCURRED HERE, FALLING BACK TO: {CementPreferences.FallbackMap}");
            NetworkManager.singleton.ServerChangeScene(CementPreferences.FallbackMap);*/
            CustomContentModule.Logger?.BigError(
                $"UNCAUGHT BUNDLE LOAD ERROR OCCURRED HERE, RETRYING: {__instance._sceneLoader.CurrentKey}");
            NetworkManager.singleton.ServerChangeScene(__instance._sceneLoader.CurrentKey);
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(AudioMixerSnapshot), nameof(AudioMixerSnapshot.TransitionTo))]
internal static class TransitionToPatch
{
    private static bool Prefix(AudioMixerSnapshot __instance)
    {
        if (__instance.audioMixer == null)
        {
            UnityEngine.Object.Destroy(__instance);
            return false;
        }

        return __instance.audioMixer != null;
    }
}

[HarmonyPatch(typeof(SceneLoader), nameof(SceneLoader.OnSceneLoaded))]
internal static class OnSceneLoadedPatch
{
    private static void Postfix(SceneLoader __instance)
    {
        var mixers = UnityEngine.Resources.FindObjectsOfTypeAll<AudioMixer>();
        var mixerGroups = UnityEngine.Resources.FindObjectsOfTypeAll<AudioMixerGroup>();
        var goodMixer = mixers.First();
        if (__instance._sceneData._audioConfig == null)
            __instance._sceneData._audioConfig = ScriptableObject.CreateInstance<SceneAudioConfig>();
        var prevMixer = __instance._sceneData._audioConfig.audioMixer;
        if (prevMixer == goodMixer) return;
        __instance._sceneData._audioConfig.audioMixer = goodMixer;
        foreach (var mixerGroup in mixerGroups)
        {
            if (mixerGroup.audioMixer == prevMixer)
            {
                UnityEngine.Object.Destroy(mixerGroup);
            }
        }

        UnityEngine.Object.Destroy(prevMixer);
        __instance._sceneData._audioConfig.musicData.bSide ??= __instance._sceneData._audioConfig.musicData.aSide;
        __instance._sceneData._audioConfig.musicData.drums ??= __instance._sceneData._audioConfig.musicData.aSide;
    }
}

[HarmonyPatch(typeof(SceneLoader), nameof(SceneLoader.OnSceneListComplete))]
internal static class OnSceneListCompletePatch
{
    private static void Postfix(SceneLoader __instance, Object data)
    {
        var sceneList = data.TryCast<AddressableDataCache>();

        if (!sceneList || sceneList == null)
        {
            return;
        }

        foreach (var mapRef in CustomAddressableRegistration.CustomMaps)
        {
            if (!mapRef.IsValid)
                continue;

            Resources._assetList.Add(new Resources.LoadLoadedItem(mapRef.SceneData));
            sceneList._assets.Add(new AddressableDataCache.AssetData
                { Asset = mapRef.SceneData, Key = mapRef.SceneName });

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
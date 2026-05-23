using GBMDK;
using HarmonyLib;
using Il2CppAudio;
using Il2CppGB.Core.Loading;
using Il2CppGB.Data.Loading;
using Il2CppGB.Gamemodes;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.Audio;
using ConsoleColor = System.ConsoleColor;
using NetworkManager = UnityEngine.Networking.NetworkManager;
using Object = Il2CppSystem.Object;
using Resources = Il2CppGB.Core.Resources;

namespace CementGB.Modules.CustomContent.Patches;

[HarmonyPatch(typeof(SceneLoader.NetworkLoading), nameof(SceneLoader.NetworkLoading.ActivateScene))]
internal static class ActivateScenePatch
{
    private static bool Prefix(SceneLoader.NetworkLoading __instance)
    {
        if (__instance._loadingLevel?._sceneInstance?.m_Operation == null)
        {
            __instance.CompleteLoad();
            var bundles = UnityEngine.Resources.FindObjectsOfTypeAll<AssetBundle>()
                .Where(b => b.name.Contains("unitybuiltinshaders")).ToArray();
            if (bundles.Length > 1)
            {
                foreach (var b in bundles[1..])
                {
                    b.Unload(false);
                }
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
        // Make sure mixers are managed properly
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

        ConstructAndAttachWavesData(__instance);
    }

    private static void ConstructAndAttachWavesData(SceneLoader __instance)
    {
        // Construct WavesData at runtime if wrapper provided/gamemode is forced to Waves to lessen GBMDK workload
        // TODO: Make more reliable way to get SceneInfo from SceneData

        var mapRef =
            CustomAddressableRegistration.CustomMaps.FirstOrDefault(x =>
                __instance._sceneData.name.StartsWith(x.SceneName));

        if (mapRef is not { SceneInfo.allowedGamemodes: not null } ||
            (!mapRef.SceneInfo.allowedGamemodes.Get().HasFlag(GameModeEnum.Waves) && Mod.ModeArg != "waves")) return;
        var wavesDataWrapperFld = mapRef.SceneInfo.wavesData;
        if (wavesDataWrapperFld == null) return;
        var wavesDataWrapper = wavesDataWrapperFld.Get() ?? new WavesDataWrapper();
        wavesDataWrapper.createWavesData = true;

        var wavesData = wavesDataWrapper.Result;
        __instance._sceneData._wavesData ??= wavesData;
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
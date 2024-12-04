using CementGB.Mod.Utilities;
using Il2CppGB.Core;
using Il2CppGB.Core.Loading;
using Il2CppGB.Data.Loading;
using MelonLoader;
using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace CementGB.Mod.Patches;

[HarmonyLib.HarmonyPatch(typeof(SceneLoader), nameof(SceneLoader.LoadScene))]
internal static class LoadScenePatch
{
    private static bool Prefix(SceneLoader __instance, string key, LoadSceneMode loadMode, bool activateOnLoad, int priority, ref SceneLoadTask __result)
    {
        try
        {
            if (__instance._sceneList[key] != null || !AssetUtilities.IsModdedKey(key))
            {
                return true; // Scene is vanilla, just do normal behavior
            }

            Melon<Mod>.Logger.Msg("Loading custom scene. . .");

            var assetReference = AssetUtilities.CreateModdedAssetReference(key);
            if (assetReference == null)
            {
                Melon<Mod>.Logger.Error($"Custom scene AssetReference is null! Key probably belongs to an addressable that does not exist or wasn't loaded. Key: {key}");
                __result = null;
                return false;
            }

            if (!assetReference.RuntimeKeyIsValid())
            {
                Melon<Mod>.Logger.Error("[SCENELOAD] LoadScene: Failed validation | Key: {0} | Ref: {1}",
                [
                    key,
                    assetReference != null
                ]);
                __result = null;
                return false;
            }

            __instance._currentKey = key;
            __instance._currentScene = key;
            SceneLoadTask sceneLoadTask = new((SceneLoadTask.OnLoaded)__instance.OnSceneLoaded, loadMode, activateOnLoad, priority);

            Melon<Mod>.Logger.Msg($"[SCENELOAD] LoadScene: Start loading | Key: {__instance._currentKey}");
            sceneLoadTask._sceneDataRef = assetReference;
            var sceneData = Addressables.LoadAssetAsync<SceneData>(key + "-Data").Acquire();
            sceneData.WaitForCompletion();

            sceneLoadTask._sceneData = sceneData.Result;
            sceneLoadTask._sceneLoading = Addressables.LoadSceneAsync(key).Acquire();
            sceneLoadTask._sceneLoading.WaitForCompletion();
            sceneLoadTask._sceneInstance = sceneLoadTask._sceneLoading.Result;
            if (sceneLoadTask._activateOnLoad)
            {
                sceneLoadTask._status = AsyncOperationStatus.Succeeded;
            }

            __instance.OnSceneLoaded(sceneLoadTask);
            sceneLoadTask._dataLoading = null;
            Global.Instance.LevelLoadSystem.LoadingScreen.LoadTasks.Add(new ICompleteTracker(sceneLoadTask.Pointer));

            __result = sceneLoadTask;
            sceneData.Release();
            return false;
        }
        catch (Exception e)
        {
            LoggingUtilities.Logger.Error($"Error occurred loading scene {key}: {e}");
            __instance.LoadMainMenuWithLoadingScreen();
            return false;
        }
    }
}
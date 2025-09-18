using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using CementGB.Mod.CustomContent;
using CementGB.Mod.Utilities;
using CementGB.Modules.CustomContent.Utilities;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Linq;
using MelonLoader;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CementGB.Modules.CustomContent;

public static class AddressableShaderCache
{
    private static readonly Dictionary<string, Shader> CachedShaders = [];

    static AddressableShaderCache()
    {
        SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>)OnSceneWasLoaded);
    }

    private static void OnSceneWasLoaded(Scene scene, LoadSceneMode mode)
    {
        if (CustomAddressableRegistration.IsModdedKey(scene.name))
            _ = MelonCoroutines.Start(ReloadAddressableShaders());
    }

    private static IEnumerator ReloadAddressableShaders(GameObject? parent = null)
    {
        yield return new WaitForEndOfFrame();
        LoggingUtilities.VerboseLog(ConsoleColor.DarkYellow, "Reloading Addressable shaders. . .");
        Il2CppArrayBase<MeshRenderer> renderers;
        if (!parent)
        {
            renderers = Object.FindObjectsOfType<MeshRenderer>();
        }
        else
        {
            var rendList = new List<MeshRenderer>();
            rendList.AddRange(parent.GetComponents<MeshRenderer>());
            rendList.AddRange(parent.GetComponentsInChildren<MeshRenderer>());

            renderers = new Il2CppReferenceArray<MeshRenderer>([.. rendList]);
        }

        foreach (var meshRenderer in renderers)
        {
            foreach (var material in meshRenderer.materials)
            {
                if (CachedShaders.TryGetValue(material.shader.name, out var shader))
                    material.shader = shader;
            }
        }

        LoggingUtilities.VerboseLog(ConsoleColor.DarkGreen, "Reloaded Addressable shaders!");
    }

    private static IEnumerator InitCacheShaders()
    {
        Mod.Mod.Logger.Msg("Caching Addressable game shaders, please wait. . .");
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        foreach (var locator in Addressables.ResourceLocators.ToArray())
        {
            var locatorKeys = locator.Keys.ToList();
            var handle = Addressables.LoadResourceLocationsAsync(
                locatorKeys.Cast<Il2CppSystem.Collections.Generic.IList<Il2CppSystem.Object>>(),
                Addressables.MergeMode.Union,
                Il2CppType.Of<Shader>());
            yield return handle.HandleAsynchronousAddressableOperation();

            if (!AssetUtilities.IsHandleSuccess(handle))
            {
                LoggingUtilities.VerboseLog(
                    ConsoleColor.DarkYellow,
                    $"Addressable resource locator of ID {locator.LocatorId} could not be loaded, not caching. . .");
                continue;
            }

            var result = handle.Result.Cast<Il2CppSystem.Collections.Generic.List<IResourceLocation>>();
            foreach (var location in result)
            {
                if (CachedShaders.ContainsKey(location.PrimaryKey)) continue;

                var assetHandle = Addressables.LoadAssetAsync<Shader>(location);
                yield return assetHandle.HandleAsynchronousAddressableOperation();

                if (!AssetUtilities.IsHandleSuccess(assetHandle))
                {
                    LoggingUtilities.VerboseLog(
                        ConsoleColor.DarkYellow,
                        $"Addressable shader of key {location.PrimaryKey} could not be loaded, not caching. . .");
                    continue;
                }

                assetHandle.Result.MakePersistent();
                CachedShaders.Add(location.PrimaryKey, assetHandle.Result);

                assetHandle.Release();
            }

            handle.Release();
        }

        stopwatch.Stop();
        Mod.Mod.Logger.Msg(
            ConsoleColor.Green,
            $"Caching Addressable game shaders done! Total time taken: {stopwatch.ElapsedMilliseconds}ms");
    }

    private static void StartShaderReload(Scene scene, LoadSceneMode mode)
    {
        _ = MelonCoroutines.Start(InitCacheShaders());
    }

    internal static void Initialize()
    {
        _ = MelonCoroutines.Start(InitCacheShaders());
        SceneManager.add_sceneLoaded((UnityAction<Scene, LoadSceneMode>)StartShaderReload);
    }
}
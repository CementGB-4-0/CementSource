using System;
using System.Diagnostics;
using CementGB.Mod.Utilities;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Linq;
using MelonLoader;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = UnityEngine.Object;

namespace CementGB.Mod.CustomContent;

public static class AddressableShaderCache
{
    private static readonly System.Collections.Generic.Dictionary<string, Shader> CachedShaders = [];

    public static System.Collections.IEnumerator ReloadAddressableShaders(GameObject parent = null)
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
            var rendList = new System.Collections.Generic.List<MeshRenderer>();
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

    private static System.Collections.IEnumerator InitCacheShaders()
    {
        Mod.Logger.Msg("Caching Addressable game shaders, please wait. . .");
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
                var assetHandle = Addressables.LoadAssetAsync<Shader>(location);
                yield return assetHandle.HandleAsynchronousAddressableOperation();

                if (!AssetUtilities.IsHandleSuccess(assetHandle))
                {
                    LoggingUtilities.VerboseLog(
                        ConsoleColor.DarkYellow,
                        $"Addressable shader of key {location.PrimaryKey} could not be loaded, not caching. . .");
                    continue;
                }
                
                if (!CachedShaders.ContainsKey(location.PrimaryKey))
                    CachedShaders.Add(location.PrimaryKey, assetHandle.Result);

                assetHandle.Release();
            }

            handle.Release();
        }
        
        stopwatch.Stop();
        Mod.Logger.Msg(
            ConsoleColor.Green,
            $"Caching Addressable game shaders done! Total time taken: {stopwatch.Elapsed}");
    }

    internal static void Initialize()
    {
        MelonCoroutines.Start(InitCacheShaders());
    }
}
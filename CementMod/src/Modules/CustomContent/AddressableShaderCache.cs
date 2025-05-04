using System;
using System.Diagnostics;
using CementGB.Mod.Utilities;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;
using Il2CppSystem.Linq;
using Il2CppSystem.Net;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = UnityEngine.Object;

namespace CementGB.Mod.CustomContent;

public static class AddressableShaderCache
{
    private static readonly System.Collections.Generic.Dictionary<string, Shader> _cachedShaders = [];
    
    public static System.Collections.IEnumerator ReloadAddressableShaders(GameObject parent = null)
    {
        yield return new WaitForEndOfFrame();
        Mod.Logger.Warning("Reloading Addressable shaders. . .");
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
                if (_cachedShaders.TryGetValue(material.shader.name, out var shader))
                    material.shader = shader;
            }
        }
    }

    private static void InitCacheShaders()
    {
        Mod.Logger.Msg("Caching Addressable game shaders, please wait. . .");
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        foreach (var locator in Addressables.ResourceLocators.ToArray())
        {
            var locatorKeys = locator.Keys.ToList();
            var handle = Addressables.LoadResourceLocationsAsync(locatorKeys.Cast<IList<Il2CppSystem.Object>>(),
                Addressables.MergeMode.Union, Il2CppType.Of<Shader>());
            var success = handle.HandleSynchronousAddressableOperation();
            if (!success)
            {
                LoggingUtilities.VerboseLog(ConsoleColor.DarkYellow, $"Addressable ResourceLocator of id {locator.LocatorId} could not be loaded, not caching. . ."); 
                continue;
            }

            var result = handle.Result.Cast<List<IResourceLocation>>();
            foreach (var location in result)
            {
                var assetHandle = Addressables.LoadAssetAsync<Shader>(location);
                var success2 = handle.HandleSynchronousAddressableOperation(location.PrimaryKey);
                if (!success2)
                {
                    LoggingUtilities.VerboseLog(ConsoleColor.DarkYellow, $"Addressable shader {location.PrimaryKey} could not be loaded, not caching. . .");
                    continue;
                }
                
                if (!_cachedShaders.ContainsKey(location.PrimaryKey))
                    _cachedShaders.Add(location.PrimaryKey, assetHandle.Result);

                assetHandle.Release();
            }

            handle.Release();
        }
    }

    internal static void Initialize()
    {
        InitCacheShaders();
    }
}
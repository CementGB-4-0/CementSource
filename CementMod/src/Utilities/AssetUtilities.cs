using System;
using System.Collections;
using System.Diagnostics;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;
using Il2CppSystem.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = Il2CppSystem.Object;

namespace CementGB.Mod.Utilities;

public static class AssetUtilities
{
    /// <summary>
    ///     Shorthand for loading an AssetBundle's asset by name and type in way that prevents it from being garbage collected.
    /// </summary>
    /// <typeparam name="T">The type of the asset to load.</typeparam>
    /// <param name="bundle">The bundle to load the asset from.</param>
    /// <param name="name">The exact name of the asset to load.</param>
    /// <returns>The loaded asset with <c>hideFlags</c> set to <c>HideFlags.DontUnloadUnusedAsset</c></returns>
    public static T LoadPersistentAsset<T>(this AssetBundle bundle, string name) where T : UnityEngine.Object
    {
        var asset = bundle.LoadAsset(name);

        if (asset != null)
        {
            asset.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return asset.TryCast<T>();
        }

        return null;
    }

    /// <summary>
    ///     Shorthand for loading an AssetBundle's asset by name and type in way that prevents it from being garbage collected.
    ///     This method will execute the callback when async loading is complete.
    /// </summary>
    /// <typeparam name="T">The type of the asset to load.</typeparam>
    /// <param name="bundle">The bundle to load the asset from.</param>
    /// <param name="name">The exact name of the asset to load.</param>
    /// <param name="onLoaded">The callback to execute once the asset loads. Takes the loaded asset as a parameter.</param>
    public static void LoadPersistentAssetAsync<T>(this AssetBundle bundle, string name, Action<T> onLoaded)
        where T : UnityEngine.Object
    {
        var request = bundle.LoadAssetAsync<T>(name);

        request.add_completed((Il2CppSystem.Action<AsyncOperation>)(a =>
        {
            if (request.asset == null)
            {
                return;
            }

            var result = request.asset.TryCast<T>();
            if (result == null)
            {
                return;
            }

            result.MakePersistent();
            onLoaded?.Invoke(result);
        }));
    }

    public static void LoadAllAssetsPersistentAsync<T>(this AssetBundle bundle, Action<T> onLoaded)
        where T : UnityEngine.Object
    {
        var request = bundle.LoadAllAssetsAsync<T>();

        request.add_completed(new Action<AsyncOperation>(a =>
        {
            if (request.asset == null)
            {
                return;
            }

            var result = request.asset.TryCast<T>();
            if (result == null)
            {
                return;
            }

            result.MakePersistent();
            onLoaded?.Invoke(result);
        }));
    }

    private static void CacheShaders()
    {
        Mod.Logger.Msg("Caching Addressable game shaders, please wait. . .");
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        foreach (var locator in Addressables.ResourceLocators.ToArray())
        {
            var locatorKeys = locator.Keys.ToList();
            var handle = Addressables.LoadResourceLocations(locatorKeys.Cast<IList<Object>>(),
                Addressables.MergeMode.Union, Il2CppType.Of<Shader>()).Acquire();
            handle.WaitForCompletion();

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                LoggingUtilities.VerboseLog(ConsoleColor.DarkRed,
                    $"Shader cache failed for locator (ID \"{locator.LocatorId}\")! : OperationException \"{handle.OperationException.ToString()}\"");
                stopwatch.Reset();
                continue;
            }

            if (handle.Result == null)
            {
                LoggingUtilities.VerboseLog(ConsoleColor.DarkRed,
                    $"Shader cache returned no result for locator (ID \"{locator.LocatorId}\")! : OperationException \"{handle.OperationException?.ToString() ?? "NONE"}\"");
                stopwatch.Reset();
                continue;
            }

            var result = handle.Result.Cast<List<IResourceLocation>>();
            foreach (var location in result)
            {
                var assetHandle = Addressables.LoadAssetAsync<Shader>(location).Acquire();
                assetHandle.WaitForCompletion();

                if (assetHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    LoggingUtilities.VerboseLog(ConsoleColor.DarkRed,
                        $"Shader cache ASSET HANDLE failed for location (Key \"{location.PrimaryKey}\")! : OperationException \"{assetHandle.OperationException.ToString() ?? "NONE"}\"");
                    assetHandle.Release();
                    continue;
                }

                if (assetHandle.Result == null)
                {
                    LoggingUtilities.VerboseLog(ConsoleColor.DarkRed,
                        $"Shader cache ASSET HANDLE returned no result for location (Key \"{location.PrimaryKey}\")! : OperationException \"{assetHandle.OperationException.ToString() ?? "NONE"}\"");
                    assetHandle.Release();
                    continue;
                }

                assetHandle.Result.hideFlags = HideFlags.DontUnloadUnusedAsset;

                if (!_cachedShaders.ContainsKey(assetHandle.Result.name))
                {
                    _cachedShaders.Add(assetHandle.Result.name, assetHandle.Result);
                }

                assetHandle.Release();
            }

            handle.Release();
        }

        stopwatch.Stop();
        Mod.Logger.Msg(ConsoleColor.Green, $"Shader caching done! Total time taken: {stopwatch.Elapsed}");
    }

    /*     /// <summary>
        /// A hacky Coroutine that calls Shader.Find on all materials on the passed GameObject and children, or all objects in the scene if not specified, and passes in the name of the current shader, setting the Material.shader value.
        /// </summary>
        public static IEnumerator RefindMaterials(GameObject parent=null)
        {
            yield return new WaitForEndOfFrame();
            Mod.Logger.Warning("Refreshing materials. . .");
            Il2CppArrayBase<MeshRenderer> renderers;
            if (parent == null)
                renderers = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();
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
                    material.shader = Shader.Find(material.shader.name);
                }
            }
        } */

    public static IEnumerator ReloadAddressableShaders(GameObject parent = null)
    {
        yield return new WaitForEndOfFrame();
        Mod.Logger.Warning("Reloading Addressable shaders. . .");
        Il2CppArrayBase<MeshRenderer> renderers;
        if (parent == null)
        {
            renderers = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();
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
                if (_cachedShaders.ContainsKey(material.shader.name))
                {
                    material.shader = _cachedShaders[material.shader.name];
                }
            }
        }
    }
}
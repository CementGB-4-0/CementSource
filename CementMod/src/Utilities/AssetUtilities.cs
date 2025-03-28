using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;
using Il2CppSystem.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = Il2CppSystem.Object;

namespace CementGB.Mod.Utilities;

public static class AssetUtilities
{
    private static readonly System.Collections.Generic.Dictionary<string, List<Object>> _packAddressableKeys = [];
    private static readonly System.Collections.Generic.List<IResourceLocator> _moddedResourceLocators = [];

    private static readonly System.Collections.Generic.Dictionary<string, Shader> _cachedShaders = [];

    public static ReadOnlyDictionary<string, Object[]> PackAddressableKeys // { modName: addressableKeys }
    {
        get
        {
            var dict = new System.Collections.Generic.Dictionary<string, Object[]>();

            foreach (var kvp in _packAddressableKeys)
            {
                dict.Add(kvp.Key, kvp.Value.ToArray());
            }

            return new ReadOnlyDictionary<string, Object[]>(dict);
        }
    }

    public static IResourceLocator[] ModdedResourceLocators => [.. _moddedResourceLocators];

    /// <summary>
    ///     Fires when a modded addressable catalog is registered into the game, after its keys are added to
    ///     <see cref="PackAddressableKeys" />.
    ///     Takes the catalog path as a parameter.
    /// </summary>
    public static event Action<string> OnModdedAddressableCatalogLoaded;

    /// <summary>
    ///     Shorthand for loading an AssetBundle's asset by name and type in way that prevents it from being garbage collected.
    /// </summary>
    /// <typeparam name="T">The type of the asset to load.</typeparam>
    /// <param name="bundle">The bundle to load the asset from.</param>
    /// <param name="name">The exact name of the asset to load.</param>
    /// <returns>The loaded asset with <c>hideFlags</c> set to <c>HideFlags.DontUnloadUnusedAsset</c></returns>
    public static T LoadPersistentAsset<T>(this AssetBundle bundle, string name) where T : UnityEngine.Object
    {
        var asset = bundle.LoadAsset<T>(name);

        if (asset != null)
        {
            asset.MakePersistent();
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

    /// <summary>
    ///     Gets all custom-loaded IResourceLocations of a certain result type. Used to iterate through and find custom content
    ///     addressable keys depending on type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns>
    ///     An array containing IResourceLocations that, if loaded, will result in the passed type. Will return an array
    ///     even if empty.
    /// </returns>
    public static IResourceLocation[] GetAllModdedResourceLocationsOfType<T>() where T : Object
    {
        System.Collections.Generic.List<IResourceLocation> ret = [];
        List<Object> allModdedKeys = new();
        foreach (var value in _packAddressableKeys)
        {
            allModdedKeys.AddRange(value.Value.Cast<IEnumerable<Object>>());
        }

        var allModdedLocations = Addressables
            .LoadResourceLocations(allModdedKeys.Cast<IList<Object>>(), Addressables.MergeMode.Union).Acquire();
        allModdedLocations.WaitForCompletion();

        if (allModdedLocations.Status != AsyncOperationStatus.Succeeded)
        {
            Mod.Logger.Error(
                $"Failed to load modded resource locations! OperationException: {allModdedLocations.OperationException}");
            allModdedLocations.Release();
            return [];
        }

        var result = allModdedLocations.Result.Cast<List<IResourceLocation>>();
        foreach (var location in result)
        {
            if (location.ResourceType == Il2CppType.Of<T>())
            {
                ret.Add(location);
            }
        }

        if (ret.Count == 0)
        {
            LoggingUtilities.VerboseLog(ConsoleColor.DarkRed,
                $"Returned empty array! Type {typeof(T)} probably wasn't found in addressables.");
        }

        allModdedLocations.Release();
        return [.. ret];
    }

    public static bool IsModdedKey(string key)
    {
        foreach (var moddedKeyish in _moddedResourceLocators)
        {
            if (moddedKeyish.Keys.Contains(key))
            {
                return true;
            }
        }

        return false;
    }

    internal static void InitializeAddressables()
    {
        _packAddressableKeys.Clear();
        Mod.Logger.Msg("Starting registration of modded Addressable content catalogs. . .");

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        foreach (var contentMod in Directory.EnumerateDirectories(Mod.CustomContentPath, "*",
                     SearchOption.AllDirectories))
        {
            var aaPath = Path.Combine(contentMod, "aa");

            if (!Directory.Exists(aaPath))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(aaPath, "catalog_*.json", SearchOption.AllDirectories))
            {
                var catalogPath = file;

                var resourceLocatorHandle = Addressables.LoadContentCatalog(catalogPath).Acquire();
                var addressablePackName = Path.GetDirectoryName(aaPath);
                if (string.IsNullOrWhiteSpace(addressablePackName))
                {
                    continue;
                }

                resourceLocatorHandle.WaitForCompletion();
                if (resourceLocatorHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Mod.Logger.Error(
                        $"Failed to load Addressable content catalog for \"{addressablePackName}\". OperationException: {resourceLocatorHandle.OperationException.ToString()}");
                    resourceLocatorHandle.Release();
                    continue;
                }

                var resourceLocator = resourceLocatorHandle.Result;
                if (resourceLocator == null)
                {
                    Mod.Logger.Error(
                        $"Handle for modded Addressable content catalog returned no result. OperationException: {resourceLocatorHandle.OperationException.ToString()}");
                    resourceLocatorHandle.Release();
                    continue;
                }

                Addressables.AddResourceLocator(resourceLocator);
                _moddedResourceLocators.Add(resourceLocator);
                _packAddressableKeys.Add(Path.GetFileNameWithoutExtension(contentMod), resourceLocator.Keys.ToList());

                foreach (var key in resourceLocator.Keys.ToArray())
                {
                    LoggingUtilities.VerboseLog($"{addressablePackName} : {key.ToString()}");
                }

                Mod.Logger.Msg(ConsoleColor.Green, $"Content catalog for \"{addressablePackName}\" loaded OK");
                OnModdedAddressableCatalogLoaded?.Invoke(catalogPath);
                resourceLocatorHandle.Release();
            }
        }

        stopwatch.Stop();
        Mod.Logger.Msg(ConsoleColor.Green, $"Done custom content catalogs! Total time taken: {stopwatch.Elapsed}");
        CacheShaders();
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
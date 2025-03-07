using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Il2CppSystem.Linq;
using CementGB.Mod.Utilities;
using Il2CppInterop.Runtime;
using Il2CppSystem.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = Il2CppSystem.Object;

namespace CementGB.Mod.CustomContent;

public static class CustomAddressableRegistration
{
    private static readonly System.Collections.Generic.Dictionary<string, List<Object>> _packAddressableKeys = [];
    private static readonly System.Collections.Generic.List<IResourceLocator> _moddedResourceLocators = [];

    private static readonly System.Collections.Generic.Dictionary<string, Shader> _cachedShaders = [];

    public static ReadOnlyDictionary<string, Object[]> PackAddressableKeys // { modName: addressableKeys }
    {
        get
        {
            var dict = _packAddressableKeys
                .ToDictionary<System.Collections.Generic.KeyValuePair<string, List<Object>>, string, Object[]>(
                    kvp => kvp.Key, kvp => kvp.Value.ToArray());

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
                $"Returned empty array! Type {typeof(T)} probably wasn't found in Addressables.");
        }

        allModdedLocations.Release();
        return [.. ret];
    }
    
    internal static void Initialize()
    {
        
    }

    internal static void InitializeAddressables()
    {
        _packAddressableKeys.Clear();
        Mod.Logger.Msg("Starting registration of modded Addressable content catalogs. . .");

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        foreach (var contentMod in Directory.EnumerateDirectories(Mod.CustomContentPath,  "*", SearchOption.AllDirectories))
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
    }

    public static bool IsModdedKey(string key)
    {
        for (var i = 0; i < _moddedResourceLocators.Count; i++)
        {
            var moddedKeyish = _moddedResourceLocators[i];
            if (moddedKeyish.Keys.Contains(key))
            {
                return true;
            }
        }

        return false;
    }
}
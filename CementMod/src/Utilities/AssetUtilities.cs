using Il2CppInterop.Runtime;
using Il2CppSystem.Linq;
using MelonLoader;
using System;
using Il2CppSystem.Collections.Generic;
using Il2CppSystem.Collections.ObjectModel;
using System.IO;
using System.Linq;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace CementGB.Mod.Utilities;

public static class AssetUtilities
{
    /// <summary>
    /// Fires when a modded addressable catalog is registered into the game, after its keys are added to <see cref="PackAddressableKeys"/>.
    /// Takes the catalog path as a parameter.
    /// </summary>
    public static event Action<string> OnModdedAddressableCatalogLoaded;

    /// <summary>
    /// Creates an AssetReference with a new Guid referring to the passed Addressable key. The key does not need to refer to a modded addressable, however this method is designed for that purpose.
    /// </summary>
    /// <param name="key">The key to refer to when creating the <see cref="AssetReference">.</param>
    /// <returns>The created <see cref="AssetReference"/>.</returns>
    public static AssetReference CreateModdedAssetReference(string key)
    {
        AssetReference assetReference = new(key);
        assetReference.m_AssetGUID = Guid.NewGuid().ToString() + assetReference.m_SubObjectName;
        return assetReference;
    }

    /// <summary>
    /// Gets all custom-loaded IResourceLocations of a certain result type. Used to iterate through and find custom content addressable keys depending on type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns>An array containing IResourceLocations that, if loaded, will result in the passed type. Will return an array even if empty.</returns>
    public static IResourceLocation[] GetAllModdedResourceLocationsOfType<T>() where T : Il2CppSystem.Object
    {
        List<IResourceLocation> ret = new();
        List<Il2CppSystem.Object> allModdedKeys = new();
        foreach (var value in _packAddressableKeys)
            allModdedKeys.AddRange(value.Value.Cast<IEnumerable<Il2CppSystem.Object>>());

        var allModdedLocations = Addressables.LoadResourceLocationsAsync(allModdedKeys.Cast<IList<Il2CppSystem.Object>>(), Addressables.MergeMode.Union).Acquire();
        allModdedLocations.WaitForCompletion();
        if (allModdedLocations.Status != AsyncOperationStatus.Succeeded)
        {
            Mod.Logger.Error($"Failed to load modded resource locations! Exception: {allModdedLocations.OperationException}");
            return [];
        }

        var result = allModdedLocations.Result.Cast<List<IResourceLocation>>();
        foreach (var location in result)
        {
            if (location.ResourceType == Il2CppType.Of<T>())
                ret.Add(location);
        }

        if (ret.Count == 0)
            LoggingUtilities.VerboseLog(ConsoleColor.DarkRed, "Returned empty array! Type probably wasn't found in addressables.");
        return [.. ret];
    }

    public static bool IsModdedKey(string key)
    {
        foreach (var moddedKeyish in _moddedResourceLocators)
        {
            if (moddedKeyish.Keys.Contains(key))
                return true;
        }
        return false;
    }

    /*
    public static ReadOnlyDictionary<string, Il2CppSystem.Object[]> PackAddressableKeys // { catalogPath: addressableKeys }
    {
        get
        {
            var dict = new Dictionary<string, Il2CppSystem.Object[]>();

            foreach (var kvp in _packAddressableKeys)
            {
                dict.Add(kvp.Key, kvp.Value.ToArray());
            }

            return new();
        }
    }
    */
    private static readonly Dictionary<string, List<Il2CppSystem.Object>> _packAddressableKeys = new();

    public static ReadOnlyCollection<IResourceLocator> ModdedResourceLocators => new(_moddedResourceLocators.Cast<IList<IResourceLocator>>());
    private static readonly List<IResourceLocator> _moddedResourceLocators = new();

    internal static void LoadCCCatalogs()
    {
        _packAddressableKeys.Clear();
        Melon<Mod>.Logger.Msg("Starting registration of modded Addressable content catalogs. . .");

        foreach (var dir in Directory.GetDirectories(Mod.CustomContentPath))
        {
            var aaPath = Path.Combine(dir, "aa");
            if (!Directory.Exists(aaPath))
            {
                Mod.Logger.Warning($"Folder {dir} has no \"aa\" folder! Custom Addressables, if any, will not be loaded.");
                continue;
            }

            var catalogPath = Path.Combine(aaPath, "catalog.json");
            if (File.Exists(catalogPath))
            {
                var resourceLocatorHandle = Addressables.LoadContentCatalogAsync(catalogPath).Acquire();
                var addressablePackName = Path.GetDirectoryName(catalogPath);
                if (string.IsNullOrWhiteSpace(addressablePackName)) continue;

                resourceLocatorHandle.WaitForCompletion();
                if (resourceLocatorHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Melon<Mod>.Logger.Error($"Failed to load Addressable content catalog for \"{addressablePackName}\": ", resourceLocatorHandle.OperationException);
                    continue;
                }

                var resourceLocator = resourceLocatorHandle.Result;
                if (resourceLocator is null) continue;
                _moddedResourceLocators.Add(resourceLocator);
                _packAddressableKeys.Add(catalogPath, resourceLocator.Keys.ToList());

                foreach (var key in resourceLocator.Keys.ToArray())
                {
                    LoggingUtilities.VerboseLog($"{addressablePackName} : {key.ToString()}");
                }

                Melon<Mod>.Logger.Msg(ConsoleColor.Green, $"Content catalog for \"{addressablePackName}\" loaded OK");
                OnModdedAddressableCatalogLoaded?.Invoke(catalogPath);
                resourceLocatorHandle.Release();
            }
            else
            {
                Melon<Mod>.Logger.Warning($"No catalog found in directory \"{dir}\".");
            }
        }
        Melon<Mod>.Logger.Msg(ConsoleColor.Green, "Done!");
    }
}
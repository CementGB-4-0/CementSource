using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CementGB.Mod.Utilities;
using GBMDK;
using Il2CppGB.Data.Loading;
using Il2CppInterop.Runtime;
using Il2CppSystem.Collections.Generic;
using Il2CppSystem.Linq;
using MelonLoader;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = Il2CppSystem.Object;

namespace CementGB.Mod.CustomContent;

public static class CustomAddressableRegistration
{
    public const string ModsDirectoryPropertyName = "MelonLoader.Utils.MelonEnvironment.ModsDirectory";
    
    public static event Action ContentCatalogsFinished;
    
    private static readonly System.Collections.Generic.Dictionary<string, List<Object>> _packAddressableKeys = [];
    private static readonly System.Collections.Generic.List<IResourceLocator> _moddedResourceLocators = [];
    private static readonly System.Collections.Generic.List<CustomMapRefHolder> _customMaps = [];

    public static ReadOnlyDictionary<string, string[]> PackAddressableKeys // { modName: addressableKeys }
    {
        get
        {
            var dict = new System.Collections.Generic.Dictionary<string, string[]>();
            
            foreach (var kvp in _packAddressableKeys)
            {
                var addrKeys = new List<string>();
                
                foreach (var uncastedString in kvp.Value.ToArray())
                    addrKeys.Add(uncastedString.ToString()); 
                dict.Add(kvp.Key, addrKeys.ToArray());
            }

            return new ReadOnlyDictionary<string, string[]>(dict);
        }
    }
    public static ReadOnlyCollection<IResourceLocator> ModdedResourceLocators => _moddedResourceLocators.AsReadOnly();
    public static ReadOnlyCollection<CustomMapRefHolder> CustomMaps => _customMaps.AsReadOnly();

    /// <summary>
    ///     Gets all custom-loaded IResourceLocations of a certain result type. Used to iterate through and find custom content
    ///     addressable keys depending on type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns>
    ///     An array containing IResourceLocations that, if loaded, will result in the passed type. Will return an array
    ///     even if empty.
    /// </returns>
    private static IResourceLocation[] GetAllModdedResourceLocationsOfType<T>() where T : Object
    {
        System.Collections.Generic.List<IResourceLocation> ret = [];

        foreach (var locator in _moddedResourceLocators)
        {
            foreach (var key in locator.Keys.ToArray())
            {
                var locateRes = locator.Locate(key, Il2CppType.Of<T>(), out var locatorLocations);
                LoggingUtilities.VerboseLog($"Locator of ID {locator.LocatorId} returned {locateRes.ToString().ToUpper()} locating key {key.ToString()}. . .");

                var locatorLocationsCasted = locatorLocations?.TryCast<List<IResourceLocation>>();
                if (locatorLocationsCasted == null)
                    continue;

                foreach (var location in locatorLocationsCasted.ToArray())
                {
                    if (!ret.Where((resourceLocation => resourceLocation.PrimaryKey == location.PrimaryKey)).Any())
                        ret.Add(location);
                }
            }
        }

        if (ret.Count == 0)
        {
            LoggingUtilities.VerboseLog(ConsoleColor.DarkRed,
                $"Returned empty array! Type {Il2CppType.Of<T>().ToString()} probably wasn't found in modded Addressables.");
        }

        return [.. ret];
    }
    
    public static bool IsModdedKey(string key) 
        => _moddedResourceLocators.Any(moddedKeyish => moddedKeyish.Keys.Contains(key));

    public static bool IsValidSceneDataName(string name) 
        => name.Split("-Data").Length >= 1;

    internal static void Initialize()
    {
        ContentCatalogsFinished += AddressableShaderCache.Initialize;
        ContentCatalogsFinished += () => MelonCoroutines.Start(InitializeMapReferences());
        MelonCoroutines.Start(InitializeContentCatalogs());
    }

    private static IEnumerator InitializeContentCatalogs()
    {
        _packAddressableKeys.Clear();
        Mod.Logger.Msg("Starting initialization of modded Addressable content catalogs. . .");

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        AddressablesRuntimeProperties.SetPropertyValue(ModsDirectoryPropertyName, Mod.CustomContentPath);
        LoggingUtilities.VerboseLog($"Set Addressable runtime property \"{ModsDirectoryPropertyName}\" to value \"{AddressablesRuntimeProperties.EvaluateProperty(ModsDirectoryPropertyName)}\"");
        foreach (var contentMod in Directory.EnumerateDirectories(Mod.CustomContentPath, "*",
                     SearchOption.AllDirectories))
        {
            var addressablePackName = Path.GetFileNameWithoutExtension(contentMod);
            var aaPath = Path.Combine(contentMod, "aa");

            if (!Directory.Exists(aaPath))
            {
                LoggingUtilities.VerboseLog($"Skipping over folder \"{addressablePackName}\" searching for content catalogs because it does not contain an \"aa\" folder. . ."); 
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(aaPath, "catalog_*.json", SearchOption.AllDirectories))
            {
                var resourceLocatorHandle = Addressables.LoadContentCatalogAsync(file);
                yield return resourceLocatorHandle.HandleAsynchronousAddressableOperation();

                if (!AssetUtilities.IsHandleSuccess(resourceLocatorHandle))
                {
                    Mod.Logger.Error(
                        $"Failed to load resource locator for content catalog in Addressable pack \"{addressablePackName}\"!");
                    continue;
                }
                var resourceLocator = resourceLocatorHandle.Result;
                Addressables.AddResourceLocator(resourceLocator);

                _moddedResourceLocators.Add(resourceLocator);
                _packAddressableKeys.Add(addressablePackName, resourceLocator.Keys.ToList());

                foreach (var key in resourceLocator.Keys.ToArray())
                    LoggingUtilities.VerboseLog(
                        $"Stored key from content catalog for \"{addressablePackName}\" | Key: {key.ToString()}");

                Mod.Logger.Msg(ConsoleColor.Green, $"Content catalog for \"{addressablePackName}\" loaded OK");
            }
        }

        stopwatch.Stop();
        Mod.Logger.Msg(ConsoleColor.Green, $"Done custom content catalogs! Total time taken: {stopwatch.Elapsed}");
        ContentCatalogsFinished?.Invoke();
    }

    private static IEnumerator InitializeMapReferences()
    {
        Mod.Logger.Msg("Starting initialization of custom map references. . .");
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        foreach (var sceneDataLoc in GetAllModdedResourceLocationsOfType<SceneData>())
        {
            var castedSceneDataHandle = Addressables.LoadAssetAsync<SceneData>(sceneDataLoc);
            yield return castedSceneDataHandle.HandleAsynchronousAddressableOperation();

            if (!AssetUtilities.IsHandleSuccess(castedSceneDataHandle))
                continue;
            
            var castedSceneData = castedSceneDataHandle.Result;
            if (!castedSceneData)
                continue;
            
            LoggingUtilities.VerboseLog($"Found ResourceLocation (key \"{sceneDataLoc.PrimaryKey}\" holding resource castable to type {typeof(SceneData)}. . .");

            if (!IsValidSceneDataName(sceneDataLoc.PrimaryKey))
            {
                Mod.Logger.Error($"Custom SceneData {sceneDataLoc.PrimaryKey} is not named correctly! The stage it belongs to will not be loaded.");
                continue;
            }

            if (castedSceneData.name != sceneDataLoc.PrimaryKey)
            {
                Mod.Logger.Error($"Custom SceneData of key {sceneDataLoc.PrimaryKey} has differing Object name from Addressable key! The stage it belongs to will not be loaded.");
                continue;
            }

            var parsedSceneName = sceneDataLoc.PrimaryKey.Split("-Data")[0];
            var sceneInfoHandle = Addressables.LoadAssetAsync<CustomMapInfo>($"{parsedSceneName}-Info");
            yield return sceneInfoHandle.HandleAsynchronousAddressableOperation();

            if (!AssetUtilities.IsHandleSuccess(sceneInfoHandle))
                sceneInfoHandle = null;
            else if (sceneInfoHandle.Result.name != parsedSceneName)
            {
                Mod.Logger.Error($"Custom map info of key {parsedSceneName}-Info has differing Object name from Addressable key! The stage it belongs to will not be loaded for any gamemode other than Melee.");
                continue;
            }
            
            var refHolder = new CustomMapRefHolder(sceneDataLoc, sceneInfoHandle?.Result);
            if (!refHolder.IsValid)
            {
                Mod.Logger.Error($"Custom map reference holder is not valid! | Info: {(refHolder.sceneInfo ? refHolder.sceneInfo.name : "null")} | Data: {(refHolder.SceneData ? refHolder.SceneData.name : "null")}");
                continue;
            }
            
            _customMaps.Add(refHolder);
            LoggingUtilities.VerboseLog(ConsoleColor.DarkGreen, $"Custom map reference constructed successfully. | SceneName: {refHolder.SceneName}");
        }
        stopwatch.Stop();
        Mod.Logger.Msg(ConsoleColor.Green, $"Custom map reference initialization complete! {CustomMaps.Count} maps found in {stopwatch.ElapsedMilliseconds}ms");
    }
}
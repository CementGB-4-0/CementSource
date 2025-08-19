using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CementGB.Mod.CustomContent.Costumes;
using CementGB.Mod.Utilities;
using GBMDK;
using Il2CppCostumes;
using Il2CppGB.Data.Loading;
using Il2CppInterop.Runtime;
using Il2CppSystem.Collections.Generic;
using Il2CppSystem.Linq;
using MelonLoader.Utils;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = Il2CppSystem.Object;

namespace CementGB.Mod.CustomContent;

public static class CustomAddressableRegistration
{
    public const string ModsDirectoryPropertyName = "MelonLoader.Utils.MelonEnvironment.ModsDirectory";

    private static readonly System.Collections.Generic.Dictionary<string, List<Object>> _catalogSortedAddressableKeys = [];
    private static readonly System.Collections.Generic.List<IResourceLocator> _moddedResourceLocators = [];
    private static readonly System.Collections.Generic.List<CustomMapRefHolder> _customMaps = [];
    private static readonly System.Collections.Generic.List<string> _baseGameAddressableKeys = [];
    /*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr d_TransformInternalId(IntPtr location);
    
    //[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    //private delegate IntPtr d_TransformInternalId(IntPtr alloc);
    
    private static d_TransformInternalId _patchDelegate;
    private static NativeHook<d_TransformInternalId> _hook;
    
    static unsafe CustomAddressableRegistration()
    {
        var originalMethod = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<ResourceManager>.NativeClassPtr, 100663364);
        _patchDelegate = BounceIdResolutionOffManaged;
        var delegatePointer = Marshal.GetFunctionPointerForDelegate(_patchDelegate);
        var hook = new NativeHook<d_TransformInternalId>(originalMethod, delegatePointer);
        hook.Attach();
        _hook = hook;
    }

    
    private static IntPtr BounceIdResolutionOffManaged(IntPtr location)
    {
        _hook.Trampoline(location);
        var resourceLocation = new IResourceLocation(location);
        return IL2CPP.ManagedStringToIl2Cpp(ResolveInternalId(resourceLocation));
    }
    */

    /*
    private static IntPtr BounceIdResolutionOffManaged(IntPtr alloc)
    {
        Addressables.InternalIdTransformFunc = (Func<IResourceLocation, string>)(ResolveInternalId);
        return _hook.Trampoline(alloc);
    }
    */

    internal static string ResolveInternalId(IResourceLocation location)
    {
        var text = location.InternalId;
        LoggingUtilities.VerboseLog($"Resolving InternalId for mod manager support: {text}");
        if (!location.InternalId.StartsWith(Mod.CustomContentPath)) return text;

        var file = new FileInfo(text);
        foreach (var catalogPath in Directory.EnumerateFiles(Mod.CustomContentPath, "catalog_*.json",
                     SearchOption.AllDirectories))
        {
            var catalogFile = new FileInfo(catalogPath);
            if (!catalogFile.Exists || catalogFile.Directory?.Parent?.Name != file.Directory?.Parent?.Name)
            {
                LoggingUtilities.VerboseLog($"Skipping catalog path {catalogPath}");
                continue;
            }

            var result = catalogFile.Directory != null
                ? Path.Combine(catalogFile.Directory.FullName, file.Name)
                : file.ToString();
            LoggingUtilities.VerboseLog(ConsoleColor.DarkGreen, $"Resolved InternalId: {result}");
            return result;
        }

        return text;
    }

    private static readonly System.Collections.Generic.List<CustomCostumeRefHolder> _customCostumes = [];

    /// <summary>
    ///     Dictionary lookup for all modded Addressable keys (as strings), sorted by mod name.
    /// </summary>
    public static ReadOnlyDictionary<string, string[]> CatalogSortedAddressableKeys // { catalogPath: addressableKeys }
    {
        get
        {
            var dict = new System.Collections.Generic.Dictionary<string, string[]>();

            foreach (var kvp in _catalogSortedAddressableKeys)
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

    /// <summary>
    ///     A collection of all valid maps loaded by Cement. Read-only.
    /// </summary>
    public static ReadOnlyCollection<CustomMapRefHolder> CustomMaps => _customMaps.AsReadOnly();
    public static ReadOnlyCollection<CustomCostumeRefHolder> CustomCostumes => _customCostumes.AsReadOnly();

    /// <summary>
    ///     Gets all custom-loaded IResourceLocations of a certain result type. Used to iterate through and find custom content
    ///     addressable locations depending on type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns>
    ///     An array containing IResourceLocations that, if loaded, will result in the passed type. Will return an array
    ///     even if empty.
    /// </returns>
    public static IResourceLocation[] GetAllModdedResourceLocationsOfType<T>() where T : Object
    {
        System.Collections.Generic.List<IResourceLocation> ret = [];

        foreach (var locator in _moddedResourceLocators)
        {
            foreach (var key in locator.Keys.ToArray())
            {
                var locateRes = locator.Locate(key, Il2CppType.Of<T>(), out var locatorLocations);
                LoggingUtilities.VerboseLog(
                    $"Locator of ID {locator.LocatorId} returned {locateRes.ToString().ToUpper()} locating key {key.ToString()}. . .");

                var locatorLocationsCasted = locatorLocations?.TryCast<List<IResourceLocation>>();
                if (locatorLocationsCasted == null)
                    continue;

                foreach (var location in locatorLocationsCasted.ToArray())
                {
                    if (ret.All(resourceLocation => resourceLocation.PrimaryKey != location.PrimaryKey))
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
    {
        return _catalogSortedAddressableKeys.Any(kvp => kvp.Value.Contains(key));
    }

    private static bool IsValidSceneDataName(string name)
    {
        return name.Split("-Data").Length >= 1;
    }

    internal static void Initialize()
    {
        CacheBaseGameAddressableKeys();
        InitializeContentCatalogs();
        InitializeMapReferences();
        AddressableShaderCache.Initialize();
    }

    private static void CacheBaseGameAddressableKeys()
    {
        Mod.Logger.Msg("Caching base game Addressable keys. . .");
        
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var baseGameLocator = Addressables.InitializeAsync();
        if (!baseGameLocator.HandleSynchronousAddressableOperation())
        {
            Mod.Logger.Error("Failed to handle Addressables initialization operation!");
            return;
        }
        foreach (var key in baseGameLocator.Result.Keys.ToArray())
        {
            _baseGameAddressableKeys.Add(key.ToString());
        }
    }

    private static void InitializeContentCatalogs()
    {
        _catalogSortedAddressableKeys.Clear();
        Mod.Logger.Msg("Starting initialization of modded Addressable content catalogs. . .");

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        AddressablesRuntimeProperties.SetPropertyValue(ModsDirectoryPropertyName, Mod.CustomContentPath);
        LoggingUtilities.VerboseLog(
            $"Set Addressable runtime property \"{ModsDirectoryPropertyName}\" to value \"{AddressablesRuntimeProperties.EvaluateProperty(ModsDirectoryPropertyName)}\"");
        
        foreach (var contentMod in Directory.EnumerateDirectories(Mod.CustomContentPath, "*",
                     SearchOption.AllDirectories))
        {
            var addressablePackName = Path.GetFileNameWithoutExtension(contentMod);
            var aaPath = Path.Combine(contentMod, "aa");

            if (!Directory.Exists(aaPath))
            {
                LoggingUtilities.VerboseLog(
                    $"Skipping over folder \"{addressablePackName}\" searching for content catalogs because it does not contain an \"aa\" folder. . .");
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(aaPath, "catalog_*.json", SearchOption.AllDirectories))
            {
                var resourceLocatorHandle = Addressables.LoadContentCatalogAsync(file);
                if (!resourceLocatorHandle.HandleSynchronousAddressableOperation())
                {
                    Mod.Logger.Error(
                        $"Failed to load resource locator for content catalog in Addressable pack \"{addressablePackName}\"!");
                    continue;
                }

                var resourceLocator = resourceLocatorHandle.Result;
                Addressables.AddResourceLocator(resourceLocator);

                _moddedResourceLocators.Add(resourceLocator);
                _catalogSortedAddressableKeys.Add(file, resourceLocator.Keys.ToList());

                foreach (var key in resourceLocator.Keys.ToArray())
                    LoggingUtilities.VerboseLog(
                        $"Stored key from content catalog for \"{addressablePackName}\" | Key: {key.ToString()}");

                Mod.Logger.Msg(ConsoleColor.Green, $"Content catalog for \"{addressablePackName}\" loaded OK");
            }
        }

        stopwatch.Stop();
        Mod.Logger.Msg(ConsoleColor.Green, $"Done custom content catalogs! Total time taken: {stopwatch.Elapsed}");
    }

    private static void InitializeMapReferences()
    {
        Mod.Logger.Msg("Starting initialization of custom map reference holders. . .");
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        foreach (var sceneDataLoc in GetAllModdedResourceLocationsOfType<SceneData>())
        {
            var castedSceneDataHandle = Addressables.LoadAssetAsync<SceneData>(sceneDataLoc);
            
            if (!castedSceneDataHandle.HandleSynchronousAddressableOperation())
                continue;

            var castedSceneData = castedSceneDataHandle.Result;
            if (!castedSceneData)
                continue;

            if (!IsValidSceneDataName(sceneDataLoc.PrimaryKey))
            {
                Mod.Logger.Error(
                    $"Custom SceneData {sceneDataLoc.PrimaryKey} is not named correctly! The stage it belongs to will not be loaded.");
                continue;
            }

            if (castedSceneData.name != sceneDataLoc.PrimaryKey)
            {
                Mod.Logger.Error(
                    $"Custom SceneData of key {sceneDataLoc.PrimaryKey} has differing Object name from Addressable key! The stage it belongs to will not be loaded.");
                continue;
            }

            var parsedSceneName = sceneDataLoc.PrimaryKey.Split("-Data")[0];
            var sceneInfoHandle = Addressables.LoadAssetAsync<Object>($"{parsedSceneName}-Info");
            sceneInfoHandle.HandleSynchronousAddressableOperation();
            
            if (!AssetUtilities.IsHandleSuccess(sceneInfoHandle))
                sceneInfoHandle = null;

            var sceneInfo = sceneInfoHandle?.Result.TryCast<CustomMapInfo>();
            if (sceneInfo && sceneInfo.name != $"{parsedSceneName}-Info")
            {
                Mod.Logger.Error(
                    $"Custom map info of key {parsedSceneName}-Info has differing Object name from Addressable key! The stage it belongs to will not be loaded for any gamemode other than Melee.");
                sceneInfo = null;
            }

            var refHolder = new CustomMapRefHolder(sceneDataLoc, sceneInfo);
            if (!refHolder.IsValid)
            {
                Mod.Logger.Error(
                    $"Custom map reference holder is not valid! | Info (optional): {(refHolder.SceneInfo ? refHolder.SceneInfo.name : "null")} | Data: {(refHolder.SceneData ? refHolder.SceneData.name : "null")}");
                continue;
            }

            _customMaps.Add(refHolder);
            LoggingUtilities.VerboseLog(ConsoleColor.DarkGreen,
                $"Custom map reference constructed successfully. | SceneName: {refHolder.SceneName}");
        }

        stopwatch.Stop();
        Mod.Logger.Msg(ConsoleColor.Green,
            $"Custom map reference initialization complete! {CustomMaps.Count} maps found in {stopwatch.ElapsedMilliseconds}ms");
    }

    internal static void InitializeCostumeReferences()
    {
        Mod.Logger.Msg("Starting initialization of custom costume reference holders. . .");
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        foreach (var costumeDataLoc in GetAllModdedResourceLocationsOfType<CostumeObject>())
        {
            var assetReference = new AssetReferenceT<CostumeObject>(costumeDataLoc.PrimaryKey);
            
            var refHolder = new CustomCostumeRefHolder(assetReference);
            if (!refHolder.IsValid(costumeDataLoc.PrimaryKey))
            {
                Mod.Logger.Error($"Custom costume reference holder carrying data of key \"{costumeDataLoc.PrimaryKey}\" is not valid! The costume it belongs to will not be loaded.");
                continue;
            }
            
            _customCostumes.Add(refHolder);
            Mod.Logger.Msg(ConsoleColor.Green, $"Custom costume reference holder carrying data of key \"{costumeDataLoc.PrimaryKey}\" initialized OK");
        }
        
        stopwatch.Stop();
        Mod.Logger.Msg(ConsoleColor.Green , $"Custom costume reference holder initialization complete! Took {stopwatch.ElapsedMilliseconds}ms");
    }
}
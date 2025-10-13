using System.Collections.ObjectModel;
using System.Diagnostics;
using CementGB.Modules.CustomContent;
using CementGB.Modules.CustomContent.Utilities;
using CementGB.Utilities;
using CementGB.Modules.CustomContent.CustomMaps;
using Il2CppGB.Data.Loading;
using Il2CppInterop.Runtime;
using Il2CppSystem.Linq;
using MelonLoader.Utils;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using ConsoleColor = System.ConsoleColor;
using Object = Il2CppSystem.Object;

namespace CementGB.CustomContent;

public static class CustomAddressableRegistration
{
    public const string ModsDirectoryPropertyName = "MelonLoader.Utils.MelonEnvironment.ModsDirectory";
    
    /// <summary>
    ///     The path Cement reads custom content from. Custom content must be in its own folder.
    /// </summary>
    /// <remarks>See <see cref="AssetUtilities" /> for modded Addressable helpers.</remarks>
    public static readonly string CustomContentPath = MelonEnvironment.ModsDirectory;

    private static readonly Dictionary<string, Il2CppSystem.Collections.Generic.List<Object>> _catalogSortedAddressableKeys =
        [];

    private static readonly List<IResourceLocator> _moddedResourceLocators = [];
    private static readonly List<CustomMapRefHolder> _customMaps = [];
    private static readonly List<string> _baseGameAddressableKeys = [];

    /// <summary>
    ///     Dictionary lookup for all modded Addressable keys (as strings), sorted by catalog path.
    /// </summary>
    public static ReadOnlyDictionary<string, string[]> CatalogSortedAddressableKeys // { catalogPath: addressableKeys }
    {
        get
        {
            var dict = new Dictionary<string, string[]>();

            foreach (var kvp in _catalogSortedAddressableKeys)
            {
                var addrKeys = new List<string>();

                foreach (var uncastedString in kvp.Value.ToArray())
                {
                    var addrKeyish = uncastedString.ToString();
                    if (addrKeyish != null)
                        addrKeys.Add(addrKeyish);
                }

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

    internal static string ResolveModdedInternalId(string bundleFile)
    {
        var bundleFileInfo = new FileInfo(bundleFile);
        foreach (var catalogPath in Directory.EnumerateFiles(
                     CustomContentPath,
                     "catalog_*.json",
                     SearchOption.AllDirectories))
        {
            var catalogFile = new FileInfo(catalogPath);
            if (!catalogFile.Exists || catalogFile.Directory?.Parent == null ||
                !bundleFile.Contains(catalogFile.Directory.Parent.Name))
            {
                LoggingUtilities.VerboseLog($"Skipping catalog path {catalogPath}. . .");
                continue;
            }

            var result = catalogFile.Directory != null
                ? Path.Combine(catalogFile.Directory.FullName, bundleFileInfo.Name)
                : bundleFileInfo.FullName;
            LoggingUtilities.VerboseLog(ConsoleColor.DarkGreen, $"Resolved InternalId: {result}. . .");
            return result;
        }

        return bundleFile;
    }

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
        List<IResourceLocation> ret = [];

        LoggingUtilities.VerboseLog(
            $"Searching for all modded resource locations of type {Il2CppType.Of<T>().ToString()}. . .");

        foreach (var locator in ModdedResourceLocators)
        {
            var handle = Addressables.LoadResourceLocationsAsync(locator.Keys.ToList().Cast<Il2CppSystem.Collections.Generic.IList<Object>>(),
                Addressables.MergeMode.Union, Il2CppType.Of<T>());

            if (!handle.HandleSynchronousAddressableOperation())
                continue;

            var locatorLocations = handle.Result;
            var locatorLocationsCasted = locatorLocations?.TryCast<Il2CppSystem.Collections.Generic.List<IResourceLocation>>();
            if (locatorLocationsCasted == null)
                continue;

            foreach (var location in locatorLocationsCasted.ToArray())
            {
                if (ret.All(resourceLocation => resourceLocation.PrimaryKey != location.PrimaryKey))
                {
                    //_ = ResolveModdedInternalId(location);
                    ret.Add(location);
                }
            }
        }

        LoggingUtilities.VerboseLog(
            $"Found {ret.Count} modded locations for resource type {Il2CppType.Of<T>().ToString()}.");

        if (ret.Count == 0)
        {
            LoggingUtilities.VerboseLog(
                ConsoleColor.DarkRed,
                $"Returned empty array! Type {Il2CppType.Of<T>().ToString()} probably wasn't found in modded Addressables.");
        }

        return [.. ret];
    }

    public static bool IsModdedKey(string key)
    {
        return CatalogSortedAddressableKeys.Any(kvp => kvp.Value.Contains(key));
    }

    private static bool IsValidSceneDataName(string name)
    {
        return name.Split("-Data").Length >= 1;
    }

    internal static void Initialize()
    {
        MixerFinder.AssignMainMixer();
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

        var baseGameLocator = Addressables.ResourceLocators.First();

        foreach (var key in baseGameLocator.Keys.ToArray())
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

        LoggingUtilities.VerboseLog(
            $"Set Addressable runtime property \"{ModsDirectoryPropertyName}\" to value \"{AddressablesRuntimeProperties.EvaluateProperty(ModsDirectoryPropertyName)}\"");

        foreach (var contentMod in Directory.EnumerateDirectories(
                     CustomContentPath,
                     "*",
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
                {
                    LoggingUtilities.VerboseLog(
                        $"Stored key from content catalog for \"{addressablePackName}\" | Key: {key.ToString()}");
                }

                Mod.Logger.Msg(ConsoleColor.Green, $"Content catalog for \"{addressablePackName}\" loaded OK");
            }
        }

        stopwatch.Stop();
        Mod.Logger.Msg(ConsoleColor.Green, $"Done custom content catalogs! Total time taken: {stopwatch.Elapsed}");
    }

    private static void InitializeMapReferences()
    {
        Mod.Logger.Msg("Starting initialization of custom map references. . .");
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        foreach (var sceneDataLoc in GetAllModdedResourceLocationsOfType<SceneData>())
        {
            if (!IsValidSceneDataName(sceneDataLoc.PrimaryKey))
            {
                Mod.Logger.Error(
                    $"Custom SceneData \"{sceneDataLoc.PrimaryKey}\" is not named correctly! The stage it belongs to will not be loaded.");
                continue;
            }

            var parsedSceneName = sceneDataLoc.PrimaryKey.Split("-Data")[0];
            var infoLoc = GetAllModdedResourceLocationsOfType<Object>()
                .FirstOrDefault(loc => loc.PrimaryKey == $"{parsedSceneName}-Info");
            var refHolder = new CustomMapRefHolder(sceneDataLoc, infoLoc);
            if (!refHolder.IsValid)
            {
                Mod.Logger.Error(
                    $"Custom map reference holder is not valid! | Info: {(refHolder.SceneInfo ? refHolder.SceneInfo.name : "null")} | Data: {(refHolder.SceneData ? refHolder.SceneData?.name : "null")}");
                continue;
            }

            _customMaps.Add(refHolder);
            LoggingUtilities.VerboseLog(
                ConsoleColor.DarkGreen,
                $"Custom map reference constructed successfully. | SceneName: {refHolder.SceneName}");
        }

        stopwatch.Stop();
        Mod.Logger.Msg(
            ConsoleColor.Green,
            $"Custom map reference initialization complete! {CustomMaps.Count} maps found in {stopwatch.ElapsedMilliseconds}ms");
    }
}
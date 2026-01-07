using System.Diagnostics;
using CementGB.Modules.CustomContent.Utilities;
using CementGB.Utilities;
using Il2CppInterop.Runtime;
using Il2CppSystem.Linq;
using MelonLoader.Utils;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using ConsoleColor = System.ConsoleColor;
using Object = Il2CppSystem.Object;

namespace CementGB.Modules.CustomContent;

public static class CustomAddressableRegistration
{
    public const string ModsDirectoryPropertyName = "MelonLoader.Utils.MelonEnvironment.ModsDirectory";

    /// <summary>
    ///     The path Cement reads custom content from. Custom content must be in its own folder.
    /// </summary>
    /// <remarks>See <see cref="AssetUtilities" /> for modded Addressable helpers.</remarks>
    public static readonly string CustomContentPath = MelonEnvironment.ModsDirectory;

    private static readonly CustomContentRefLoader[] CustomContentLoaders =
    [
        new CustomMapLoader()
    ];

    private static readonly Dictionary<string, Il2CppSystem.Collections.Generic.List<Object>>
        _catalogSortedAddressableKeys =
            [];

    private static readonly List<IResourceLocator> _moddedResourceLocators = [];
    private static readonly Dictionary<string, CustomContentRefHolder[]> _customContentMap = new();
    private static readonly List<string> _baseGameAddressableKeys = [];

    /// <summary>
    ///     Dictionary lookup for all modded Addressable keys (as strings), sorted by catalog path.
    /// </summary>
    public static IReadOnlyDictionary<string, string[]> CatalogSortedAddressableKeys // { catalogPath: addressableKeys }
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

            return dict;
        }
    }

    public static IReadOnlyCollection<IResourceLocator> ModdedResourceLocators => _moddedResourceLocators;
    public static CustomMapRefHolder[] CustomMaps => _customContentMap["Stages"].Cast<CustomMapRefHolder>().ToArray();

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
                CustomContentModule.Logger?.VerboseLog($"Skipping catalog path {catalogPath}. . .");
                continue;
            }

            var result = catalogFile.Directory != null
                ? Path.Combine(catalogFile.Directory.FullName, bundleFileInfo.Name)
                : bundleFileInfo.FullName;
            CustomContentModule.Logger?.VerboseLog(ConsoleColor.DarkGreen, $"Resolved InternalId: {result}. . .");
            return result;
        }

        return bundleFile;
    }

    public static bool IsModdedKey(string key)
    {
        return CatalogSortedAddressableKeys.Any(kvp => kvp.Value.Contains(key));
    }

    internal static void Initialize()
    {
        MixerFinder.AssignMainMixer();
        if (Entrypoint.DebugArg) CacheBaseGameAddressableKeys();
        InitializeContentCatalogs();
        ExecuteCustomContentLoaders();
        AddressableShaderCache.Initialize();
    }

    private static void ExecuteCustomContentLoaders()
    {
        CustomContentModule.Logger?.Msg("Triggering custom content loaders. . .");
        foreach (var loader in CustomContentLoaders)
        {
            _customContentMap[loader.CustomContentTypeString] = loader.Load();
        }

        CustomContentModule.Logger?.Msg(ConsoleColor.Green, "Done!");
    }

    private static void CacheBaseGameAddressableKeys()
    {
        CustomContentModule.Logger?.Msg("Caching base game Addressable keys. . .");

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var baseGameLocator = Addressables.ResourceLocators.First();

        foreach (var key in baseGameLocator.Keys.ToArray())
        {
            _baseGameAddressableKeys.Add(key.ToString());
        }

        stopwatch.Stop();
        CustomContentModule.Logger?.Msg(ConsoleColor.Green, $"Done! Took {stopwatch.ElapsedMilliseconds}ms");
    }

    private static void InitializeContentCatalogs()
    {
        _catalogSortedAddressableKeys.Clear();
        CustomContentModule.Logger?.Msg("Starting initialization of modded Addressable content catalogs. . .");

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        foreach (var contentMod in Directory.EnumerateDirectories(
                     CustomContentPath,
                     "*",
                     SearchOption.AllDirectories))
        {
            var addressablePackName = Path.GetFileNameWithoutExtension(contentMod);
            var aaPath = Path.Combine(contentMod, "aa");

            if (!Directory.Exists(aaPath))
            {
                CustomContentModule.Logger?.VerboseLog(
                    $"Skipping over folder \"{addressablePackName}\" searching for content catalogs because it does not contain an \"aa\" folder. . .");
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(aaPath, "catalog_*.json", SearchOption.AllDirectories))
            {
                var resourceLocatorHandle = Addressables.LoadContentCatalogAsync(file);
                if (!resourceLocatorHandle.HandleSynchronousAddressableOperation())
                {
                    CustomContentModule.Logger?.Error(
                        $"Failed to load resource locator for content catalog in Addressable pack \"{addressablePackName}\"!");
                    continue;
                }

                var resourceLocator = resourceLocatorHandle.Result;
                Addressables.AddResourceLocator(resourceLocator);

                _moddedResourceLocators.Add(resourceLocator);
                _catalogSortedAddressableKeys.Add(file, resourceLocator.Keys.ToList());

                foreach (var key in resourceLocator.Keys.ToArray())
                {
                    CustomContentModule.Logger?.VerboseLog(
                        $"Stored key from content catalog for \"{addressablePackName}\" | Key: {key.ToString()}");
                }

                CustomContentModule.Logger?.Msg(ConsoleColor.Green,
                    $"Content catalog for \"{addressablePackName}\" loaded OK");
            }
        }

        stopwatch.Stop();
        CustomContentModule.Logger?.Msg(ConsoleColor.Green,
            $"Done custom content catalogs! Took {stopwatch.ElapsedMilliseconds}ms");
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

        CustomContentModule.Logger?.VerboseLog(
            $"Searching for all modded resource locations of type {Il2CppType.Of<T>().ToString()}. . .");

        foreach (var locator in ModdedResourceLocators)
        {
            var handle = Addressables.LoadResourceLocationsAsync(
                locator.Keys.ToList().Cast<Il2CppSystem.Collections.Generic.IList<Object>>(),
                Addressables.MergeMode.Union, Il2CppType.Of<T>());

            if (!handle.HandleSynchronousAddressableOperation())
                continue;

            var locatorLocations = handle.Result;
            var locatorLocationsCasted =
                locatorLocations?.TryCast<Il2CppSystem.Collections.Generic.List<IResourceLocation>>();
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

        CustomContentModule.Logger?.VerboseLog(
            $"Found {ret.Count} modded locations for resource type {Il2CppType.Of<T>().ToString()}.");

        if (ret.Count == 0)
        {
            CustomContentModule.Logger?.VerboseLog(
                ConsoleColor.DarkRed,
                $"Returned empty array! Type {Il2CppType.Of<T>().ToString()} probably wasn't found in modded Addressables.");
        }

        return [.. ret];
    }
}
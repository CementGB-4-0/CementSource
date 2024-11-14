using MelonLoader;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CementGB.Mod.Utilities;

/// <summary>
/// Utilities that make working with AssetBundles easier in the IL2CPP space. Implements shorthand for persistent asset loading and embedded AssetBundles.
/// </summary>
public static class AssetBundleUtilities
{
    /// <summary>
    /// Shorthand for loading an AssetBundle's asset by name and type in way that prevents it from being garbage collected.
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
    /// Shorthand for loading an AssetBundle's asset by name and type in way that prevents it from being garbage collected. This method will execute the callback when async loading is complete.
    /// </summary>
    /// <typeparam name="T">The type of the asset to load.</typeparam>
    /// <param name="bundle">The bundle to load the asset from.</param>
    /// <param name="name">The exact name of the asset to load.</param>
    /// <param name="onLoaded">The callback to execute once the asset loads. Takes the loaded asset as a parameter.</param>
    public static void LoadPersistentAssetAsync<T>(this AssetBundle bundle, string name, Action<T> onLoaded) where T : UnityEngine.Object
    {
        var request = bundle.LoadAssetAsync<T>(name);

        request.add_completed((Il2CppSystem.Action<AsyncOperation>)((a) =>
        {
            if (request.asset == null) return;
            var result = request.asset.TryCast<T>();
            if (result == null) return;
            result.hideFlags = HideFlags.DontUnloadUnusedAsset;
            onLoaded?.Invoke(result);
        }));
    }

    public static void LoadAllAssetsPersistentAsync<T>(this AssetBundle bundle, Action<T> onLoaded) where T : UnityEngine.Object
    {
        var request = bundle.LoadAllAssetsAsync<T>();

        request.add_completed((Il2CppSystem.Action<AsyncOperation>)new Action<AsyncOperation>((a) =>
        {
            if (request.asset == null) return;
            var result = request.asset.TryCast<T>();
            if (result == null) return;
            result.hideFlags = HideFlags.DontUnloadUnusedAsset;
            onLoaded?.Invoke(result);
        }));
    }

    /// <summary>
    /// Loads an AssetBundle from an assembly that has it embedded. 
    /// Good for keeping mods small and single-filed. 
    /// Mark an AssetBundle as an EmbeddedResource in your csproj in order for this to work.
    /// </summary>
    /// <param name="assembly">The Assembly instance the AssetBundle is embedded within. Usually it is fine to use <c>Assembly.GetExecutingAssembly()</c> for this.</param>
    /// <param name="name">The embedded path to the AssetBundle file. Embedded paths usually start with the csproj name and progress by dots, e.g. ExampleMod/Assets/coag.bundle -> ExampleMod.Assets.coag.bundle</param>
    /// <returns></returns>
    /// <exception cref="Exception">Throws if it can't find the AssetBundle within the assembly.</exception>
    public static AssetBundle LoadEmbeddedAssetBundle(Assembly assembly, string name)
    {
        if (assembly.GetManifestResourceNames().Contains(name))
        {
            Melon<Mod>.Logger.Msg($"Loading stream for resource '{name}' embedded from assembly...");
            using var str = assembly.GetManifestResourceStream(name) ?? throw new Exception("Resource stream returned null. This could mean an inaccessible resource caller-side or an invalid argument was passed.");
            using MemoryStream memoryStream = new();
            str.CopyTo(memoryStream);
            Melon<Mod>.Logger.Msg(ConsoleColor.Green, "Done!");
            var resource = memoryStream.ToArray();

            Melon<Mod>.Logger.Msg($"Loading assetBundle from data '{name}', please be patient...");
            var bundle = AssetBundle.LoadFromMemory(resource);
            Melon<Mod>.Logger.Msg(ConsoleColor.Green, "Done!");
            return bundle;
        }
        throw new Exception($"No resources matching the name '{name}' were found in the assembly '{assembly.FullName}'. Please ensure you passed the correct name.");
    }
}

using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace CementGB.Utilities;

/// <summary>
///     Utilities that make working with AssetBundles easier in the IL2CPP space. Implements shorthand for embedded
///     AssetBundles.
/// </summary>
public static class EmbeddedUtilities
{
    /// <summary>
    ///     Loads an AssetBundle from an assembly that has it embedded.
    ///     Good for keeping mods small and single-filed.
    ///     Mark an AssetBundle as an EmbeddedResource in your csproj in order for this to work.
    /// </summary>
    /// <param name="assembly">
    ///     The Assembly instance the AssetBundle is embedded within. Usually it is fine to use
    ///     <c>Assembly.GetExecutingAssembly()</c> for this.
    /// </param>
    /// <param name="name">
    ///     The embedded path to the AssetBundle file. Embedded paths usually start with the csproj name and
    ///     progress by dots, e.g. ExampleMod/Assets/coag.bundle -> ExampleMod.Assets.coag.bundle
    /// </param>
    /// <returns></returns>
    /// <exception cref="Exception">Throws if it can't find the AssetBundle within the assembly.</exception>
    public static AssetBundle LoadEmbeddedAssetBundle(Assembly assembly, string name)
    {
        if (assembly.GetManifestResourceNames().Contains(name))
        {
            Melon<Entrypoint>.Logger.Msg($"Loading stream for resource '{name}' embedded from assembly...");
            using var str = assembly.GetManifestResourceStream(name) ?? throw new Exception(
                "Resource stream returned null. This could mean an inaccessible resource caller-side or an invalid argument was passed.");
            using MemoryStream memoryStream = new();
            str.CopyTo(memoryStream);
            Melon<Entrypoint>.Logger.Msg(ConsoleColor.Green, "Done!");
            var resource = memoryStream.ToArray();

            Melon<Entrypoint>.Logger.Msg($"Loading assetBundle from data '{name}', please be patient...");
            var bundle = AssetBundle.LoadFromMemory(resource);
            Melon<Entrypoint>.Logger.Msg(ConsoleColor.Green, "Done!");
            return bundle;
        }

        throw new Exception(
            $"No resources matching the name '{name}' were found in the assembly '{assembly.FullName}'. Please ensure you passed the correct name.");
    }

    /// <summary>
    ///     Reads all text from an embedded file. File must be marked as an EmbeddedResource in the mod's csproj.
    /// </summary>
    /// <param name="assembly">
    ///     The assembly the file is embedded in. It's usually okay to use
    ///     <c>Assembly.GetExecutingAssembly</c> or <c>MelonMod.MelonAssembly.Assembly</c> to get the current assembly.
    /// </param>
    /// <param name="resourceName">
    ///     The embedded path to the file. Usually you can just use the path pseudo-relative to the
    ///     solution directory separated by dots, e.g. ExampleMod/Assets/text.txt ExampleMod.Assets.text.txt
    /// </param>
    /// <returns>The text the file contains.</returns>
    /// <exception cref="Exception"></exception>
    public static string ReadEmbeddedText(Assembly assembly, string resourceName)
    {
        assembly ??= Assembly.GetCallingAssembly();

        if (assembly.GetManifestResourceNames().Contains(resourceName))
        {
            using var str = assembly.GetManifestResourceStream(resourceName) ?? throw new Exception(
                "Resource stream returned null. This could mean an inaccessible resource caller-side or an invalid argument was passed.");
            using StreamReader reader = new(str);

            return reader.ReadToEnd();
        }

        throw new Exception(
            $"No resources matching the name '{resourceName}' were found in the assembly '{assembly.FullName}'. Please ensure you passed the correct name.");
    }
}
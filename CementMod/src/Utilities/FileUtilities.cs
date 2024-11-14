using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CementGB.Mod.Utilities;

/// <summary>
/// File-related utilities. Currently only contains <seealso cref="ReadEmbeddedText(Assembly, string)"/>.
/// </summary>
public static class FileUtilities
{
    /// <summary>
    /// Reads all text from an embedded file. File must be marked as an EmbeddedResource in the mod's csproj.
    /// </summary>
    /// <param name="assembly">The assembly the file is embedded in. Its usually okay to use <c>Assembly.GetExecutingAssembly</c> or <c>MelonMod.MelonAssembly.Assembly</c> to get the current assembly.</param>
    /// <param name="resourceName">The embedded path to the file. Usually you can just use the path pseudo-relative to the solution directory separated by dots, e.g. ExampleMod/Assets/text.txt ExampleMod.Assets.text.txt</param>
    /// <returns>The text the file contains.</returns>
    /// <exception cref="Exception"></exception>
    public static string ReadEmbeddedText(Assembly assembly, string resourceName)
    {
        assembly ??= Assembly.GetCallingAssembly();

        if (assembly.GetManifestResourceNames().Contains(resourceName))
        {
            using var str = assembly.GetManifestResourceStream(resourceName) ?? throw new Exception("Resource stream returned null. This could mean an inaccessible resource caller-side or an invalid argument was passed.");
            using StreamReader reader = new(str);

            return reader.ReadToEnd();
        }
        throw new Exception($"No resources matching the name '{resourceName}' were found in the assembly '{assembly.FullName}'. Please ensure you passed the correct name.");
    }
}
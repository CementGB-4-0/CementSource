using System;
using System.Collections.Generic;

namespace CementGB.Mod.Utilities;

/// <summary>
///     This class registers simple modded string localization, e.g. "STAGE_GRIND" -> "Grind"
/// </summary>
public static class ExtendedStringLoader
{
    internal static readonly Dictionary<string, string> items = [];

    /// <summary>
    ///     Registers a key-value pair for rudimentary localization. When the key is loading via GB's <c>StringLoader.LoadString</c>
    ///     methods, it returns the value string instead.
    /// </summary>
    /// <param name="key">
    ///     A string in uppercase, prefixed with the type of object it is naming, also in uppercase. This will be
    ///     replaced by <paramref name="value" /> upon load. MUST BE UNIQUE!
    /// </param>
    /// <param name="value">Can be any string in any format. Will replace appearances of <paramref name="key" />.</param>
    public static void Register(string key, string value)
    {
        if (!items.TryAdd(key, value))
        {
            LoggingUtilities.VerboseLog(ConsoleColor.DarkRed,
                $"'{key}' has already been registered in ExtendedStringLoader");
        }
    }
}
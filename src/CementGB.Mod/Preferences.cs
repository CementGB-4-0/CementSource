using MelonLoader;

namespace CementGB;

/// <summary>
///     Home to Cement's individual MelonPreferences. If you are looking for the custom Preferences system, please refer
///     back to the docs for <c>PreferenceModule</c>.
/// </summary>
public static class CementPreferences
{
    private static MelonPreferences_Category _cmtPrefCateg =
        MelonPreferences.CreateCategory("CementGBPrefs", "CementGB Preferences");

    private static MelonPreferences_Entry<bool>? _verboseModeEntry;
    private static MelonPreferences_Entry<string>? _fallbackMapEntry;

    /// <summary>
    ///     Enables extra log messages for debugging.
    ///     Controlled by a MelonPreference.
    /// </summary>
    public static bool VerboseMode => _verboseModeEntry?.Value ?? Mod.DebugArg;

    public static string FallbackMap => _fallbackMapEntry?.Value ?? "Grind";

    internal static void Initialize()
    {
        _cmtPrefCateg = MelonPreferences.CreateCategory("CementGBPrefs", "CementGB Preferences");
        _cmtPrefCateg.SetFilePath(Path.Combine(Mod.UserDataPath, "CementPrefs.cfg"));
        _verboseModeEntry = _cmtPrefCateg.CreateEntry(
            "verbose_mode",
            false,
            "Verbose Mode",
            "Enables extra log messages for developers.");
        _fallbackMapEntry = _cmtPrefCateg.CreateEntry("fallback_map", "Grind", "Fallback Map",
            "CASE-SENSITIVE name of the SCENE (not map) to fall back to when a custom map fails to load.");
        _cmtPrefCateg?.SaveToFile();
    }

    internal static void Deinitialize()
    {
        _cmtPrefCateg?.SaveToFile();
    }
}
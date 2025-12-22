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

    /// <summary>
    ///     Enables extra log messages for debugging.
    ///     Controlled by a MelonPreference.
    /// </summary>
    public static bool VerboseMode => _verboseModeEntry?.Value ?? Mod.DebugArg;

    internal static void Initialize()
    {
        _cmtPrefCateg = MelonPreferences.CreateCategory("CementGBPrefs", "CementGB Preferences");
        _cmtPrefCateg.SetFilePath(Path.Combine(Mod.UserDataPath, "CementPrefs.cfg"));
        _verboseModeEntry = _cmtPrefCateg.CreateEntry(
            "verbose_mode",
            false,
            "Verbose Mode",
            "Enables extra log messages for developers.");
        _cmtPrefCateg?.SaveToFile();
    }

    internal static void Deinitialize()
    {
        _cmtPrefCateg?.SaveToFile();
    }
}
using MelonLoader;
using System.Collections.Generic;

namespace CementGB.Mod.Modules.PreferenceModule;
public static class MelonCategExtensions
{
    private static readonly Dictionary<MelonPreferences_Category, bool> isLivePref_Tracker = new();

    public static void SetLivePref(this MelonPreferences_Category category, bool value)
    {
        isLivePref_Tracker[category] = value;
    }

    public static bool IsLivePref(this MelonPreferences_Category category)
    {
        return isLivePref_Tracker.ContainsKey(category) && isLivePref_Tracker[category];
    }
}

using HarmonyLib;
using Il2CppGB.Setup;

namespace CementGB.Patches;

[HarmonyPatch(typeof(GlobalSceneLoader), nameof(GlobalSceneLoader.DisplaySplashScreen))]
internal static class GlobalSceneLoader_DisplaySplashScreenPatch
{
    private static bool Prefix()
    {
        return !CementPreferences.SkipSplashes;
    }
}
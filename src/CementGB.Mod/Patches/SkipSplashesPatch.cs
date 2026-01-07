using HarmonyLib;
using Il2CppGB.Setup;

namespace CementGB.Patches;

[HarmonyPatch]
internal static class SkipSplashesPatch
{
    [HarmonyPatch(typeof(GlobalSceneLoader), nameof(GlobalSceneLoader.DisplaySplashScreen))]
    [HarmonyPrefix]
    private static bool Prefix()
    {
        return !Entrypoint.SkipSplashScreens;
    }
}
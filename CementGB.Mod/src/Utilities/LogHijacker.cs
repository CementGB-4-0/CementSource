using HarmonyLib;
using Il2CppCoatsink.Platform.Utils;
using Il2CppCS.CorePlatform;

namespace CementGB.Utilities;

internal static class LogHijacker
{
    [HarmonyPatch(typeof(CSStartupCalls), nameof(CSStartupCalls.Initialize))]
    [HarmonyPostfix]
    internal static void CSStartupCallsInitializePatch()
    {
        Debug.ActiveLevel = Debug.Level.INFO | Debug.Level.WARNING | Debug.Level.ERROR | Debug.Level.DEEP |
                            (CementPreferences.VerboseMode ? Debug.Level.SPAM : Debug.Level.NONE) | Debug.Level.EXCEPTION;
    }

    internal static void HijackLogs()
    {
        Debug.OnLog += (Il2CppSystem.Action<Debug.Level, string>)((_, message) =>
        {
            Mod.Logger.VerboseLog("[HIJACKED COATSINK LOG] " + message);
        });
    }
}
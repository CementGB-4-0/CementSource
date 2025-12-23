using HarmonyLib;
using Il2Cpp;

namespace CementGB.Modules.NetBeardModule.Patches;

internal static class DevelopmentTestServerUIPatches
{
    [HarmonyPatch(typeof(DevelopmentTestServerUI), nameof(DevelopmentTestServerUI.LoadConfig))]
    private static class StartPatch
    {
        private static void Postfix(DevelopmentTestServerUI __instance)
        {
            __instance.m_config.connectIP = NetBeardModule.IP;
            __instance.m_config.connectPort = NetBeardModule.Port;
            __instance.UpdateInputs(__instance.m_config);
        }
    }
}
using HarmonyLib;
using Il2Cpp;

namespace CementGB.Modules.NetBeard.Patches;

internal static class DevelopmentTestServerUIPatches
{
    [HarmonyPatch(typeof(DevelopmentTestServerUI), nameof(DevelopmentTestServerUI.LoadConfig))]
    private static class StartPatch
    {
        private static void Postfix(DevelopmentTestServerUI __instance)
        {
            __instance.m_config.connectIP = ServerManager.IP;
            __instance.m_config.connectPort = ServerManager.Port;
            __instance.UpdateInputs(__instance.m_config);
        }
    }
}
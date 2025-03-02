using CementGB.Mod.Modules.NetBeard;
using HarmonyLib;
using Il2CppCoreNet;
using Il2CppCoreNet.Config;
using UnityEngine;

namespace CementGB.Mod.Patches;

internal static class FWDPatches
{
    [HarmonyPatch(typeof(NetworkManager), nameof(NetworkManager.LaunchServer))]
    private static class NetworkManagerLaunchHostPatch
    {
        private static void Prefix(object[] __args)
        {
            if (ServerManager.IsForwardedHost && !Application.isBatchMode)
            {
                __args[0] = NetConfigLoader.LoadServerConfig();
            }
        }

        private static void Postfix()
        {
            if (ServerManager.IsForwardedHost && !Application.isBatchMode)
            {
                NetworkManager.OnHostStarted.Invoke();
            }
        }
    }
}
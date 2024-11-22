using CementGB.Mod.Modules.NetBeard;
using CementGB.Mod.Utilities;
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
        private static void Prefix(NetworkManager __instance, object[] __args)
        {
            LoggingUtilities.Logger.BigError("this ran");

            if (ServerManager.IsForwarded && !Application.isBatchMode)
            {
                __args[0] = NetConfigLoader.LoadServerConfig();

                NetworkManager.OnHostStarted.Invoke();
            }
        }
    }
}

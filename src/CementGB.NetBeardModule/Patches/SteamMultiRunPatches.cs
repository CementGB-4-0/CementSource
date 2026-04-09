using HarmonyLib;
using Il2CppCoatsink.Platform;
using Il2CppCoatsink.Platform.Systems.UI;
using Il2CppCS.CorePlatform;
using Il2CppCS.CorePlatform.CSPlatform;
using Il2CppGB.Core;
using Il2CppSteamworks;
using Core = Il2CppCoatsink.Platform.Steam.Core;

namespace CementGB.Modules.NetBeardModule.Patches;

[HarmonyPatch]
internal static class SteamMultiRunPatches
{
    [HarmonyPatch(typeof(SteamAPI), nameof(SteamAPI.RestartAppIfNecessary))]
    [HarmonyPrefix]
    private static bool DoRestartCheckPrefix()
    {
        return !NetBeardProps.IsServer;
    }

    [HarmonyPatch(typeof(Core), nameof(Core.Initialize))]
    [HarmonyPrefix]
    private static void CoreInitializePrefix(Core __instance)
    {
        if (NetBeardProps.IsServer) __instance._gameID = 497110;
    }

    [HarmonyPatch(typeof(SteamUtils), nameof(SteamUtils.GetAppID))]
    [HarmonyPostfix]
    private static void ServerAppIdOverridePostfix(ref AppId_t __result)
    {
        if (NetBeardProps.IsServer)
        {
            __result = new AppId_t(497110);
        }
    }

    [HarmonyPatch(typeof(CStoCorePlatform), nameof(CStoCorePlatform.OnInitializeComplete))]
    [HarmonyPrefix]
    private static bool Prefix(CStoCorePlatform __instance)
    {
        if (!NetBeardProps.IsServer)
        {
            return true;
        }

        Users.MaxUsers = Global.NetworkMaxPlayers;
        UI.PopUpUI = __instance._dialogUI.Cast<IUIPopUpManager>();
        BasePlatformManager._InitializedPlatformAPI = true;
        BasePlatformManager.Initialized = true;
        __instance.PassEntitlement();
        __instance._online.CheckSetup();
        __instance._network.CheckSetup();
        return false;
    }
}
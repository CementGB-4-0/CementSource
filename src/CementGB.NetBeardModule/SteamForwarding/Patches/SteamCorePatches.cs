using System.Net;
using HarmonyLib;
using Il2CppCoatsink.Platform;
using Il2CppCoatsink.Platform.Systems.UI;
using Il2CppCS.CorePlatform;
using Il2CppCS.CorePlatform.CSPlatform;
using Il2CppGB.Core;
using Il2CppSteamworks;
using Steamworks;
using Core = Il2CppCoatsink.Platform.Steam.Core;
using SteamApps = Il2CppSteamworks.SteamApps;
using SteamClient = Steamworks.SteamClient;
using SteamUser = Il2CppSteamworks.SteamUser;
using SteamUtils = Il2CppSteamworks.SteamUtils;
using Users = Il2CppCoatsink.Platform.Steam.Users;

namespace CementGB.NetBeardModule.Patches;

[HarmonyPatch]
internal static class SteamCorePatches
{
    [HarmonyPatch(typeof(SteamAPI), nameof(SteamAPI.RestartAppIfNecessary))]
    [HarmonyPrefix]
    private static bool DoRestartCheckPrefix()
    {
        return !NetBeardProps.IsServer;
    }

    [HarmonyPatch(typeof(Core), nameof(Core.Initialize))]
    [HarmonyPrefix]
    private static bool CoreInitializePrefix(Core __instance, TaskResult<bool> taskResult)
    {
        if (NetBeardProps.IsServer)
        {
            __instance._gameID = NetBeardModule.ServerAppId;
            InitializeSteamServer();

            Core._steamActive = true;
            taskResult.Result = true;
            taskResult.Complete();
            return false;
        }

        SteamClient.Init(NetBeardModule.ClientAppId);

        return true;
    }

    private static void InitializeSteamServer()
    {
        SteamServer.Init(NetBeardModule.ServerAppId, new SteamServerInit
        {
            DedicatedServer = true,
            GameDescription = "Gang Beasts Server",
            GamePort = (ushort)NetBeardProps.Port,
            IpAddress = IPAddress.Loopback,
            ModDir = "Gang Beasts",
            Secure = true,
            VersionString = "1.0.0.0",
            QueryPort = 27015
        }.WithRandomSteamPort(), false);
        SteamServer.MaxPlayers = Global.NetworkMaxPlayers;
        SteamServer.ServerName = NetBeardConfig.Current.ServerName;
        SteamServer.MapName = "unset";
        SteamServer.SetKey("map", "unset");
        SteamServer.LogOnAnonymous();
    }

    [HarmonyPatch(typeof(Core), nameof(Core.UpdateCore))]
    [HarmonyPrefix]
    private static bool CoreUpdatePrefix(Core __instance)
    {
        if (NetBeardProps.IsServer)
        {
            SteamServer.RunCallbacks();
            return false;
        }

        SteamClient.RunCallbacks();

        return true;
    }

    [HarmonyPatch(typeof(Core), nameof(Core.Terminate))]
    [HarmonyPrefix]
    private static bool CoreTerminatePrefix(Core __instance)
    {
        if (NetBeardProps.IsServer)
        {
            SteamServer.Shutdown();
            Core._steamActive = false;
            return false;
        }

        SteamClient.Shutdown();

        return true;
    }

    [HarmonyPatch(typeof(SteamApps), nameof(SteamApps.GetLaunchCommandLine))]
    [HarmonyPrefix]
    private static bool GetLaunchCommandLinePrefix(ref string pszCommandLine, ref int __result)
    {
        if (NetBeardProps.IsServer)
        {
            pszCommandLine = string.Empty;
            __result = 0;
            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(SteamUser), nameof(SteamUser.GetSteamID))]
    [HarmonyPrefix]
    private static bool GetUserIdPrefix(Users __instance, ref CSteamID __result)
    {
        __result = (CSteamID)SteamClient.SteamId.Value;
        NetBeardModule.Logger?.Msg(__result.ToString());
        return false;
    }

    [HarmonyPatch(typeof(SteamUtils), nameof(SteamUtils.GetAppID))]
    [HarmonyPostfix]
    private static void ServerAppIdOverridePostfix(ref AppId_t __result)
    {
        if (NetBeardProps.IsServer)
        {
            __result = new AppId_t(NetBeardModule.ServerAppId);
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

        Il2CppCoatsink.Platform.Users.MaxUsers = Global.NetworkMaxPlayers;
        UI.PopUpUI = __instance._dialogUI.Cast<IUIPopUpManager>();
        BasePlatformManager._InitializedPlatformAPI = true;
        BasePlatformManager.Initialized = true;
        __instance.PassEntitlement();
        __instance._online.CheckSetup();
        __instance._network.CheckSetup();
        return false;
    }
}
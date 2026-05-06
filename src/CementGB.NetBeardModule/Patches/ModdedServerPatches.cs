using CementGB.Utilities;
using HarmonyLib;
using Il2CppCoreNet.Components.Server;
using Il2CppCoreNet.Utils;
using Il2CppGB.Game;
using Il2CppGB.UI;
using UnityEngine;
using UnityEngine.Networking;
using Object = Il2CppSystem.Object;

namespace CementGB.NetBeardModule.Patches;

[HarmonyPatch]
internal static class ModdedServerPatches
{
    [HarmonyPatch(typeof(Debug), nameof(Debug.Log), typeof(Object))]
    [HarmonyPostfix]
    private static void DebugLogHijackPatch(Object message)
    {
        if (Application.isBatchMode)
            NetBeardModule.Logger?.VerboseLog(message.ToString());
    }

    [HarmonyPatch(typeof(Debug), nameof(Debug.LogError), typeof(Object))]
    [HarmonyPostfix]
    private static void DebugLogErrorHijackPatch(Object message)
    {
        if (Application.isBatchMode)
            NetBeardModule.Logger?.VerboseLog(ConsoleColor.DarkRed, message.ToString());
    }

/*
    [HarmonyPatch(typeof(NetworkBootstrapper), nameof(NetworkBootstrapper.ResetStatics))]
    [HarmonyPostfix]
    private static void ResetStaticsPostfix()
    {
        NetworkBootstrapper.IsOfficialServer = NetBeardProps.IsServer;
    }
*/
    [HarmonyPatch(typeof(Il2CppCoreNet.NetworkManager), nameof(Il2CppCoreNet.NetworkManager.LaunchClient))]
    [HarmonyPrefix]
    private static void LaunchClientPrefix(NetworkManager __instance, ref string IP)
    {
        if (GameManagerNew.Instance && GameManagerNew.Instance.CurrentGameType != GameManagerNew.GameType.Matchmaker)
            return;
        NetBeardModule.Logger?.Msg($"Connecting to UNET server IP: {IP}");
    }

    [HarmonyPatch(typeof(MenuHandlerGamemodes), nameof(MenuHandlerGamemodes.OnStartGame))]
    [HarmonyPrefix]
    private static bool SingleplayerOnlineBypassPrefix(MenuHandlerGamemodes __instance)
    {
        if (!__instance.PrivateGame)
        {
            return true;
        }

        NetBeardModule.Logger?.VerboseLog("Bypassing matchmaker auth, player joining modded server");
        __instance.StartGameLogic();
        return false;
    }

    [HarmonyPatch(typeof(NetUtils), nameof(NetUtils.DisconnectPlayer))]
    [HarmonyPrefix]
    private static bool AntiTimeoutDisconnectPrefix(NetworkConnection conn, string reason)
    {
        if (!NetBeardProps.IsServer || reason != "DISCONNECT_PLAYER_LOADING_TIMEOUT")
        {
            return true;
        }

        NetBeardModule.Logger?.Warning(
            $"{NetBeardModule.ServerLogPrefix} Server tried to disconnect player that took too long to load; blocked");
        return false;
    }

    [HarmonyPatch(typeof(GameManagerNew), nameof(GameManagerNew.Shutdown))]
    [HarmonyPrefix]
    private static bool ShutdownFix(GameManagerNew __instance, string disconnectMessage)
    {
        if (!NetBeardProps.IsServer)
        {
            return true;
        }

        __instance.StopAllCoroutines();
        __instance.ActiveGameMode?.Cleanup();
        __instance.ActiveGameMode = null;

        NetBeardModule.Logger?.Warning(
            $"{NetBeardModule.ServerLogPrefix} About to disconnect all players with reason: " + disconnectMessage);
        NetUtils.DisconnectAllPlayers(disconnectMessage);
        __instance.CurrentState = GameManagerNew.GameState.Inactive;
        __instance._SceneManager.expectedNumPlayers = -1;
        __instance.authPassed = false;
        __instance.gameManagerSetup = false;
        __instance.joinTimer.Active = false;
        NetBeardModule.Logger?.Msg(ConsoleColor.Green,
            $"{NetBeardModule.ServerLogPrefix} Disconnected all players and deactivated server.");

        return false;
    }

    [HarmonyPatch(typeof(NetServerSceneManager), nameof(NetServerSceneManager.Start))]
    [HarmonyPrefix]
    private static bool JoinTimerFix(NetServerSceneManager __instance)
    {
        if (!NetBeardProps.IsServer)
        {
            return true;
        }

        NetBeardModule.Logger?.Msg($"{NetBeardModule.ServerLogPrefix} Setting up join timers for modded server. . .");

        __instance.LOAD_TIME_MAX = 120f;
        __instance.READY_TIME_MAX = 30f;

        __instance.timer = __instance.LOAD_TIME_MAX;

        NetBeardModule.Logger?.Msg(ConsoleColor.Green, $"{NetBeardModule.ServerLogPrefix} Done!");

        return false;
    }
}
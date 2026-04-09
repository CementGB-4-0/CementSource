using System.Net;
using CementGB.Utilities;
using HarmonyLib;
using Il2Cpp;
using Il2CppCoatsink.UnityServices.Matchmaking;
using Il2CppCoreNet.Components.Server;
using Il2CppCoreNet.Config;
using Il2CppCoreNet.Utils;
using Il2CppGB.Config;
using Il2CppGB.Core;
using Il2CppGB.Game;
using Il2CppGB.Gamemodes;
using Il2CppGB.Menu;
using Il2CppGB.Networking.Components.Client;
using Il2CppGB.Platform.Lobby;
using Il2CppGB.UI;
using Il2CppGB.UnityServices.Matchmaking;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine.Networking;

namespace CementGB.Modules.NetBeardModule.Patches;

[HarmonyPatch]
internal static class ModdedServerPatches
{
    [HarmonyPatch(typeof(GBClientPlatformManager), nameof(GBClientPlatformManager.Awake))]
    [HarmonyPostfix]
    private static void ClientPlatformAwakePostfix(GBClientPlatformManager __instance)
    {
        __instance._wantsToLeave = false;
    }

    [HarmonyPatch(typeof(Il2CppCoreNet.NetworkManager), nameof(Il2CppCoreNet.NetworkManager.LaunchClient))]
    [HarmonyPrefix]
    private static void LaunchClientPrefix(NetworkManager __instance, ref string IP)
    {
        NetBeardModule.Logger?.Msg(ConsoleColor.Blue, $"Connecting to server IP: {IP}");
    }

    [HarmonyPatch(typeof(MenuHandlerGamemodes), nameof(MenuHandlerGamemodes.StartGameLogic))]
    [HarmonyPrefix]
    private static bool StartGameLogicPatch(MenuHandlerGamemodes __instance)
    {
        if (__instance.type != MenuHandlerGamemodes.MenuType.Online || !__instance.PrivateGame)
        {
            return true;
        }

        var shouldJoinModded = TCPCommunicator.Client?.Connected ?? false;
        if (!shouldJoinModded)
        {
            return true;
        }
        // should only run for self-hosted server hosts

        var num2 = __instance.winsSetup.CurrentValue * 60;
        var stageTime = __instance.CurrentGamemode == GameModeEnum.Football ? num2 : 300;

        var currentSelectedLevels = __instance.mapSetup.GetCurrentSelectedLevels(out var isRandomSelected);
        __instance.selectedConfig = GBConfigLoader.CreateRotationConfig(
            (Il2CppStringArray)currentSelectedLevels.ToArray(),
            __instance.CurrentGamemode,
            __instance.CurrentGamemode is GameModeEnum.Football or GameModeEnum.Waves
                ? 1
                : __instance.winsSetup.CurrentValue,
            isRandomSelected,
            stageTime);

        var address = NetBeardProps.IP;

        MonoSingleton<Global>.Instance.buttonController.HideButton(InputMapActions.Accept);
        __instance.PopulateVisibleButtons(true);
        LobbyManager.Instance.LobbyStates.CurrentState = LobbyState.State.Ready | LobbyState.State.Joinable |
                                                         LobbyState.State.Editable | LobbyState.State.Matching;
        LobbyManager.Instance.LobbyStates.UpdateLobbyState();

        LobbyCommunicator.SendLobbyDataToServer(
            new GBGameData
            {
                Gamemode = __instance.CurrentGamemode.GetGameModeID(),
                MapName =
                    __instance.selectedConfig.GameConfigs.Count == 1
                        ? __instance.selectedConfig.GameConfigs[0].Map
                        : "random",
                NumberOfWins = __instance.selectedConfig.Wins,
                PrivateGame = true,
                StageTimeLimit = __instance.selectedConfig.StageTimeLimit,
                TotalPlayerCountExclLocal = (uint)LobbyManager.Instance.Players.GetPlayerCount(),
                TotalPlayerCountInclLocal = (uint)LobbyManager.Instance.Players.GetBeastCount()
            });

        __instance.onlineCountdown.StartCountdown(
            3f,
            new Action(() =>
            {
                LobbyManager.Instance.LobbyStates.CurrentState = LobbyState.State.Ready | LobbyState.State.InGame;
                LobbyManager.Instance.LobbyStates.IP = address.ToString();
                LobbyManager.Instance.LobbyStates.Port = NetBeardProps.Port;
                LobbyManager.Instance.LobbyStates.UpdateLobbyState();

                var result = new MatchmakingResult(MatchmakingState.Success, "Modded lobby done")
                {
                    IpAddress = address,
                    Port = NetBeardProps.Port,
                    State = MatchmakingState.Success
                };

                LobbyManager.Instance.LobbyStates.MatchmakingComplete(result);
            }));

        return false;
    }

    [HarmonyPatch(typeof(LobbyState), nameof(LobbyState.MatchmakingComplete))]
    [HarmonyPrefix]
    private static bool MatchmakingCompletePatch(LobbyState __instance, MatchmakingResult clientResult)
    {
        if (__instance.Private && TCPCommunicator.Client?.Connected != true && NetBeardProps.IsFwd)
        {
            // Private game, Fwd mode enabled and failed to pre-connect to self-hosted servers

            LobbyManager.Instance.LobbyStates.SelfState = LobbyState.Game.Wireless;

            var playerEnumer = LobbyManager.Instance.Players.GetPlayerEnumer();
            while (playerEnumer.MoveNext())
            {
                // For all players in the lobby
                var keyValuePair = playerEnumer._current;
                var key = keyValuePair.Key; // Get BaseUserInfo of player
                if (key == LobbyManager.Instance.MeCache) continue;
                __instance.SendLobbyGameEvent(key, (NetBeardProps.LocalExternalIP ?? IPAddress.Loopback).ToString(),
                    clientResult.Port); // Send player message to connect to server properly
            }

            LobbyManager.Instance.LocalBeasts.SetupNetMemberContext(true);
            MonoSingleton<Global>.Instance.UNetManager.LaunchHost();

            return false;
        }

        return true;
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

    [HarmonyPatch(typeof(NetConfigLoader), nameof(NetConfigLoader.LoadServerConfig), [])]
    [HarmonyPostfix]
    private static void ServerConfigLoadPostfix(ref ServerConfig __result)
    {
        if (NetBeardProps.IsServer)
        {
            __result.Ip = NetBeardProps.IP;
            __result.ServerPort = NetBeardProps.Port;
        }
    }
}
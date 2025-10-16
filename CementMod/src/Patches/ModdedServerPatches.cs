using System;
using System.Net;
using CementGB.Mod.Modules.NetBeard;
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

namespace CementGB.Mod.Patches;

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
        if (GameManagerNew.Instance && GameManagerNew.Instance.CurrentGameType != GameManagerNew.GameType.Matchmaker)
            return;
        _ = LobbyManager.Instance.LocalBeasts.SetupNetMemberContext(true);
        if (LobbyCommunicator.UserExternalIP == null || LobbyCommunicator.UserExternalIP.ToString() == IP)
        {
            IP = IPAddress.Loopback.ToString();
            __instance.networkAddress = IP;
        }

        Mod.Logger.Msg(ConsoleColor.Blue, $"Connecting to server IP: {IP}");
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

        var stageTime = 0;

        if (__instance.PrivateGame || __instance.type == MenuHandlerGamemodes.MenuType.Local ||
            __instance.type == MenuHandlerGamemodes.MenuType.LocalWireless)
        {
            var num2 = __instance.winsSetup.CurrentValue * 60;
            stageTime = __instance.CurrentGamemode == GameModeEnum.Football ? num2 : 300;
        }

        var currentSelectedLevels = __instance.mapSetup.GetCurrentSelectedLevels(out var isRandomSelected);
        __instance.selectedConfig = GBConfigLoader.CreateRotationConfig(
            (Il2CppStringArray)currentSelectedLevels.ToArray(),
            __instance.CurrentGamemode,
            __instance.CurrentGamemode is GameModeEnum.Football or GameModeEnum.Waves
                ? 1
                : __instance.winsSetup.CurrentValue,
            isRandomSelected,
            stageTime);

        var address = LobbyCommunicator.UserExternalIP ?? IPAddress.Loopback; // Offline safety net

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
                LobbyManager.Instance.LobbyStates.Port = ServerManager.Port;
                LobbyManager.Instance.LobbyStates.UpdateLobbyState();

                var result = new MatchmakingResult(MatchmakingState.Success, "Modded lobby done")
                {
                    IpAddress = address.ToString(),
                    Port = ServerManager.Port,
                    State = MatchmakingState.Success
                };

                LobbyManager.Instance.LobbyStates.MatchmakingComplete(result);
            }));

        return false;
    }

    [HarmonyPatch(typeof(MenuHandlerGamemodes), nameof(MenuHandlerGamemodes.OnStartGame))]
    [HarmonyPrefix]
    private static bool SingleplayerOnlineBypass(MenuHandlerGamemodes __instance)
    {
        var shouldJoinModded = TCPCommunicator.Client?.Connected ?? false;

        if (!shouldJoinModded || !__instance.PrivateGame)
        {
            return true;
        }

        Mod.Logger.Msg(ConsoleColor.Blue, "Bypassing matchmaker auth, player joining modded server");
        __instance.StartGameLogic();
        return false;
    }

    [HarmonyPatch(typeof(NetUtils), nameof(NetUtils.DisconnectPlayer))]
    [HarmonyPrefix]
    private static bool AntiTimeoutDisconnect(NetworkConnection conn, string reason)
    {
        if (!ServerManager.IsServer || reason != "DISCONNECT_PLAYER_LOADING_TIMEOUT")
        {
            return true;
        }

        Mod.Logger.Msg(ConsoleColor.Blue, "Server tried to disconnect player that took too long to load");
        return false;
    }

    [HarmonyPatch(typeof(GameManagerNew), nameof(GameManagerNew.Shutdown))]
    [HarmonyPrefix]
    private static bool ShutdownFix(GameManagerNew __instance, string disconnectMessage)
    {
        if (!ServerManager.IsServer)
        {
            return true;
        }

        __instance.StopAllCoroutines();
        __instance.ActiveGameMode?.Cleanup();
        __instance.ActiveGameMode = null;

        LogCS.Log(
            "[MODDEDSERVER] About to disconnect all players with reason: " + disconnectMessage,
            LogCS.LogType.LogInfo,
            2,
            true);
        NetUtils.DisconnectAllPlayers(disconnectMessage);
        __instance.CurrentState = GameManagerNew.GameState.Inactive;
        __instance._SceneManager.expectedNumPlayers = -1;
        __instance.authPassed = false;
        __instance.gameManagerSetup = false;
        __instance.joinTimer.Active = false;

        return false;
    }

    [HarmonyPatch(typeof(NetServerSceneManager), nameof(NetServerSceneManager.Start))]
    [HarmonyPrefix]
    private static bool JoinTimerFix(NetServerSceneManager __instance)
    {
        if (!ServerManager.IsServer)
        {
            return true;
        }

        Mod.Logger.Msg(ConsoleColor.Blue, "Setting up join timers for modded server");

        __instance.LOAD_TIME_MAX = 120f;
        __instance.READY_TIME_MAX = 30f;

        __instance.timer = __instance.LOAD_TIME_MAX;

        return false;
    }

    [HarmonyPatch(typeof(NetConfigLoader), nameof(NetConfigLoader.LoadServerConfig), [])]
    [HarmonyPostfix]
    private static void ModdedPortApplicator(ref ServerConfig __result)
    {
        if (ServerManager.IsServer)
        {
            __result.ServerPort = ServerManager.Port;
        }
    }

    /*    [HarmonyPatch(typeof(Il2CppCoatsink.Platform.Users), nameof(Il2CppCoatsink.Platform.Users.MaxUsers), MethodType.Getter), HarmonyPostfix]
        public static void MaxUserSetter(ref int __result) => __result = ServerManager.maxPlayers;

        [HarmonyPatch(typeof(Il2CppGB.UI.Beasts.BeastMenuSpawner), nameof(Il2CppGB.UI.Beasts.BeastMenuSpawner.Awake)), HarmonyPrefix]
        public static void SpawnPointAdjuster(Il2CppGB.UI.Beasts.BeastMenuSpawner __instance)
        {
            if (ServerManager.maxPlayers % 8 == 0) // New max players fits into 8.
            {
                List<Transform> toDuplicate = __instance._spawnPoint.ToList<Transform>();
                int extraRows = (ServerManager.maxPlayers / 8) - 1;

                if (extraRows > 0) // More spawns are needed
                {
                    for (int i = 0; i < extraRows; i++)
                    {
                        foreach (Transform spawn in __instance._spawnPoint)
                        {
                            Transform newSpawn = null;
                            newSpawn = GameObject.Instantiate(spawn, spawn.parent, true);
                            newSpawn.GetComponentInChildren<NamebarHandler>()._pointID += 8 * (i + 1); // Add onto the point ID for each row

                            newSpawn.name = "Spawn";
                            newSpawn.position -= Vector3.right * 2f * (i + 1); // Initial offset from prior rows
                            newSpawn.position -= Vector3.right * 2f; // This is the amount of spacing that happens on real Gang Beasts spawnpoints
                            toDuplicate.Add(newSpawn);
                        }
                    }
                }

                __instance._spawnPoint = toDuplicate.ToArray();
                Mod.Logger.Msg(System.Drawing.Color.Beige, $"Finished setting up spawns. New count is {toDuplicate.Count} with an extra row amount of {extraRows}");
            }
        }*/
}
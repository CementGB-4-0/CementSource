using CementGB.Mod.Modules.NetBeard;
using HarmonyLib;
using Il2Cpp;
using Il2CppCoatsink.UnityServices.Matchmaking;
using Il2CppCoreNet.Components.Server;
using Il2CppCoreNet.Objects;
using Il2CppCoreNet.Utils;
using Il2CppGB.Config;
using Il2CppGB.Core;
using Il2CppGB.Game;
using Il2CppGB.Gamemodes;
using Il2CppGB.Menu;
using Il2CppGB.Platform.Lobby;
using Il2CppGB.UI;
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine.Networking;

namespace CementGB.Mod.Patches;

[HarmonyPatch(typeof(MenuHandlerGamemodes), nameof(MenuHandlerGamemodes.StartGameLogic))]
internal static class ModdedServerPatches
{
    internal static bool Prefix(MenuHandlerGamemodes __instance)
    {
        if (__instance.type != MenuHandlerGamemodes.MenuType.Online || !__instance.PrivateGame) return true;

        bool shouldJoinModded = ClientServerCommunicator.IsServerRunning();
        if (!shouldJoinModded) return true;



        int stageTime = 0;

        if (__instance.PrivateGame || __instance.type == MenuHandlerGamemodes.MenuType.Local || __instance.type == MenuHandlerGamemodes.MenuType.LocalWireless)
        {
            int num2 = __instance.winsSetup.CurrentValue * 60;
            stageTime = (__instance.CurrentGamemode == GameModeEnum.Football) ? num2 : 300;
        }

        bool isRandomSelected;
        Il2CppSystem.Collections.Generic.List<string> currentSelectedLevels = __instance.mapSetup.GetCurrentSelectedLevels(out isRandomSelected);
        __instance.selectedConfig = GBConfigLoader.CreateRotationConfig(
            (Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStringArray)currentSelectedLevels.ToArray(),
            __instance.CurrentGamemode,
            (__instance.CurrentGamemode == GameModeEnum.Football || __instance.CurrentGamemode == GameModeEnum.Waves) ? 1 : __instance.winsSetup.CurrentValue,
            isRandomSelected, stageTime);



        IPAddress address = LobbyCommunicator.UserIP;
        if (address == null) address = IPAddress.Loopback; // Offline safety net

        MonoSingleton<Global>.Instance.buttonController.HideButton(InputMapActions.Accept);
        __instance.PopulateVisibleButtons(true);
        LobbyManager.Instance.LobbyStates.CurrentState = LobbyState.State.Ready | LobbyState.State.Joinable | LobbyState.State.Editable | LobbyState.State.Matching;
        LobbyManager.Instance.LobbyStates.UpdateLobbyState();


        LobbyCommunicator.SendLobbyDataToServer(new()
        {
            Gamemode = __instance.CurrentGamemode.GetGameModeID(),
            MapName = (__instance.selectedConfig.GameConfigs.Count == 1) ? __instance.selectedConfig.GameConfigs[0].Map : "random",
            NumberOfWins = __instance.selectedConfig.Wins,
            PrivateGame = true,
            StageTimeLimit = __instance.selectedConfig.StageTimeLimit,
            TotalPlayerCountExclLocal = (uint)LobbyManager.Instance.Players.GetPlayerCount(),
            TotalPlayerCountInclLocal = (uint)LobbyManager.Instance.Players.GetBeastCount()
        });

        __instance.onlineCountdown.StartCountdown(3f, new Action(() =>
        {
            LobbyManager.Instance.LobbyStates.CurrentState = LobbyState.State.Ready | LobbyState.State.InGame;
            LobbyManager.Instance.LobbyStates.UpdateLobbyState();
            LobbyManager.Instance.LocalBeasts.SetupNetMemberContext(true);

            MatchmakingResult result = new MatchmakingResult(MatchmakingState.Success, "Modded lobby done");
            result.IpAddress = address.ToString();
            result.Port = 5999;

            LobbyManager.Instance.LobbyStates.MatchmakingComplete(result);
        }));

        return false;
    }
}



[HarmonyPatch(typeof(MenuHandlerGamemodes), nameof(MenuHandlerGamemodes.OnStartGame))]
internal static class PrivateModdedSupport
{
    private static bool Prefix(MenuHandlerGamemodes __instance)
    {
        bool shouldJoinModded = ClientServerCommunicator.IsServerRunning();

        if (!shouldJoinModded || !__instance.PrivateGame) return true;

        Mod.Logger.Msg(ConsoleColor.Blue, "Bypassing matchmaker auth, player joining modded server");
        __instance.StartGameLogic();
        return false;
    }
}



[HarmonyPatch(typeof(NetUtils), nameof(NetUtils.DisconnectPlayer))]
internal static class AntiPlayerKickOnLoad
{
    public static bool Prefix(NetworkConnection conn, string reason)
    {
        if (ServerManager.IsServer && reason == "DISCONNECT_PLAYER_LOADING_TIMEOUT")
        {
            Mod.Logger.Msg(ConsoleColor.Blue, "Server tried to disconnect player that took too long to load");
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(GameManagerNew), nameof(GameManagerNew.Shutdown))]
internal static class DoRealShutdown
{
    public static bool Prefix(GameManagerNew __instance, string disconnectMessage)
    {
        if (!ServerManager.IsServer) return true;

        __instance.StopAllCoroutines();
        if (__instance.ActiveGameMode != null)
        {
            __instance.ActiveGameMode.Cleanup();
            __instance.ActiveGameMode = null;
        }

        LogCS.Log("[MODDEDSERVER] About to disconnect all players with reason: " + disconnectMessage, LogCS.LogType.LogInfo, 2, true);
        NetUtils.DisconnectAllPlayers(disconnectMessage);
        __instance.CurrentState = GameManagerNew.GameState.Inactive;
        __instance._SceneManager.expectedNumPlayers = -1;
        __instance.authPassed = false;
        __instance.gameManagerSetup = false;
        __instance.joinTimer.Active = false;

        return false;
    }
}
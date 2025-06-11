using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CementGB.Mod.Modules.NetBeard;
using HarmonyLib;
using Il2Cpp;
using Il2CppCoatsink.UnityServices.Matchmaking;
using Il2CppGB.Core;
using Il2CppGB.Gamemodes;
using Il2CppGB.Menu;
using Il2CppGB.Platform.Lobby;
using Il2CppGB.Platform.Lobby.Utils;
using Il2CppGB.UI;
using Il2CppGB.UnityServices.Matchmaking;

namespace CementGB.Mod.Patches;

[HarmonyPatch(typeof(MenuHandlerGamemodes), nameof(MenuHandlerGamemodes.StartGameLogic))]
internal static class JoinModdedServerPatches
{
    internal static bool Prefix(MenuHandlerGamemodes __instance)
    {
        if (__instance.type != MenuHandlerGamemodes.MenuType.Online) return true;

        bool shouldJoinModded = PipeMessenger.IsServerRunning();
        if (!shouldJoinModded) return true;


        IPAddress address = PipeMessenger.UserIP;
        if (address == null) address = IPAddress.Loopback; // Offline safety net

        MonoSingleton<Global>.Instance.buttonController.HideButton(InputMapActions.Accept);
        __instance.PopulateVisibleButtons(true);
        LobbyManager.Instance.LobbyStates.CurrentState = LobbyState.State.Ready | LobbyState.State.Joinable | LobbyState.State.Editable | LobbyState.State.Matching;
        LobbyManager.Instance.LobbyStates.UpdateLobbyState();

        PipeMessenger.SendLobbyDataToServer(new()
        {
            Gamemode = "melee",
            MapName = "Aquarium",
            NumberOfWins = 3,
            PrivateGame = false,
            StageTimeLimit = 60,
            TotalPlayerCountExclLocal = (uint)LobbyManager.Instance.Players.GetPlayerCount(),
            TotalPlayerCountInclLocal = (uint)LobbyManager.Instance.Players.GetBeastCount()
        });;

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

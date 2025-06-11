using System;
using System.Linq;
using CementGB.Mod.Utilities;
using Il2Cpp;
using Il2CppCoatsink.UnityServices;
using Il2CppCoreNet.Contexts;
using Il2CppCoreNet.Model;
using Il2CppCoreNet.Objects;
using Il2CppCoreNet.Utils;
using Il2CppGB.Config;
using Il2CppGB.Core;
using Il2CppGB.Core.Bootstrappers;
using Il2CppGB.Game;
using Il2CppGB.Networking.Objects;
using Il2CppGB.Platform.Lobby;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CementGB.Mod.Modules.NetBeard;

[RegisterTypeInIl2Cpp]
public class ServerManager : MonoBehaviour
{
    /// <summary>
    ///     The default IP setting for the server.
    /// </summary>
    public const string DefaultIP = "127.0.0.1";

    /// <summary>
    ///     The default Port setting for the server.
    /// </summary>
    public const int DefaultPort = 5999;

    private const string ServerLogPrefix = "[SERVER]";

    private static bool _autoLaunchUpdateEnabled = IsClientJoiner && !LaunchArguments.DontAutoStartArg;

    /// <summary>
    ///     True if <see cref="IsServerArg" /> is false, but the ip and port are provided. Unlocks the DevelopmentTestServerUI.
    /// </summary>
    public static bool IsClientJoiner =>
        !LaunchArguments.IsServerArg &&
        (!string.IsNullOrWhiteSpace(LaunchArguments.IpArg) ||
         !string.IsNullOrWhiteSpace(
             LaunchArguments.PortArg)); // TODO: Auto start as client (similar to NetworkBootstrapper.AutoRunServer) if this is true

    /// <summary>
    ///     The IP provided in launch arguments, or <see cref="DefaultIP" /> if none is provided.
    /// </summary>
    public static string IP => string.IsNullOrWhiteSpace(LaunchArguments.IpArg) ? DefaultIP : LaunchArguments.IpArg;

    /// <summary>
    ///     The Port provided in launch arguments, or <see cref="DefaultPort" /> if none is provided.
    /// </summary>
    public static int Port => string.IsNullOrWhiteSpace(LaunchArguments.PortArg) ? DefaultPort : int.Parse(LaunchArguments.PortArg);

    private void Awake()
    {
        LobbyManager.add_onSetupComplete(new Action(OnBoot));

        if (MelonUtils.IsWindows && !Application.isBatchMode)
        {
            if (LaunchArguments.IsForwardedHostArg)
            {
                LoggingUtilities.MessageBox(0,
                    $"Gang Beasts is loading in FWD mode. This will open a server on port {Port} upon creating a local game for LAN or port-forwarded players to join.\nIf this is unintended, please remove the launch argument \"-FWD\" from the Gang Beasts executable.",
                    "Warning", 0);
            }
            else if (IsClientJoiner)
            {
                LoggingUtilities.MessageBox(0,
                    "Gang Beasts is loading in Joiner mode. This will unlock a panel allowing you to join a server with a specific IP and port.\nIf this is unintended, please remove the launch arguments \"-ip\" and \"-port\" from the Gang Beasts executable.",
                    "Warning", 0);
            }
        }

        if (!LaunchArguments.IsServerArg) return;

        Mod.Logger.Msg($"{ServerLogPrefix} Setting up pre-boot dedicated server overrides. . .");
        AudioListener.pause = true;
        Mod.Logger.Msg(ConsoleColor.Green, $"{ServerLogPrefix} Done!");
    }

    private void Update()
    {
        if (!_autoLaunchUpdateEnabled || (!IsClientJoiner && !LaunchArguments.IsForwardedHostArg) ||
            LaunchArguments.DontAutoStartArg || !LobbyManager.Instance || !LobbyManager.Instance._completedSetup ||
            SceneManager.GetActiveScene().name != "Menu") return;

        // TODO: Connect if client, start local game if fwd
        _autoLaunchUpdateEnabled = false;

        /*
        if (IsForwardedHost)
        {

        }
        else if (IsClientJoiner)
        {
            LobbyManager.Instance.LobbyStates.IP = IP;
            LobbyManager.Instance.LobbyStates.Port = Port;
            LobbyManager.Instance.LobbyStates.CurrentState = LobbyState.State.Ready | LobbyState.State.Joinable;
            foreach (var account in LobbyManager.Instance.OnlineBeasts._lobbyBeasts)
            {
                foreach (var localOnlineBeast in account.Value)
                {
                    localOnlineBeast._state = BeastUtils.PlayerState.Ready;
                }
            }
            LobbyManager.Instance.LobbyStates.CurrentState = LobbyState.State.Ready | LobbyState.State.InGame;
            LobbyManager.Instance.LocalBeasts.SetupNetMemberContext(false);
            MonoSingleton<Global>.Instance.UNetManager.LaunchClient(IP, Port);
        }
        */
    }

    private void OnBoot()
    {
        if ((IsClientJoiner && !LaunchArguments.IsForwardedHostArg) || LaunchArguments.IsServerArg)
        {
            NetworkBootstrapper.IsDedicatedServer = LaunchArguments.IsServerArg;
            LobbyManager.Instance.LobbyObject.AddComponent<DevelopmentTestServer>();
            Mod.Logger.Msg(ConsoleColor.Green, "Added DevelopmentTestServer to lobby object.");
        }

        if (LaunchArguments.IsServerArg)
            ServerBoot();
        else if (IsClientJoiner && !LaunchArguments.DontAutoStartArg)
        {
        }
    }

    private static void ServerBoot()
    {
        Mod.Logger.Msg($"{ServerLogPrefix} Setting up server boot...");
        var bootstrapper = FindObjectOfType<NetworkBootstrapper>();
        bootstrapper.AutoRunServer = LaunchArguments.IsServerArg && !LaunchArguments.DontAutoStartArg;
        UnityServicesManager.Instance.Initialise(UnityServicesManager.InitialiseFlags.DedicatedServer, null, "",
            "DGS");
        MonoSingleton<Global>.Instance.LevelLoadSystem.gameObject.SetActive(false);
        NetMemberContext.LocalHostedGame = true;
        GameManagerNew.add_OnGameManagerCreated((Action)SetConfigOnGameManager);
        NetUtils.Model.Subscribe("SERVER_READY", (NetModelItem<NetInt>.ItemHandler)OnServerReady);
        NetUtils.Model.Subscribe("NET_PLAYERS", (NetModelCollection<NetBeast>.ItemHandler)OnPlayerAdded, null,
            (NetModelCollection<NetBeast>.ItemHandler)OnPlayerRemoved);
        NetUtils.Model.Subscribe("NET_MEMBERS", (NetModelCollection<NetMember>.ItemHandler)OnNetMemberAdded, null,
            (NetModelCollection<NetMember>.ItemHandler)OnNetMemberRemoved);
        Mod.Logger.Msg(ConsoleColor.Green, $"{ServerLogPrefix} Done!");
    }

    private static void OnServerReady(NetInt value)
    {
        if (value.Value == 1)
            Mod.Logger.Msg(ConsoleColor.Green, $"{ServerLogPrefix} Ready for players!");
    }

    private static void OnNetMemberAdded(NetMember member)
    {
        Mod.Logger.Msg(
            $"{ServerLogPrefix} NetMember with connection ID {member.ConnectionId} ADDED to model. (key \"NET_MEMBERS\")");
    }

    private static void OnNetMemberRemoved(NetMember member)
    {
        Mod.Logger.Msg(
            $"{ServerLogPrefix} NetMember with connection ID {member.ConnectionId} REMOVED from model. (key \"NET_MEMBERS\")");
    }

    private static void OnPlayerAdded(NetBeast beast)
    {
        Mod.Logger.Msg(
            $"{ServerLogPrefix} {(beast.playerType == NetPlayer.PlayerType.AI ? $"AI Beast with gang ID {beast.GangId}" : $"Player Beast with connection ID {beast.ConnectionId}")} ADDED to model. (key \"NET_PLAYERS\")");
    }

    private static void OnPlayerRemoved(NetBeast beast)
    {
        Mod.Logger.Msg(
            $"{ServerLogPrefix} {(beast.playerType == NetPlayer.PlayerType.AI ? $"AI Beast with gang ID {beast.GangId}" : $"Player Beast with connection ID {beast.ConnectionId}")} removed from model. (key \"NET_PLAYERS\")");
    }

    private static void SetConfigOnGameManager()
    {
        if (!string.IsNullOrWhiteSpace(LaunchArguments.MapArg))
            GameManagerNew.Instance.ChangeRotationConfig(GBConfigLoader.CreateRotationConfig(LaunchArguments.MapArg,
                string.IsNullOrWhiteSpace(LaunchArguments.ModeArg) ? "melee" : LaunchArguments.ModeArg, 8, int.MaxValue));
    }
}
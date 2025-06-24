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
    public const int DefaultPort = 6000;

    private const string ServerLogPrefix = "[SERVER]";

    private static readonly string
        IpArg = CommandLineParser.Instance.GetValueForKey("-ip", false); // set to server via vanilla code

    private static readonly string
        PortArg = CommandLineParser.Instance.GetValueForKey("-port", false); // set to server via vanilla code

    private static bool _autoLaunchUpdateEnabled = IsClientJoiner && !DontAutoStart;

    /// <summary>
    ///     True if the -SERVER argument is passed to the Gang Beasts executable.
    /// </summary>
    public static bool IsServer => Environment.GetCommandLineArgs().Contains("-SERVER");

    /// <summary>
    ///     True if <see cref="IsServer" /> is false, but the ip and port are provided. Unlocks the DevelopmentTestServerUI.
    /// </summary>
    public static bool IsClientJoiner =>
        !IsServer &&
        (!string.IsNullOrWhiteSpace(IpArg) ||
         !string.IsNullOrWhiteSpace(
             PortArg)); // TODO: Auto start as client (similar to NetworkBootstrapper.AutoRunServer) if this is true

    /// <summary>
    ///     True if the -SERVER argument is not passed, but the -FWD argument is. Forwards a local game to an ip and port.
    /// </summary>
    public static bool IsForwardedHost => !IsServer && Environment.GetCommandLineArgs().Contains("-FWD");

    /// <summary>
    ///     True if the -DONT-AUTOSTART argument is passed. Will prevent the server or client from automatically joining the
    ///     server as soon as it can.
    /// </summary>
    public static bool DontAutoStart => Environment.GetCommandLineArgs().Contains("-DONT-AUTOSTART");

    /// <summary>
    ///     The IP provided in launch arguments, or <see cref="DefaultIP" /> if none is provided.
    /// </summary>
    public static string IP => string.IsNullOrWhiteSpace(IpArg) ? DefaultIP : IpArg;

    /// <summary>
    ///     The Port provided in launch arguments, or <see cref="DefaultPort" /> if none is provided.
    /// </summary>
    public static int Port => string.IsNullOrWhiteSpace(PortArg) ? DefaultPort : int.Parse(PortArg);

    /// <summary>
    ///     Should the server load in low graphics mode?
    /// </summary>
    public static bool LowGraphicsMode => Environment.GetCommandLineArgs().Contains("-lowgraphics");


    private void Awake()
    {
        LobbyManager.add_onSetupComplete(new Action(OnBoot));

        if (MelonUtils.IsWindows && !Application.isBatchMode)
        {
            if (IsForwardedHost)
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

        if (!IsServer) return;

        Mod.Logger.Msg($"{ServerLogPrefix} Setting up pre-boot dedicated server overrides. . .");
        AudioListener.pause = true;
        Mod.Logger.Msg(ConsoleColor.Green, $"{ServerLogPrefix} Done!");
    }

    private void Update()
    {
        if (!_autoLaunchUpdateEnabled || (!IsClientJoiner && !IsForwardedHost) ||
            DontAutoStart || !LobbyManager.Instance || !LobbyManager.Instance._completedSetup ||
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
        if ((IsClientJoiner && !IsForwardedHost) || IsServer)
        {
            NetworkBootstrapper.IsDedicatedServer = IsServer;
            LobbyManager.Instance.LobbyObject.AddComponent<DevelopmentTestServer>();
            Mod.Logger.Msg(ConsoleColor.Green, "Added DevelopmentTestServer to lobby object.");
        }

        if (IsServer)
            ServerBoot();
        else if (IsClientJoiner && !DontAutoStart)
        {
        }
    }

    private static void ServerBoot()
    {
        Mod.Logger.Msg($"{ServerLogPrefix} Setting up server boot...");
        var bootstrapper = FindObjectOfType<NetworkBootstrapper>();
        bootstrapper.AutoRunServer = IsServer && !DontAutoStart;
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
        if (!string.IsNullOrWhiteSpace(Mod.MapArg))
            GameManagerNew.Instance.ChangeRotationConfig(GBConfigLoader.CreateRotationConfig(Mod.MapArg,
                string.IsNullOrWhiteSpace(Mod.ModeArg) ? "melee" : Mod.ModeArg, 8, int.MaxValue));
    }
}
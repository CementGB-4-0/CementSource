using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CementGB.Mod.Utilities;
using Il2Cpp;
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
using Open.Nat;
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
    ///     True if IsServer is true and the -pfwd argument is added. Attempts to automatically port-forward the server instance using UPnP.
    ///     This may be disabled on some networks, so it isn't for everybody.
    /// </summary>
    public static bool IsForwardedHost => IsServer && Environment.GetCommandLineArgs().Contains("-pfwd");

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

    // public static int maxPlayers = 16;

    private void Awake()
    {
        /*        PlatformEvents.add_OnLobbyCreatingEvent(new Action(() =>
                {
                    Global.NetworkMaxPlayers = (ushort)maxPlayers;
                    Users.MaxUsers = maxPlayers;
                }));*/

        LobbyManager.add_onSetupComplete(new Action(OnBoot));

        if (MelonUtils.IsWindows && !Application.isBatchMode)
        {
            if (IsClientJoiner)
            {
                _ = LoggingUtilities.MessageBox(
                    0,
                    "Gang Beasts is loading in Joiner mode. This will unlock a panel allowing you to join a server with a specific IP and port.\nIf this is unintended, please remove the launch arguments \"-ip\" and \"-port\" from the Gang Beasts executable.",
                    "Warning",
                    0);
            }
        }

        if (!IsServer)
        {
            return;
        }

        Mod.Logger.Msg($"{ServerLogPrefix} Setting up pre-boot dedicated server overrides. . .");
        AudioListener.pause = true;
        Mod.Logger.Msg(ConsoleColor.Green, $"{ServerLogPrefix} Done!");
    }

    private void Update()
    {
        if (!_autoLaunchUpdateEnabled || !IsClientJoiner ||
            DontAutoStart || !LobbyManager.Instance || !LobbyManager.Instance._completedSetup ||
            SceneManager.GetActiveScene().name != "Menu")
        {
            return;
        }

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
        if (IsClientJoiner || IsServer)
        {
            NetworkBootstrapper.IsDedicatedServer = IsServer;
            _ = LobbyManager.Instance.LobbyObject.AddComponent<DevelopmentTestServer>();
            Mod.Logger.Msg(ConsoleColor.Green, "Added DevelopmentTestServer to lobby object.");
        }

        if (IsServer)
        {
            ServerBoot();
        }
    }

    private static async void ServerBoot()
    {
        Mod.Logger.Msg($"{ServerLogPrefix} Setting up server boot...");
        var bootstrapper = FindObjectOfType<NetworkBootstrapper>();
        bootstrapper.AutoRunServer = IsServer && !DontAutoStart;
        // UnityServicesManager.Instance.Initialise(UnityServicesManager.InitialiseFlags.DedicatedServer, null, "", "DGS");
        MonoSingleton<Global>.Instance.LevelLoadSystem.gameObject.SetActive(false);
        NetMemberContext.LocalHostedGame = true;
        GameManagerNew.add_OnGameManagerCreated((Action)SetConfigOnGameManager);
        NetUtils.Model.Subscribe("SERVER_READY", (NetModelItem<NetInt>.ItemHandler)OnServerReady);
        NetUtils.Model.Subscribe(
            "NET_PLAYERS",
            (NetModelCollection<NetBeast>.ItemHandler)OnPlayerAdded,
            null,
            (NetModelCollection<NetBeast>.ItemHandler)OnPlayerRemoved);
        NetUtils.Model.Subscribe(
            "NET_MEMBERS",
            (NetModelCollection<NetMember>.ItemHandler)OnNetMemberAdded,
            null,
            (NetModelCollection<NetMember>.ItemHandler)OnNetMemberRemoved);
        if (IsForwardedHost)
        {
            var forwardExternalIP = await OpenPort(Port, Port, Protocol.Udp, "NetBeard: Modded Gang Beasts Server");
            if (forwardExternalIP != null)
            {
                Mod.Logger.Msg(ConsoleColor.Green, $"{ServerLogPrefix} Server successfully forwarded to address {forwardExternalIP}:{Port}");
                LobbyCommunicator.UserIP = forwardExternalIP;
            }
        }
        Mod.Logger.Msg(ConsoleColor.Green, $"{ServerLogPrefix} Done!");
    }

    private static async Task<IPAddress?> OpenPort(int internalPort, int externalPort, Protocol protocol, string description)
    {
        try
        {
            var natDiscoverer = new NatDiscoverer();
            var cancellationTokenSource = new CancellationTokenSource(5000);

            var device = await natDiscoverer.DiscoverDeviceAsync(PortMapper.Upnp, cancellationTokenSource);

            if (device != null)
            {
                var externalIp = await device.GetExternalIPAsync();
                var mapping = new Mapping(protocol, internalPort, externalPort, description);
                await device.CreatePortMapAsync(mapping);
                return externalIp;
            }
        }
        catch (NatDeviceNotFoundException ex)
        {
            Mod.Logger.Error($"No UPnP-enabled NAT device found or discovery timed out. {ex}");
        }
        catch (Exception ex)
        {
            Mod.Logger.Error($"An error occurred attempting to port forward: {ex}");
        }

        return null;
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
        {
            GameManagerNew.Instance.ChangeRotationConfig(
                GBConfigLoader.CreateRotationConfig(
                    Mod.MapArg,
                    string.IsNullOrWhiteSpace(Mod.ModeArg) ? "melee" : Mod.ModeArg,
                    8,
                    int.MaxValue));
        }
    }
}
using System.Net;
using Il2Cpp;
using Il2CppCoreNet.Contexts;
using Il2CppCoreNet.Model;
using Il2CppCoreNet.Objects;
using Il2CppCoreNet.Utils;
using Il2CppCS.CorePlatform;
using Il2CppGB.Config;
using Il2CppGB.Core;
using Il2CppGB.Core.Bootstrappers;
using Il2CppGB.Game;
using Il2CppGB.Networking.Objects;
using Il2CppGB.Platform.Lobby;
using Il2CppGB.UI.Beasts;
using MelonLoader;
using Open.Nat;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CementGB.Modules.NetBeardModule;

public class NetBeardModule : InstancedCementModule
{
    /// <summary>
    ///     The default IP setting for the server.
    /// </summary>
    public const string DefaultIP = "127.0.0.1";

    /// <summary>
    ///     The default Port setting for the server.
    /// </summary>
    public const int DefaultPort = 5999;

    public const string ServerLogPrefix = "[SERVER]";

    public static readonly string?
        IpArg = CommandLineParser.Instance.GetValueForKey("-ip", false); // set to server via vanilla code

    public static readonly string?
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
    ///     True if IsServer is true and the -pfwd argument is added. Attempts to automatically port-forward the server
    ///     instance using UPnP.
    ///     This may be disabled on some networks, so it isn't for everybody.
    /// </summary>
    public static bool PortForward => IsServer && Environment.GetCommandLineArgs().Contains("-pfwd");

    /// <summary>
    ///     True if the -DONT-AUTOSTART argument is passed. Will prevent the server or client from automatically joining the
    ///     server as soon as it can.
    /// </summary>
    public static bool DontAutoStart => Environment.GetCommandLineArgs().Contains("-DONT-AUTOSTART");

    /// <summary>
    ///     The IP provided in launch arguments, or <see cref="DefaultIP" /> if none is provided/game is server.
    /// </summary>
    public static string IP => string.IsNullOrWhiteSpace(IpArg) || IsServer ? DefaultIP : IpArg;

    /// <summary>
    ///     The Port provided in launch arguments, or <see cref="DefaultPort" /> if none is provided.
    /// </summary>
    public static int Port => string.IsNullOrWhiteSpace(PortArg) ? DefaultPort : int.Parse(PortArg);

    /// <summary>
    ///     Should the server load in low graphics mode? Will also cap the server to 60fps.
    /// </summary>
    public static bool LowGraphicsMode => Environment.GetCommandLineArgs().Contains("-lowgraphics");

    internal new static MelonLogger.Instance? Logger => GetModule<NetBeardModule>()?.Logger;

    // public static int maxPlayers = 16;

    protected override void OnInitialize()
    {
        /*        PlatformEvents.add_OnLobbyCreatingEvent(new Action(() =>
                {
                    Global.NetworkMaxPlayers = (ushort)maxPlayers;
                    Users.MaxUsers = maxPlayers;
                }));*/

        LobbyCommunicator.Awake();
        TCPCommunicator.Init();
        LobbyManager.add_onSetupComplete(new Action(OnBoot));

        if (!IsServer)
            return;

        Logger?.Msg($"{ServerLogPrefix} Setting up pre-boot dedicated server overrides. . .");
        AudioListener.pause = true;
        Logger?.Msg(ConsoleColor.Green, $"{ServerLogPrefix} Done!");
    }

    private void OnBoot()
    {
        if (IsClientJoiner || IsServer)
        {
            NetworkBootstrapper.IsDedicatedServer = IsServer;
            _ = LobbyManager.Instance.LobbyObject.AddComponent<DevelopmentTestServer>();
            Logger?.Msg(ConsoleColor.Green, "Added DevelopmentTestServer to lobby object.");
        }

        if (IsServer)
            ServerBoot();
        else if (IsClientJoiner && !DontAutoStart)
            PlatformEvents.add_OnGameSetup((PlatformEvents.PlatformVoidEventDel)OnSetupComplete);

        if (Application.isBatchMode)
            MelonEvents.OnUpdate.Subscribe(RemoveRendering);
    }

    private void OnSetupComplete()
    {
        if (!BasePlatformManager.Instance.IsJoiningLobby && !BasePlatformManager.Instance.IsInLobby)
        {
            LobbyManager.Instance.LobbyStates.SelfState = LobbyState.Game.Online;
            BasePlatformManager.Instance.CreateLobby(LOBBY_TYPE.PUBLIC, Global.NetworkMaxPlayers);
        }

        LobbyManager.Instance.LobbyStates.CurrentState = LobbyState.State.Ready | LobbyState.State.InGame;
        LobbyManager.Instance.LobbyStates.UpdateLobbyState();
        LobbyManager.Instance.LocalBeasts.GetPlayerInfo(0).CurrentState = BeastUtils.PlayerState.Ready;
        LobbyManager.Instance.LocalBeasts.SetupNetMemberContext(true);
        MonoSingleton<Global>.Instance.UNetManager.LaunchClient(IP, Port);
    }

    private async void ServerBoot()
    {
        Logger?.Msg($"{ServerLogPrefix} Setting up server boot...");
        Entrypoint.SkipSplashScreens = true;
        var bootstrapper = Object.FindObjectOfType<NetworkBootstrapper>();
        bootstrapper.AutoRunServer = IsServer && !DontAutoStart;
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

        if (PortForward)
        {
            var forwardExternalIPUdp =
                await OpenPort(Port, Port, Protocol.Udp, "NetBeard: Modded Gang Beasts Server (UDP)");
            if (forwardExternalIPUdp != null)
            {
                Entrypoint.Logger.Msg(ConsoleColor.Green,
                    $"{ServerLogPrefix} Server successfully forwarded to address {forwardExternalIPUdp}:{Port} (UDP)");
                LobbyCommunicator.UserExternalIP = forwardExternalIPUdp;
            }

            var forwardExternalIP =
                await OpenPort(Port, Port, Protocol.Udp, "NetBeard: Modded Gang Beasts Server (TCP)");
            if (forwardExternalIP != null)
            {
                Entrypoint.Logger.Msg(ConsoleColor.Green,
                    $"{ServerLogPrefix} Server successfully forwarded to address {forwardExternalIP}:{Port} (UDP)");
                LobbyCommunicator.UserExternalIP = forwardExternalIP;
            }
        }

        Entrypoint.Logger.Msg(ConsoleColor.Green, $"{ServerLogPrefix} Done!");
    }

    private static void RemoveRendering()
    {
        foreach (var meshRenderer in Object.FindObjectsOfType<Renderer>())
        {
            meshRenderer.forceRenderingOff = true;
        }

        foreach (var ui in Object.FindObjectsOfType<CanvasRenderer>())
        {
            ui.cull = true;
        }
    }

    private static async Task<IPAddress?> OpenPort(int internalPort, int externalPort, Protocol protocol,
        string description)
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
            Logger?.Error(
                $"Failed to port forward: No UPnP-enabled NAT device found. UPnP or PMP may not be supported or enabled on your router. {ex}");
        }
        catch (OperationCanceledException ex)
        {
            Logger?.Error($"Failed to port forward: NAT device discovery timed out. {ex}");
        }
        catch (Exception ex)
        {
            Logger?.Error($"An error occurred attempting to port forward: {ex}");
        }

        return null;
    }

    private void OnServerReady(NetInt value)
    {
        if (value.Value == 1)
            Logger?.Msg(ConsoleColor.Green, $"{ServerLogPrefix} Ready for players!");
    }

    private void OnNetMemberAdded(NetMember member)
    {
        Logger?.Msg(
            $"{ServerLogPrefix} NetMember with connection ID {member.ConnectionId} ADDED to model. (key \"NET_MEMBERS\")");
    }

    private void OnNetMemberRemoved(NetMember member)
    {
        Logger?.Msg(
            $"{ServerLogPrefix} NetMember with connection ID {member.ConnectionId} REMOVED from model. (key \"NET_MEMBERS\")");
    }

    private void OnPlayerAdded(NetBeast beast)
    {
        Logger?.Msg(
            $"{ServerLogPrefix} {(beast.playerType == NetPlayer.PlayerType.AI ? $"AI Beast with gang ID {beast.GangId}" : $"Player Beast with connection ID {beast.ConnectionId}")} ADDED to model. (key \"NET_PLAYERS\")");
    }

    private void OnPlayerRemoved(NetBeast beast)
    {
        Logger?.Msg(
            $"{ServerLogPrefix} {(beast.playerType == NetPlayer.PlayerType.AI ? $"AI Beast with gang ID {beast.GangId}" : $"Player Beast with connection ID {beast.ConnectionId}")} removed from model. (key \"NET_PLAYERS\")");
    }

    private static void SetConfigOnGameManager()
    {
        if (!string.IsNullOrWhiteSpace(Entrypoint.MapArg))
        {
            GameManagerNew.Instance.ChangeRotationConfig(
                GBConfigLoader.CreateRotationConfig(
                    Entrypoint.MapArg,
                    string.IsNullOrWhiteSpace(Entrypoint.ModeArg) ? "melee" : Entrypoint.ModeArg,
                    8,
                    int.MaxValue));
        }
    }
}
using System.Net;
using System.Reflection;
using CementGB.Utilities;
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
using MelonLoader.Utils;
using Open.Nat;
using Steamworks;
using UnityEngine;
using Object = UnityEngine.Object;
using SteamClient = Steamworks.SteamClient;

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

    protected override void OnUpdate()
    {
        if (SteamClient.IsValid)
        {
            SteamClient.RunCallbacks();
        }
        else if (SteamServer.IsValid)
        {
            SteamServer.RunCallbacks();
        }
    }

    private static void MoveUnstrippedSteamAPI()
    {
        var pluginsDirPath = Path.Combine(Application.dataPath, "Plugins", "x86_64");
        if (!Directory.Exists(pluginsDirPath))
        {
            Logger?.Warning($"Failed to find path {pluginsDirPath} for API unstripping, some Steam API functions may not work correctly!");
            return;
        }

        var dllFilePath = Path.Combine(pluginsDirPath, "steam_api64.dll");
        if (!File.Exists($"{dllFilePath}.bak"))
        {
            Logger?.Msg($"Loading unstripped steam_api64.dll from assembly {Assembly.GetExecutingAssembly().FullName}. . .");
            File.Move(dllFilePath, dllFilePath + ".bak");
            EmbeddedUtilities.WriteResourceToFile("CementGB.NetBeardModule.Assets.steam_api64.dll", dllFilePath);
            Logger?.Msg(ConsoleColor.Green, "Done!");
        }
    }

    private void OnBoot()
    {
        if (IsClientJoiner || IsServer)
        {
            _ = LobbyManager.Instance.LobbyObject.AddComponent<DevelopmentTestServer>(); 
            Logger?.Msg(ConsoleColor.Green, "Added DevelopmentTestServer to lobby object.");
        }

        if (IsServer)
            ServerBoot();
        else if (IsClientJoiner && !DontAutoStart)
        {
            ClientBoot();
            PlatformEvents.add_OnGameSetup((PlatformEvents.PlatformVoidEventDel)OnSetupComplete);
        }

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

    private void ClientBoot()
    {
        Logger?.Msg("Setting up Steam matchmaking client-side. . .");
        try
        {
            SteamClient.Init(285900, false);
            MelonEvents.OnApplicationQuit.Subscribe(OnApplicationQuitClient);
        }
        catch (Exception ex)
        {
            Logger?.Error("Failed to init Facepunch.Steamworks client! ", ex);
        }
        Logger?.Msg(ConsoleColor.Green, "Done!");
    }

    private void OnApplicationQuitClient()
    {
        if (SteamClient.IsValid)
        {
            SteamClient.Shutdown();
        }
    }

    private void OnApplicationQuitServer()
    {
        if (SteamServer.IsValid)
        {
            SteamServer.LogOff();
            SteamServer.Shutdown();
        }
    }

    private async void ServerBoot()
    {
        Logger?.Msg($"{ServerLogPrefix} Setting up server boot...");
        NetworkBootstrapper.IsDedicatedServer = true;
        //NetworkBootstrapper.IsOfficialServer = true;
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
            var forwardExternalIP = await OpenPort(Port, Port, Protocol.Udp, "NetBeard: Modded Gang Beasts Server");
            if (forwardExternalIP != null)
            {
                Mod.Logger.Msg(ConsoleColor.Green,
                    $"{ServerLogPrefix} Server successfully forwarded to address {forwardExternalIP}:{Port} (UDP)");
                LobbyCommunicator.UserExternalIP = forwardExternalIP;
            }
        }
        
        var steamConfig = new SteamServerInit()
        {
            DedicatedServer = true,
            GamePort = (ushort)Port,
            GameDescription = "Gang Beasts: \"NetBeard\" Modded Server",
            IpAddress = LobbyCommunicator.UserExternalIP,
            ModDir = MelonEnvironment.GameRootDirectory,
            Secure = true,
            VersionString = MyPluginInfo.Version
        }.WithRandomSteamPort();

        SteamServer.Init(285900, steamConfig, false);
        SteamServer.LogOnAnonymous();
        MelonEvents.OnApplicationQuit.Subscribe(OnApplicationQuitServer);
        
        Mod.Logger.Msg(ConsoleColor.Green, $"{ServerLogPrefix} Done!");
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
            Logger?.Error($"Failed to port forward: No UPnP-enabled NAT device found or discovery timed out. {ex}");
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
        {
            Logger?.Msg(ConsoleColor.Green, $"{ServerLogPrefix} Ready for players!");
        }
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
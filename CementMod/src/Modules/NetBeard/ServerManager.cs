using System;
using System.Linq;
using CementGB.Mod.Utilities;
using Il2Cpp;
using Il2CppCoatsink.UnityServices;
using Il2CppCoreNet.Contexts;
using Il2CppGB.Config;
using Il2CppGB.Core;
using Il2CppGB.Core.Bootstrappers;
using Il2CppGB.Game;
using Il2CppGB.Platform.Lobby;
using Il2CppGB.UI.Beasts;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CementGB.Mod.Modules.NetBeard;

[RegisterTypeInIl2Cpp]
public class ServerManager : MonoBehaviour
{
    public const string DefaultIP = "127.0.0.1";
    public const int DefaultPort = 5999;

    private static readonly string
        IpArg = CommandLineParser.Instance.GetValueForKey("-ip", false); // set to server via vanilla code

    private static readonly string
        PortArg = CommandLineParser.Instance.GetValueForKey("-port", false); // set to server via vanilla code

    private static bool _autoLaunchUpdateEnabled = IsClientJoiner && !DontAutoStart;

    public static bool IsServer => Environment.GetCommandLineArgs().Contains("-SERVER");

    public static bool IsClientJoiner =>
        !IsServer &&
        (!string.IsNullOrWhiteSpace(IpArg) ||
         !string.IsNullOrWhiteSpace(
             PortArg)); // TODO: Auto start as client (similar to NetworkBootstrapper.AutoRunServer) if this is true

    public static bool IsForwardedHost => !IsServer && Environment.GetCommandLineArgs().Contains("-FWD");
    public static bool DontAutoStart => Environment.GetCommandLineArgs().Contains("-DONT-AUTOSTART");
    public static string IP => string.IsNullOrWhiteSpace(IpArg) ? DefaultIP : IpArg;
    public static int Port => string.IsNullOrWhiteSpace(PortArg) ? DefaultPort : int.Parse(PortArg);

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

        Mod.Logger.Msg("Setting up pre-boot dedicated server overrides. . .");
        AudioListener.pause = true;
        Mod.Logger.Msg(ConsoleColor.Green, "Done!");
    }

    private void Update()
    {
        if (!_autoLaunchUpdateEnabled || (!IsClientJoiner && !IsForwardedHost) ||
            DontAutoStart || !LobbyManager.Instance || !LobbyManager.Instance._completedSetup || SceneManager.GetActiveScene().name != "Menu") return;
        
        // TODO: Connect if client, start local game if fwd
        _autoLaunchUpdateEnabled = false;

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
        Mod.Logger.Msg("Setting up server boot...");
        var bootstrapper = FindObjectOfType<NetworkBootstrapper>();
        bootstrapper.AutoRunServer = IsServer && !DontAutoStart;
        UnityServicesManager.Instance.Initialise(UnityServicesManager.InitialiseFlags.DedicatedServer, null, "",
            "DGS");
        MonoSingleton<Global>.Instance.LevelLoadSystem.gameObject.SetActive(false);
        if (!string.IsNullOrWhiteSpace(Mod.MapArg))
            GameManagerNew.add_OnGameManagerCreated((Action)SetConfigOnGameManager);
        NetMemberContext.LocalHostedGame = true;
        Mod.Logger.Msg(ConsoleColor.Green, "Done!");
    }

    private static void SetConfigOnGameManager()
    {
        GameManagerNew.Instance.ChangeRotationConfig(GBConfigLoader.CreateRotationConfig(Mod.MapArg, "melee", 8, int.MaxValue));
    }
}
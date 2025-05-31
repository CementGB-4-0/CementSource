using System;
using System.Linq;
using CementGB.Mod.Utilities;
using Il2Cpp;
using Il2CppCoatsink.UnityServices;
using Il2CppGB.Core;
using Il2CppGB.Core.Bootstrappers;
using Il2CppGB.Platform.Lobby;
using MelonLoader;
using UnityEngine;
using UnityEngine.Networking;

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

        if (MelonUtils.IsWindows)
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
        NetworkBootstrapper.IsDedicatedServer = true;
        Mod.Logger.Msg(ConsoleColor.Green, "Done!");
    }

    private void Update()
    {
        if (!NetworkClient.active && _autoLaunchUpdateEnabled && IsClientJoiner && CommonHooks.GlobalInitialized)
        {
            // TODO: Connect if client, start local game if fwd
            _autoLaunchUpdateEnabled = false;
        }
    }

    private static void OnBoot()
    {
        if ((IsClientJoiner && !IsForwardedHost) || IsServer)
        {
            UnityServicesManager.Instance.Initialise(UnityServicesManager.InitialiseFlags.DedicatedServer, null, "",
                "DGS");
            NetworkBootstrapper.IsDedicatedServer = IsServer;
            LobbyManager.Instance.LobbyObject.AddComponent<DevelopmentTestServer>();
            Mod.Logger.Msg(ConsoleColor.Green, "Added DevelopmentTestServer to lobby object.");
        }

        if (IsServer)
            ServerBoot();
    }

    private static void ServerBoot()
    {
        Mod.Logger.Msg("Setting up server boot...");
        FindObjectOfType<NetworkBootstrapper>().AutoRunServer = IsServer && !DontAutoStart;
        MonoSingleton<Global>.Instance.LevelLoadSystem.gameObject.SetActive(false);
        Mod.Logger.Msg(ConsoleColor.Green, "Done!");
    }
}
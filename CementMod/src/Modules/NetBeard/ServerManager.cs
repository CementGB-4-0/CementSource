﻿using System;
using System.Linq;
using System.Runtime.InteropServices;
using CementGB.Mod.Utilities;
using Il2Cpp;
using Il2CppCoatsink.UnityServices;
using Il2CppGB.Core.Bootstrappers;
using Il2CppGB.Platform.Lobby;
using MelonLoader;
using UnityEngine;
using UnityEngine.Networking;

namespace CementGB.Mod.Modules.NetBeard;

[RegisterTypeInIl2Cpp]
public class ServerManager : MonoBehaviour
{
    public const string DEFAULT_IP = "127.0.0.1";
    public const int DEFAULT_PORT = 5999;

    private static readonly string
        _ip = CommandLineParser.Instance.GetValueForKey("-ip", false); // set to server via vanilla code

    private static readonly string
        _port = CommandLineParser.Instance.GetValueForKey("-port", false); // set to server via vanilla code

    private static bool _autoLaunchUpdateEnabled = IsClientJoiner && !DontAutoStart;

    public static bool IsServer => Environment.GetCommandLineArgs().Contains("-SERVER");

    public static bool IsClientJoiner =>
        !IsServer &&
        (!string.IsNullOrWhiteSpace(_ip) ||
         !string.IsNullOrWhiteSpace(
             _port)); // TODO: Auto start as client (similar to NetworkBootstrapper.AutoRunServer) if this is true

    public static bool IsForwardedHost => IsClientJoiner && Environment.GetCommandLineArgs().Contains("-FWD");
    public static bool DontAutoStart => Environment.GetCommandLineArgs().Contains("-DONT-AUTOSTART");
    public static string IP => string.IsNullOrWhiteSpace(_ip) ? DEFAULT_IP : _ip;
    public static int Port => string.IsNullOrWhiteSpace(_port) ? DEFAULT_PORT : int.Parse(_port);

    private void Awake()
    {
        LobbyManager.add_onSetupComplete(new Action(OnBoot));

        if (MelonUtils.IsWindows)
        {
            if (IsForwardedHost)
            {
                MessageBox(0,
                    $"Gang Beasts is loading in FWD mode. This will open a server on port {Port} upon creating a local game for LAN or port-forwarded players to join.\nIf this is unintended, please remove the launch argument \"-FWD\" from the Gang Beasts executable.",
                    "Warning", 0);
            }
            else if (IsClientJoiner)
            {
                MessageBox(0,
                    "Gang Beasts is loading in Joiner mode. This will unlock a panel allowing you to join a server with a specific IP and port.\nIf this is unintended, please remove the launch arguments \"-ip\" and \"-port\" from the Gang Beasts executable.",
                    "Warning", 0);
            }
        }

        if (IsServer)
        {
            LoggingUtilities.VerboseLog("Setting up pre-boot dedicated server overrides. . .");
            AudioListener.pause = true;
            NetworkBootstrapper.IsDedicatedServer = true;
            LoggingUtilities.VerboseLog(ConsoleColor.DarkGreen, "Done!");
        }
    }

    private void Update()
    {
        if (!NetworkClient.active && _autoLaunchUpdateEnabled && IsClientJoiner && CommonHooks.GlobalInitialized)
        {
            // TODO: Connect if client, start local game if fwd
            _autoLaunchUpdateEnabled = false;
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern IntPtr MessageBox(int hWnd, string text, string caption, uint type);

    private static void OnBoot()
    {
        if ((IsClientJoiner && !IsForwardedHost) || IsServer)
        {
            LobbyManager.Instance.LobbyObject.AddComponent<DevelopmentTestServer>();
            LoggingUtilities.VerboseLog("Added DevelopmentTestServer to lobby object.");
        }

        if (IsServer)
        {
            ServerBoot();
        }
    }

    private static void ServerBoot()
    {
        Mod.Logger.Msg("Setting up server boot...");
        FindObjectOfType<NetworkBootstrapper>().AutoRunServer = IsServer && !DontAutoStart;
        UnityServicesManager.Instance.Initialise(UnityServicesManager.InitialiseFlags.DedicatedServer, null, "", "DGS");
        GameObject.Find("Global(Clone)/LevelLoadSystem").SetActive(false);
        Mod.Logger.Msg(ConsoleColor.DarkGreen, "Done!");
    }
}
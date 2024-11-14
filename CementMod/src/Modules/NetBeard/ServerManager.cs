using CementGB.Mod.Utilities;
using Il2Cpp;
using Il2CppCoatsink.UnityServices;
using Il2CppCS.CorePlatform;
using Il2CppGB.Core.Bootstrappers;
using Il2CppGB.Platform.Lobby;
using MelonLoader;
using System;
using System.Linq;
using UnityEngine;

namespace CementGB.Mod.Modules.NetBeard;

[RegisterTypeInIl2Cpp]
public class ServerManager : MonoBehaviour
{
    public const string DEFAULT_IP = "127.0.0.1";
    public const int DEFAULT_PORT = 5999;

    public static bool DontAutoStart => Environment.GetCommandLineArgs().Contains("-DONT-AUTOSTART");
    public static bool IsServer => Environment.GetCommandLineArgs().Contains("-SERVER");
    public static bool IsClientJoiner => !IsServer && (!string.IsNullOrWhiteSpace(_ip) || !string.IsNullOrWhiteSpace(_port)); // TODO: Auto join as client (similar to NetworkBootstrapper.AutoRunServer) if this is true
    public static string IP => string.IsNullOrWhiteSpace(_ip) ? DEFAULT_IP : _ip;
    public static int Port => string.IsNullOrWhiteSpace(_port) ? DEFAULT_PORT : int.Parse(_port);

    private static readonly string _ip = CommandLineParser.Instance.GetValueForKey("-ip", false); // set to server via vanilla code
    private static readonly string _port = CommandLineParser.Instance.GetValueForKey("-port", false); // set to server via vanilla code

    private void Awake()
    {
        PlatformEvents.add_OnPlatformInitializedEvent(new Action(() => OnBoot()));

        if (IsServer)
        {
            LoggingUtilities.VerboseLog("Setting up pre-boot dedicated server overrides. . .");
            AudioListener.pause = true;
            NetworkBootstrapper.IsDedicatedServer = true;
            LoggingUtilities.VerboseLog(ConsoleColor.DarkGreen, "Done!");
        }
    }

    private static void OnBoot()
    {
        LobbyManager.Instance.LobbyObject.AddComponent<DevelopmentTestServer>();
        LoggingUtilities.VerboseLog("Added DevelopmentTestServer to lobby object.");

        if (IsServer) ServerBoot();
    }

    private static void ServerBoot()
    {
        LoggingUtilities.VerboseLog("Setting up server boot...");
        FindObjectOfType<NetworkBootstrapper>().AutoRunServer = IsServer && !DontAutoStart;
        UnityServicesManager.Instance.Initialise(UnityServicesManager.InitialiseFlags.DedicatedServer, null, "", "DGS");
        GameObject.Find("Global(Clone)/LevelLoadSystem").SetActive(false);
        LoggingUtilities.VerboseLog(ConsoleColor.DarkGreen, "Done!");
    }
}
using CementGB.Mod.Utilities;
using Il2Cpp;
using Il2CppCoatsink.UnityServices;
using Il2CppGB.Core.Bootstrappers;
using Il2CppGB.Platform.Lobby;
using Il2CppGB.Setup;
using Il2CppGB.UI;
using MelonLoader;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace CementGB.Mod.Modules.NetBeard;

[RegisterTypeInIl2Cpp]
public class ServerManager : MonoBehaviour
{
    public const string DEFAULT_IP = "127.0.0.1";
    public const int DEFAULT_PORT = 5999;

    public static bool IsServer => Environment.GetCommandLineArgs().Contains("-SERVER");
    public static bool IsClientJoiner => (!IsServer) && (!string.IsNullOrWhiteSpace(_ip) || !string.IsNullOrWhiteSpace(_port)); // TODO: Auto start as client (similar to NetworkBootstrapper.AutoRunServer) if this is true
    public static bool IsForwardedHost => IsClientJoiner && Environment.GetCommandLineArgs().Contains("-FWD");
    public static bool DontAutoStart => Environment.GetCommandLineArgs().Contains("-DONT-AUTOSTART");
    public static string IP => string.IsNullOrWhiteSpace(_ip) ? DEFAULT_IP : _ip;
    public static int Port => string.IsNullOrWhiteSpace(_port) ? DEFAULT_PORT : int.Parse(_port);

    private static readonly string _ip = CommandLineParser.Instance.GetValueForKey("-ip", false); // set to server via vanilla code
    private static readonly string _port = CommandLineParser.Instance.GetValueForKey("-port", false); // set to server via vanilla code

    private static bool _autoLaunchUpdateEnabled = IsClientJoiner && !DontAutoStart;

    private void Awake()
    {
        LobbyManager.add_onSetupComplete(new Action(OnBoot));

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
        if (!NetworkClient.active && _autoLaunchUpdateEnabled && IsClientJoiner && GlobalSceneLoader.Instance.StartResourcesLoaded)
        {
            // Start local server + local client
            var anyInputOnClick = FindObjectOfType<AnyInputOnClick>();
            if (anyInputOnClick == null)
                return;
            anyInputOnClick.button.onClick.Invoke();

            var localButtonObj = GameObject.Find("Managers/Menu/Main Menu/Canvas/Main Menu/TextSection/Local");
            if (localButtonObj == null)
                return;
            var localButton = localButtonObj.GetComponent<Button>();
            if (localButton == null)
                return;
            localButton.onClick.Invoke();

            var countdownScripts = FindObjectsOfType<MenuHandlerGamemodes>(true);
            foreach (var scr in countdownScripts)
            {
                scr.gameObject.SetActive(true);

                if (scr.type == MenuHandlerGamemodes.MenuType.Local && scr.localCountdown != null)
                {
                    scr.localCountdown.StartTimer();
                }
            }

            _autoLaunchUpdateEnabled = false;

            /*
            if (IsForwardedHost)
                MonoSingleton<Global>.Instance.UNetManager.LaunchHost();
            else
                MonoSingleton<Global>.Instance.UNetManager.LaunchClient(IP, Port);
            */
        }
    }

    private static void OnBoot()
    {
        LobbyManager.Instance.LobbyObject.AddComponent<DevelopmentTestServer>();
        LoggingUtilities.VerboseLog("Added DevelopmentTestServer to lobby object.");

        if (IsServer) ServerBoot();
        else
            UnityServicesManager.Instance.Initialise(UnityServicesManager.InitialiseFlags.GameClient, null, "", "hello-world");
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
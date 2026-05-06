using Il2Cpp;
using Il2CppCoreNet.Contexts;
using Il2CppGB.Core;
using Il2CppGB.Core.Bootstrappers;
using Il2CppGB.Platform.Lobby;
using MelonLoader;
using UnityEngine;
using NetworkManager = Il2CppCoreNet.NetworkManager;
using Object = UnityEngine.Object;
using Resources = UnityEngine.Resources;

namespace CementGB.NetBeardModule;

public class NetBeardModule : InstancedCementModule
{
    public const string ServerLogPrefix = "[SERVER]";
    public const uint ClientAppId = 285900;
    public const uint ServerAppId = 497110;

    internal new static MelonLogger.Instance? Logger => GetModule<NetBeardModule>()?.Logger;

    protected override void OnInitialize()
    {
        NetBeardConfig.DeserializeCurrent();
        NetBeardProps.Init();
        LobbyManager.add_onSetupComplete(new Action(OnBoot));
    }

    private void OnBoot()
    {
        _ = LobbyManager.Instance.LobbyObject.AddComponent<DevelopmentTestServer>();

        if (NetBeardProps.IsServer)
            ServerBoot();

        if (Application.isBatchMode)
            MelonEvents.OnUpdate.Subscribe(RemoveRendering);
    }

    private static void ServerBoot()
    {
        Logger?.Msg($"{ServerLogPrefix} Setting up server boot...");

        CementPreferences.SkipSplashes = true;
        var bootstrapper = Object.FindObjectOfType<NetworkBootstrapper>();
        bootstrapper.AutoRunServer = true;
        NetworkBootstrapper.IsDedicatedServer = true;
        NetworkBootstrapper.IsOfficialServer = true;
        MonoSingleton<Global>.Instance.LevelLoadSystem.gameObject.SetActive(false);
        NetMemberContext.LocalHostedGame = NetBeardConfig.Current.AllowDebugSpawning;
        NetworkManager.add_OnServerStarted((NetworkManager.Handler)OnServerStarted);

        Logger?.Msg(ConsoleColor.Green, $"{ServerLogPrefix} Done!");
    }

    private static async void OnServerStarted()
    {
        Logger?.Msg(ConsoleColor.Green, $"{ServerLogPrefix} Server started on port {NetBeardProps.Port}!");
    }

    private static void RemoveRendering()
    {
        foreach (var meshRenderer in Resources.FindObjectsOfTypeAll<Renderer>())
        {
            meshRenderer.forceRenderingOff = true;
        }

        foreach (var ui in Resources.FindObjectsOfTypeAll<CanvasRenderer>())
        {
            ui.cull = true;
        }
    }
}
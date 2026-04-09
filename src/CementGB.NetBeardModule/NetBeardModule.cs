using System.Net;
using Il2Cpp;
using Il2CppCoreNet;
using Il2CppCoreNet.Contexts;
using Il2CppGB.Core;
using Il2CppGB.Core.Bootstrappers;
using Il2CppGB.Platform.Lobby;
using MelonLoader;
using Open.Nat;
using UnityEngine;
using Object = UnityEngine.Object;
using Resources = UnityEngine.Resources;

namespace CementGB.Modules.NetBeardModule;

public class NetBeardModule : InstancedCementModule
{
    public const string ServerLogPrefix = "[SERVER]";

    internal new static MelonLogger.Instance? Logger => GetModule<NetBeardModule>()?.Logger;

    protected override async void OnInitialize()
    {
        NetBeardConfig.DeserializeCurrent();
        CementPreferences.ShouldSkipSplashes += () => NetBeardProps.IsServer;
        LobbyCommunicator.Awake();
        TCPCommunicator.Init();
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
        var bootstrapper = Object.FindObjectOfType<NetworkBootstrapper>();
        bootstrapper.AutoRunServer = !NetBeardProps.DontAutoStart;
        NetworkBootstrapper.IsDedicatedServer = true;
        MonoSingleton<Global>.Instance.LevelLoadSystem.gameObject.SetActive(false);
        NetMemberContext.LocalHostedGame = NetBeardConfig.Current.AllowDebugSpawning;
        NetworkManager.add_OnServerStarted((NetworkManager.Handler)OnServerStarted);

        Logger?.Msg(ConsoleColor.Green, $"{ServerLogPrefix} Done!");
    }

    private static async void OnServerStarted()
    {
        Logger?.Msg(ConsoleColor.Green, $"{ServerLogPrefix} Server started on port {NetBeardProps.Port}!");
        if (NetBeardProps.PortForward)
            await AttemptPortForward();
    }

    private static async Task AttemptPortForward()
    {
        var netDescription = $"NetBeard Server ({NetBeardConfig.Current})";

        var forwardExternalIPUdp = await OpenPort(NetBeardProps.Port, NetBeardProps.Port, Protocol.Udp, netDescription);
        if (forwardExternalIPUdp != null)
        {
            Logger?.Msg(ConsoleColor.Green,
                $"{ServerLogPrefix} Server successfully forwarded to address {forwardExternalIPUdp}:{NetBeardProps.Port} (UDP)");
        }

        var forwardExternalIPTcp = await OpenPort(NetBeardProps.Port, NetBeardProps.Port, Protocol.Tcp, netDescription);
        if (forwardExternalIPTcp != null)
        {
            Logger?.Msg(ConsoleColor.Green,
                $"{ServerLogPrefix} Server successfully forwarded to address {forwardExternalIPTcp}:{NetBeardProps.Port} (TCP)");
        }
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
}
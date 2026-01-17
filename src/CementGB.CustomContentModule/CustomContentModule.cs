using System.Collections;
using GBMDK;
using Il2Cpp;
using Il2CppCoreNet.Contexts;
using Il2CppCS.CorePlatform;
using Il2CppGB.Config;
using Il2CppGB.Core;
using Il2CppGB.Game;
using Il2CppGB.Networking.Delegates;
using Il2CppGB.Platform.Lobby;
using Il2CppGB.Platform.Utils;
using Il2CppGB.UI.Beasts;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using Action = System.Action;
using Object = UnityEngine.Object;

namespace CementGB.Modules.CustomContent;

public class CustomContentModule : InstancedCementModule
{
    internal new static MelonLogger.Instance? Logger { get; private set; } = GetModule<CustomContentModule>()?.Logger;

    protected override void OnInitialize()
    {
        ClassInjector.RegisterTypeInIl2Cpp<CustomMapInfo>();

        PlatformEvents.add_OnPlatformInitializedEvent(
            (PlatformEvents.PlatformVoidEventDel)CustomAddressableRegistration.Initialize);
        if (string.IsNullOrWhiteSpace(Entrypoint.MapArg) && string.IsNullOrWhiteSpace(Entrypoint.ModeArg))
        {
            return;
        }

        Entrypoint.SkipSplashScreens = true;

        PlatformEvents.add_OnGameSetup((PlatformEvents.PlatformVoidEventDel)OnSetupComplete);
        PlatformEvents.add_OnPlatformInitializedEvent(
            (PlatformEvents.PlatformVoidEventDel)StartWaitingToDisableStartScreen);
    }

    private static void StartWaitingToDisableStartScreen()
    {
        MelonCoroutines.Start(WaitToDisableStartScreen());
    }

    private static IEnumerator WaitToDisableStartScreen()
    {
        var startScreen = Object.FindObjectOfType<LoginEvent>();
        while (startScreen == null)
        {
            startScreen = Object.FindObjectOfType<LoginEvent>();
            yield return null;
        }

        startScreen.gameObject.GetComponent<LoginEvent>()?.TryLogin();
    }

    private void OnSetupComplete()
    {
        GameManagerNew.add_OnGameManagerCreated((Handler)SetConfigOnGameManager);
        LobbyManager.Instance.LobbyStates.CurrentState = LobbyState.State.Ready | LobbyState.State.InGame;
        LobbyManager.Instance.LobbyStates.UpdateLobbyState();
        LobbyManager.Instance.LocalBeasts[0].CurrentState = BeastUtils.PlayerState.Ready;
        LobbyManager.Instance.LocalBeasts.SetupNetMemberContext(false);
        MonoSingleton<Global>.Instance.LevelLoadSystem.ShowLoadingScreen(3f, new Action(() =>
        {
            NetMemberContext.LocalHostedGame = true;
            MonoSingleton<Global>.Instance.UNetManager.LaunchHost();
        }));
    }

    private void SetConfigOnGameManager()
    {
        GameManagerNew.Instance.ChangeRotationConfig(
            GBConfigLoader.CreateRotationConfig(Entrypoint.MapArg ?? "random", Entrypoint.ModeArg ?? "melee", 8,
                int.MaxValue));
    }
}
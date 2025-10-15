using CementGB.CustomContent;
using GBMDK;
using Il2Cpp;
using Il2CppCoreNet.Contexts;
using Il2CppCS.CorePlatform;
using Il2CppGB.Config;
using Il2CppGB.Core;
using Il2CppGB.Game;
using Il2CppGB.Networking.Delegates;
using Il2CppGB.Platform.Lobby;
using Il2CppGB.UI.Beasts;
using Il2CppInterop.Runtime.Injection;
using Action = System.Action;

namespace CementGB.Modules.CustomContent;

public class CustomContentModule : InstancedCementModule
{
    protected override void OnInitialize()
    {
        ClassInjector.RegisterTypeInIl2Cpp<CustomMapInfo>();
        
        PlatformEvents.add_OnPlatformInitializedEvent((PlatformEvents.PlatformVoidEventDel)CustomAddressableRegistration.Initialize);
        if (string.IsNullOrWhiteSpace(Mod.MapArg))
        {
            return;
        }
        
        PlatformEvents.add_OnGameSetup((PlatformEvents.PlatformVoidEventDel)OnSetupComplete);
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
            GBConfigLoader.CreateRotationConfig(Mod.MapArg, Mod.ModeArg ?? "melee", 8, int.MaxValue));
    }
}
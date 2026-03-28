using GBMDK;
using Il2Cpp;
using Il2CppCoreNet.Contexts;
using Il2CppCS.CorePlatform;
using Il2CppGB.Config;
using Il2CppGB.Core;
using Il2CppGB.Game;
using Il2CppGB.Gamemodes;
using Il2CppGB.Networking.Delegates;
using Il2CppGB.Platform.Lobby;
using Il2CppGB.UI.Beasts;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using Action = System.Action;
using Resources = UnityEngine.Resources;

namespace CementGB.Modules.CustomContent;

public class CustomContentModule : InstancedCementModule
{
    internal new static MelonLogger.Instance? Logger => GetModule<CustomContentModule>()?.Logger;

    protected override void OnInitialize()
    {
        ClassInjector.RegisterTypeInIl2Cpp<CustomMapInfo>();

        CustomAddressableRegistration.Initialize();
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

    private static void SetConfigOnGameManager()
    {
        const int wins = 8;
        const string fallbackMap = "Grind";

        var mode = string.IsNullOrWhiteSpace(Mod.ModeArg) ? "melee" : Mod.ModeArg;
        var map = Mod.MapArg;
        var config = GBConfigLoader.CreateRotationConfig(map, mode, wins);
        if (map?.ToLower() == "random")
        {
            var setupConfiguration = Resources.FindObjectsOfTypeAll<GameModeSetupConfiguration>().FirstOrDefault();
            if (setupConfiguration == null) return;

            var selectedMaps = setupConfiguration.Maps.GetMapsFor(GameModeHelpers.GamemodeIDToEnum(mode))
                .ToArray().Select(x => x.MapName).ToArray();
            config = GBConfigLoader.CreateRotationConfig(selectedMaps.Length > 0 ? selectedMaps : [fallbackMap]
                , GameModeHelpers.GamemodeIDToEnum(mode), wins,
                true, int.MaxValue);
        }

        if (map?.ToLower() == "modded")
        {
            var selectedModdedMaps = CustomAddressableRegistration.CustomMaps
                .Where(x => x.SceneInfo.allowedGamemodes?.Get().HasFlag(GameModeHelpers.GamemodeIDToEnum(mode)) ==
                            true).Select(x => x.SceneName).ToArray();
            config = GBConfigLoader.CreateRotationConfig(
                selectedModdedMaps.Length > 0 ? selectedModdedMaps : [fallbackMap]
                , GameModeHelpers.GamemodeIDToEnum(mode), wins,
                true, int.MaxValue);
        }

        GameManagerNew.Instance.ChangeRotationConfig(
            config);
    }
}
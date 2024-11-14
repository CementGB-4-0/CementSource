using Il2CppGB.Game;
using MelonLoader;
using System;

namespace CementGB.Mod;

/// <summary>
/// Provides some useful shorthand hooks for certain in-game events.
/// </summary>
public static class CommonHooks
{
    /// <summary>
    /// Fired when the Menu scene loads for the first time in the app's lifespan. Will reset on application quit.
    /// </summary>
    public static event Action OnMenuFirstBoot;
    public static event Action OnGameManagerCreated;
    public static event Action OnRoundStart;
    public static event Action OnRoundEnd;

    private static bool _menuFirstBoot;

    internal static void Initialize()
    {
        MelonEvents.OnSceneWasLoaded.Subscribe(OnSceneWasLoaded);

        GameManagerNew.add_OnGameManagerCreated(new Action(() => { OnGameManagerCreated?.Invoke(); }));
        GameManagerNew.add_OnRoundStart(new Action(() => { OnRoundStart?.Invoke(); }));
        GameManagerNew.add_OnRoundEnd(new Action(() => { OnRoundEnd?.Invoke(); }));
    }

    private static void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName == "Menu" && !_menuFirstBoot)
        {
            _menuFirstBoot = true;
            OnMenuFirstBoot?.Invoke();
        }
    }
}
using System;
using Il2CppGB.Game;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace CementGB.Mod;

/// <summary>
///     Provides some useful shorthand hooks for certain in-game events.
/// </summary>
public static class CommonHooks
{
    private static bool _menuFirstBoot;

    /// <summary>
    ///     Fired when the Menu scene loads for the first time in the app's lifespan. Will reset on application quit.
    /// </summary>
    public static event Action OnMenuFirstBoot;

    public static event Action OnGameManagerCreated;
    public static event Action OnRoundStart;
    public static event Action OnRoundEnd;

    internal static void Initialize()
    {
        SceneManager.add_sceneLoaded((UnityAction<Scene, LoadSceneMode>)OnSceneWasLoaded);

        GameManagerNew.add_OnGameManagerCreated(new Action(() => { OnGameManagerCreated?.Invoke(); }));
        GameManagerNew.add_OnRoundStart(new Action(() => { OnRoundStart?.Invoke(); }));
        GameManagerNew.add_OnRoundEnd(new Action(() => { OnRoundEnd?.Invoke(); }));
    }

    private static void OnSceneWasLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Menu" || _menuFirstBoot) return;

        _menuFirstBoot = true;
        OnMenuFirstBoot?.Invoke();
    }
}
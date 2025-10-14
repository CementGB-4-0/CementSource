using System;
using System.Collections;
using System.IO;
using System.Linq;
using CementGB.Mod.CustomContent;
using CementGB.Mod.Modules.CustomContent.CustomMaps;
using CementGB.Mod.Modules.NetBeard;
using CementGB.Mod.Modules.PoolingModule;
using CementGB.Mod.Utilities;
using Il2Cpp;
using Il2CppCS.CorePlatform;
using Il2CppGB.Config;
using Il2CppGB.Core;
using Il2CppGB.UI;
using Il2CppGB.UI.Menu;
using Il2CppInterop.Runtime;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CementGB.Mod;

/// <summary>
///     The main entrypoint for Cement. This is where everything initializes from. Public members include important paths
///     and MelonMod overrides.
/// </summary>
public class Mod : MelonMod
{
    /// <summary>
    ///     Cement's UserData path ("Gang Beasts\UserData\CementGB"). Created in <see cref="OnInitializeMelon" />.
    /// </summary>
    public static readonly string UserDataPath = Path.Combine(MelonEnvironment.UserDataDirectory, "CementGB");

    /// <summary>
    ///     The path Cement reads custom content from. Custom content must be in its own folder.
    /// </summary>
    /// <remarks>See <see cref="AssetUtilities" /> for modded Addressable helpers.</remarks>
    public static readonly string CustomContentPath = MelonEnvironment.ModsDirectory;

    private static GameObject? _cementCompContainer;
    private static bool _mapArgDidTheThing;

    public static string
        MapArg => CommandLineParser.Instance.GetValueForKey("-map", false);

    public static string
        ModeArg => CommandLineParser.Instance.GetValueForKey("-mode", false);

    public static bool
        DebugArg => Environment.GetCommandLineArgs().Contains("-debug");

    internal static MelonLogger.Instance Logger =>
        Melon<Mod>.Logger; // For if you're tired of the singleton pattern I guess

    private static GameObject CementCompContainer
    {
        get
        {
            if (_cementCompContainer == null)
            {
                _cementCompContainer = new GameObject("CMTSingletons");
            }

            return _cementCompContainer;
        }

        set
        {
            Object.Destroy(_cementCompContainer);
            _cementCompContainer = value;
        }
    }

    /// <summary>
    ///     Fires when Cement loads. Since Cement's MelonPriority is set to a very low number, the mod should initialize before
    ///     any other.
    /// </summary>
    public override void OnInitializeMelon()
    {
        base.OnInitializeMelon();

        // Setup directories and folder structure
        FileStructure();

        // Initialize static classes that need initializing
        CementPreferences.Initialize();
        if (!CementPreferences.VerboseMode)
        {
            Logger.Msg(
                System.ConsoleColor.White,
                "Verbose Mode disabled! Enable verbose mode in UserData/CementGB/CementGB.cfg for more detailed logging.");
        }

        CommonHooks.Initialize();

        // Script.ReloadScripts();
    }

    /// <summary>
    ///     Fires just before Cement is unloaded from the game. Usually this happens when the application closes/crashes, but
    ///     mods can also be unloaded manually.
    ///     This method saves MelonPreferences for Cement via <c>CementPreferences.Deinitialize()</c>, which is an internal
    ///     method.
    /// </summary>
    public override void OnDeinitializeMelon()
    {
        base.OnDeinitializeMelon();

        CementPreferences.Deinitialize();
    }

    /// <summary>
    ///     Fires after the first few Unity MonoBehaviour.Start() methods. Creates components that couldn't be loaded before
    ///     Unity's runtime started.
    /// </summary>
    public override void OnLateInitializeMelon()
    {
        base.OnLateInitializeMelon();

        PlatformEvents.add_OnPlatformInitializedEvent(
            (PlatformEvents.PlatformVoidEventDel)CustomAddressableRegistration.Initialize);
        CommonHooks.OnMenuFirstBoot += MixerFinder.AssignMainMixer;
        CreateCementComponents();
    }

    private static void FileStructure()
    {
        _ = Directory.CreateDirectory(UserDataPath);
        _ = Directory.CreateDirectory(CustomContentPath);
    }

    private static void CreateCementComponents()
    {
        CementCompContainer = new GameObject("CMTSingletons");
        Object.DontDestroyOnLoad(CementCompContainer);
        CementCompContainer.MakePersistent();

        _ = CementCompContainer.AddComponent<NetBeard>();
        _ = CementCompContainer.AddComponent<ServerManager>();
        _ = CementCompContainer.AddComponent<Pool>();
        //CementCompContainer.AddComponent<BeastInput>();
    }

    public override void OnUpdate()
    {
        MainThreadDispatcher.DispatchActions();

        if (SceneManager.GetActiveScene().name == "Menu" &&
            Global.Instance.SceneLoader && !string.IsNullOrWhiteSpace(MapArg) &&
            (!_mapArgDidTheThing || (ServerManager.IsServer && !ServerManager.DontAutoStart)))
        {
            _mapArgDidTheThing = true;
            _ = MelonCoroutines.Start(JumpToMap());
        }
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        foreach (var amb in Object.FindObjectsOfTypeAll(Il2CppType.Of<ScreenSpaceAmbientOcclusion>()))
        {
            var castedAmb = amb.Cast<ScreenSpaceAmbientOcclusion>();
            castedAmb.m_Settings.AfterOpaque = sceneName != "Menu" && !Application.isBatchMode;
            if (Application.isBatchMode)
                castedAmb.m_Settings.Intensity = 0;
        }
    }

    private IEnumerator JumpToMap()
    {
        yield return new WaitForEndOfFrame();
        var localBeastMenuObj = GameObject.Find("Managers/Menu/Beast Menu/Canvas/Local Beast Select Menu");
        while (!localBeastMenuObj)
        {
            yield return new WaitForEndOfFrame();
            localBeastMenuObj = GameObject.Find("Managers/Menu/Beast Menu/Canvas/Local Beast Select Menu");
        }

        if (!localBeastMenuObj.active)
        {
            var menuControllerObj = GameObject.Find("Managers/Menu");
            while (!menuControllerObj)
            {
                yield return new WaitForEndOfFrame();
                menuControllerObj = GameObject.Find("Managers/Menu");
            }

            var controller = menuControllerObj.GetComponent<MenuController>();
            controller.PushScreen(localBeastMenuObj.GetComponent<BaseMenuScreen>());
        }

        var menuHandlerGamemode = localBeastMenuObj.GetComponentInChildren<MenuHandlerGamemodes>();
        menuHandlerGamemode.selectedConfig = GBConfigLoader.CreateRotationConfig(MapArg, "Melee", 8, int.MaxValue);
        menuHandlerGamemode.OnCountdownComplete();
    }
}
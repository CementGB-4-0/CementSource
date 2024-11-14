using CementGB.Mod.Modules.BeastInput;
using CementGB.Mod.Modules.NetBeard;
using CementGB.Mod.Modules.PoolingModule;
using CementGB.Mod.Modules.PreferenceModule;
using CementGB.Mod.Utilities;
using MelonLoader;
using MelonLoader.Utils;
using System.IO;
using Tungsten;
using UnityEngine;

namespace CementGB.Mod;

internal static class BuildInfo
{
    public const string Name = "Cement";
    public const string Author = "HueSamai // dotpy";
    public const string Description = null;
    public const string Company = "CementGB";
    public const string Version = "4.0.0";
    public const string DownloadLink = "https://api.github.com/repos/HueSamai/CementSource/releases/latest";
}

/// <summary>
/// The main entrypoint for Cement. This is where everything initializes from. Public members include important paths and MelonMod overrides.
/// </summary>
public class Mod : MelonMod
{
    /// <summary>
    /// Cement's UserData path ("Gang Beasts\UserData\CementGB"). Created in <see cref="OnInitializeMelon"/>.
    /// </summary>
    public static readonly string userDataPath = Path.Combine(MelonEnvironment.UserDataDirectory, "CementGB");
    /// <summary>
    /// The path Cement reads custom content from. Custom content must be in its own folder.
    /// </summary>
    /// <remarks>See <see cref="AddressableUtilities"/> for modded Addressable helpers.</remarks>
    public static readonly string customContentPath = Path.Combine(userDataPath, "CustomContent");

    internal static GameObject CementCompContainer
    {
        get
        {
            if (_cementCompContainer == null)
                _cementCompContainer = new GameObject("CMTSingletons");
            return _cementCompContainer;
        }
        set
        {
            Object.Destroy(_cementCompContainer);
            _cementCompContainer = value;
        }
    }
    private static GameObject _cementCompContainer;

    /// <summary>
    /// Fires when Cement loads. Since Cement's MelonPriority is set to a very low number, the mod should initialize before any other.
    /// </summary>
    public override void OnInitializeMelon()
    {
        base.OnInitializeMelon();

        // Setup directories and folder structure
        FileStructure();

        // Initialize static classes that need initializing
        CementPreferences.Initialize();
        CommonHooks.Initialize();
        PreferenceModule.Initialize();

        // Load custom content catalogs
        AddressableUtilities.LoadCCCatalogs();

        Script.ReloadScripts();
    }

    /// <summary>
    /// Fires just before Cement is unloaded from the game. Usually this happens when the application closes/crashes, but mods can also be unloaded manually.
    /// This method saves MelonPreferences for Cement via <c>CementPreferences.Deinitialize()</c>, which is an internal method.
    /// </summary>
    public override void OnDeinitializeMelon()
    {
        base.OnDeinitializeMelon();

        CementPreferences.Deinitialize();
    }

    /// <summary>
    /// Fires after the first few Unity MonoBehaviour.Start() methods. Creates components that couldn't be loaded before Unity's runtime started.
    /// </summary>
    public override void OnLateInitializeMelon()
    {
        base.OnLateInitializeMelon();

        CreateCementComponents();
    }

    /// <summary>
    /// Fires every frame.
    /// </summary>
    public override void OnUpdate()
    {
        base.OnUpdate();
        Script.Update();
    }

    public override void OnGUI()
    {
        Script.OnGUI();
    }

    private static void FileStructure()
    {
        Directory.CreateDirectory(userDataPath);
        Directory.CreateDirectory(customContentPath);
        Directory.CreateDirectory(Script.scriptsPath);
    }

    private static void CreateCementComponents()
    {
        CementCompContainer = new("CMTSingletons");
        Object.DontDestroyOnLoad(CementCompContainer);
        CementCompContainer.hideFlags = HideFlags.DontUnloadUnusedAsset;

        CementCompContainer.AddComponent<NetBeard>();
        CementCompContainer.AddComponent<ServerManager>();
        CementCompContainer.AddComponent<Pool>();
        CementCompContainer.AddComponent<BeastInput>();
    }
}
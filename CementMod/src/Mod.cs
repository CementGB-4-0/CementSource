using System.IO;
using CementGB.Mod.Modules.NetBeard;
using CementGB.Mod.Modules.PoolingModule;
using CementGB.Mod.Utilities;
using MelonLoader;
using MelonLoader.Utils;
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

    private static GameObject _cementCompContainer;

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
        CommonHooks.Initialize();

        //Script.ReloadScripts();
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

        CreateCementComponents();

        AssetUtilities.InitializeAddressables();
    }

    private static void FileStructure()
    {
        Directory.CreateDirectory(UserDataPath);
        Directory.CreateDirectory(CustomContentPath);
    }

    private static void CreateCementComponents()
    {
        CementCompContainer = new GameObject("CMTSingletons");
        Object.DontDestroyOnLoad(CementCompContainer);
        CementCompContainer.hideFlags = HideFlags.DontUnloadUnusedAsset;

        //CementCompContainer.AddComponent<NetBeard>();
        CementCompContainer.AddComponent<ServerManager>();
        CementCompContainer.AddComponent<Pool>();
        //CementCompContainer.AddComponent<BeastInput>();
    }
}
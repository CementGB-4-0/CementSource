using CementGB.Utilities;
using CementGB.Modules;
using Il2Cpp;
using Il2CppInterop.Runtime;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace CementGB;

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
    public static readonly string ModulesPath = Path.Combine(MelonEnvironment.UserLibsDirectory, "CementGBModules");

    public static string
        MapArg => CommandLineParser.Instance.GetValueForKey("-map", false);

    public static string
        ModeArg => CommandLineParser.Instance.GetValueForKey("-mode", false);

    public static bool
        DebugArg => Environment.GetCommandLineArgs().Contains("-debug");

    public static MelonLogger.Instance Logger =>
        Melon<Mod>.Logger; // For if you're tired of the singleton pattern I guess

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
        foreach (var file in Directory.GetFiles(ModulesPath, "*.dll", SearchOption.AllDirectories))
        {
            try
            {
                var assembly = System.Reflection.Assembly.LoadFrom(file);
                InstancedCementModule.BootstrapAllCementModulesInAssembly(assembly);
            }
            catch
            {
                Logger.Error($"Failed to auto-load CementGB modules from assembly file {Path.GetFileName(file)}!");
            }
        }
    }

    private static void FileStructure()
    {
        _ = Directory.CreateDirectory(UserDataPath);
        _ = Directory.CreateDirectory(ModulesPath);
    }

    public override void OnUpdate()
    {
        MainThreadDispatcher.DispatchActions();
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        foreach (var amb in Object.FindObjectsOfTypeAll(Il2CppType.Of<ScreenSpaceAmbientOcclusion>()))
        {
            amb.Cast<ScreenSpaceAmbientOcclusion>().m_Settings.AfterOpaque = sceneName != "Menu";
        }
    }
}
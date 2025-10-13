using System.Reflection;
using CementGB.Utilities;
using MelonLoader;

namespace CementGB.Modules;

public abstract class InstancedCementModule
{
    private static readonly List<InstancedCementModule> ModuleHolder = [];

    public static InstancedCementModule? GetModule<T>() where T : InstancedCementModule
    {
        return ModuleHolder.Find(instancedModule => instancedModule.GetType() == typeof(T)) ?? null;
    }

    public static void BootstrapAllCementModulesInAssembly(Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();
        foreach (var moduleType in GetAssemblyModuleTypes(assembly))
        {
            LoggingUtilities.VerboseLog($"Found module type: \"{moduleType.FullName}\" | Bootstrapping module. . .");
            var moduleInstance = BootstrapModule(moduleType);

            if (moduleInstance != null)
                ModuleHolder.Add(moduleInstance);
        }
    }

    public static List<Type> GetAssemblyModuleTypes(Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();
        var assemblyTypes = assembly.GetTypes();

        return [.. assemblyTypes.Where(IsModuleType)];
    }

    private static bool IsModuleType(Type type)
    {
        var baseType = typeof(InstancedCementModule);
        return (baseType.IsAssignableFrom(type) || type == baseType) &&
               type is { IsAbstract: false, IsInterface: false };
    }

    private static InstancedCementModule? BootstrapModule(Type moduleType)
    {
        if (!IsModuleType(moduleType)) return null;

        if (Activator.CreateInstance(moduleType) is not InstancedCementModule instance) return null;
        instance.OnInitialize_Internal();

        return instance;
    }

    public readonly MelonLogger.Instance Logger;

    protected readonly HarmonyLib.Harmony HarmonyInstance;
    protected readonly Assembly ModuleAssembly;

    protected InstancedCementModule()
    {
        HarmonyInstance = new HarmonyLib.Harmony(GetType().Name);
        ModuleAssembly = Assembly.GetCallingAssembly();
        Logger = new MelonLogger.Instance($"(Module){GetType().FullName}");
        SubscribeInternalMethods();
    }

    protected virtual void OnInitialize() { }

    protected virtual void DoManualPatches()
    {
        Mod.Logger.Msg($"Cement Module {GetType().Name} applying patches. . .");
        HarmonyInstance.PatchAll(ModuleAssembly);
        Mod.Logger.Msg(ConsoleColor.Green, "Done!");
    }

    protected virtual void OnUpdate() { }

    private void SubscribeInternalMethods()
    {
        MelonEvents.OnUpdate.Subscribe(OnUpdate);
    }

    private void OnInitialize_Internal()
    {
        DoManualPatches();
        OnInitialize();
    }
}
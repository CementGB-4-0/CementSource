using System.Reflection;
using CementGB.Utilities;
using MelonLoader;

namespace CementGB.Modules;

public abstract class InstancedCementModule
{
    private static readonly List<InstancedCementModule> ModuleHolder = [];

    protected InstancedCementModule()
    {
        HarmonyInstance = new HarmonyLib.Harmony(GetType().FullName);
        ModuleAssembly = Assembly.GetCallingAssembly();
        Logger = new MelonLogger.Instance($"{ModuleAssembly.GetName().Name} ({nameof(InstancedCementModule)})");
        SubscribeInternalMethods();
    }

    public MelonLogger.Instance Logger { get; }

    protected HarmonyLib.Harmony HarmonyInstance { get; }
    protected Assembly ModuleAssembly { get; }

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

    private static List<Type> GetAssemblyModuleTypes(Assembly? assembly = null)
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

    protected virtual void OnInitialize()
    {
    }

    protected virtual void ApplyPatches()
    {
        Entrypoint.Logger.Msg($"Cement Module {GetType().Name} applying patches. . .");
        HarmonyInstance.PatchAll(ModuleAssembly);
        Entrypoint.Logger.Msg(ConsoleColor.Green, "Done!");
    }

    protected virtual void OnUpdate()
    {
    }

    private void SubscribeInternalMethods()
    {
        MelonEvents.OnUpdate.Subscribe(OnUpdate);
    }

    private void OnInitialize_Internal()
    {
        ApplyPatches();
        OnInitialize();
    }
}
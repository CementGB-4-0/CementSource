using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CementGB.Mod.Utilities;
using MelonLoader;

namespace CementGB.Mod.Modules;

public abstract class InstancedCementModule
{
    private static readonly List<InstancedCementModule> ModuleHolder = [];

    public static void BootstrapAllCementModulesInAssembly(Assembly? assembly=null)
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

    public readonly HarmonyLib.Harmony HarmonyInstance;

    protected InstancedCementModule()
    {
        HarmonyInstance = new HarmonyLib.Harmony(GetType().Name);
        SubscribeInternalMethods();
    }

    protected virtual void OnInitialize() { }
    protected virtual void DoManualPatches() { }
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
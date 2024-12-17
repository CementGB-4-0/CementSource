using CementGB.Mod.Utilities;
using HarmonyLib;
using UnityEngine.AddressableAssets;

namespace CementGB.Mod.Patches;


/// <summary>
/// Miracle patch by @Lionmeow on GitHub. THANK YOU!
/// https://github.com/Lionmeow/AcceleratorThings/blob/main/AcceleratorThings/CustomAddressablesPatch.cs
/// </summary>
[HarmonyPatch]
public static class CustomAddressablesPatch
{
    /*
    public static Dictionary<string, UnityEngine.Object> CustomAddressablePaths { get; private set; } = [];

    [HarmonyPatch(typeof(ResourceManager), nameof(ResourceManager.ProvideResource), typeof(IResourceLocation), typeof(Il2CppSystem.Type), typeof(bool))]
    [HarmonyPrefix]
    public static bool ProvideResourcePrefix(ResourceManager __instance, IResourceLocation location, Il2CppSystem.Type desiredType, ref AsyncOperationHandle __result)
    {
        if (location == null)
            return true;

        if (!AssetUtilities.IsModdedKey(location.PrimaryKey))
            return true;

        var firstOrDefault = CustomAddressablePaths[location.PrimaryKey];
        string systemTypeName = desiredType.FullName;
        if (!systemTypeName.StartsWith("UnityEngine"))
        {
            if (desiredType.Namespace == null)
                systemTypeName = systemTypeName.Insert(0, "Il2Cpp.");
            else
                systemTypeName = systemTypeName.Insert(0, "Il2Cpp");
        }

        MethodInfo method = AccessTools.Method(typeof(Il2CppObjectBase), "Cast", [], [AccessTools.TypeByName(systemTypeName)]);
        object result = method.Invoke(firstOrDefault, null);

        MethodInfo inf = AccessTools.Method(typeof(ResourceManager), "CreateCompletedOperationInternal");
        MethodInfo genInf = inf.MakeGenericMethod(AccessTools.TypeByName(systemTypeName));

        var completedOperationInternal = genInf.Invoke(__instance, new[] { result, true, null, false } );

        var asyncOperationHandle = new AsyncOperationHandle(((Il2CppObjectBase)AccessTools.PropertyGetter(completedOperationInternal.GetType(), "InternalOp")
            .Invoke(completedOperationInternal, null)).Cast<IAsyncOperation>(), (int)AccessTools.PropertyGetter(completedOperationInternal.GetType(), 
            "m_Version").Invoke(completedOperationInternal, null)); // this is the worst thing ever actually
        __result = asyncOperationHandle;
        return false;
    }
    */

    // Game has failsafes in order to prevent loading invalid assets, bypass them
    [HarmonyPatch(typeof(AssetReference), "RuntimeKeyIsValid")]
    [HarmonyPrefix]
    public static bool LabelModdedKeysAsValid(AssetReference __instance, ref bool __result)
    {
        var key = __instance.RuntimeKey.ToString();

        if (AssetUtilities.IsModdedKey(__instance.RuntimeKey.ToString()))
        {
            __result = true;
            return false;
        }
        return true;
    }
}
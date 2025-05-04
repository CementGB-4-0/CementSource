using CementGB.Mod.CustomContent;
using HarmonyLib;
using MelonLoader.Utils;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.ResourceManagement.AsyncOperations;
using Resources = Il2CppGB.Core.Resources;

namespace CementGB.Mod.Patches;

[HarmonyPatch]
internal static class CustomAddressablesPatches
{
    private const string ModsDirectoryPropertyName = "MelonLoader.Utils.MelonEnvironment.ModsDirectory";
    
    // Game has failsafes in order to prevent loading invalid assets, bypass them
    [HarmonyPatch(typeof(AssetReference), "RuntimeKeyIsValid")]
    [HarmonyPrefix]
    private static bool LabelModdedKeysAsValid(AssetReference __instance, ref bool __result)
    {
        if (!CustomAddressableRegistration.IsModdedKey(__instance.RuntimeKey.ToString()))
        {
            return true;
        }

        __result = true;
        return false;
    }

    [HarmonyPatch(typeof(Resources.LoadLoadedItem), nameof(Resources.LoadLoadedItem.Load))]
    internal static class LoadLoadedItemPatch
    {
        private static bool Prefix(Resources.LoadLoadedItem __instance, ref AsyncOperationHandle __result)
        {
            if (!CustomAddressableRegistration.IsModdedKey(__instance.Key))
            {
                return true;
            }

            __instance._finishedLoading = AsyncOperationStatus.None;
            __instance._loadHandle = Addressables.LoadAssetAsync<Il2CppSystem.Object>(__instance.Key);

            __result = __instance._loadHandle;
            return false;
        }
    }
    
    [HarmonyPatch(typeof(AddressablesRuntimeProperties), nameof(AddressablesRuntimeProperties.EvaluateProperty))]
    internal static class RuntimePropertiesPatch
    {
        private static bool Prefix(string name, ref string __result)
        {
            if (name != ModsDirectoryPropertyName) return true;
            __result = MelonEnvironment.ModsDirectory;
            return false;
        }
    }
}
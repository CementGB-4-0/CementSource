using CementGB.Mod.Utilities;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CementGB.Mod.Patches;

[HarmonyPatch]
internal static class CustomAddressablesPatches
{
    // Game has failsafes in order to prevent loading invalid assets, bypass them
    [HarmonyPatch(typeof(AssetReference), "RuntimeKeyIsValid")]
    [HarmonyPrefix]
    private static bool LabelModdedKeysAsValid(AssetReference __instance, ref bool __result)
    {
        if (AssetUtilities.IsModdedKey(__instance.RuntimeKey.ToString()))
        {
            __result = true;
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(Il2CppGB.Core.Resources.LoadLoadedItem), nameof(Il2CppGB.Core.Resources.LoadLoadedItem.Load))]
    internal static class LoadLoadedItemPatch
    {
        private static bool Prefix(Il2CppGB.Core.Resources.LoadLoadedItem __instance, ref AsyncOperationHandle __result)
        {
            if (AssetUtilities.IsModdedKey(__instance.Key))
            {
                __instance._finishedLoading = AsyncOperationStatus.None;
                __instance._loadHandle = Addressables.LoadAssetAsync<ScriptableObject>(__instance.Key);

                __result = __instance._loadHandle;
                return false;
            }

            return true;
        }
    }
}
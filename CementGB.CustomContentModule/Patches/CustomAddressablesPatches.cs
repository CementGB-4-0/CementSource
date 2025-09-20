using CementGB.Mod.CustomContent;
using HarmonyLib;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = Il2CppSystem.Object;
using Resources = Il2CppGB.Core.Resources;
using String = Il2CppSystem.String;

namespace CementGB.Modules.CustomContent.Patches;

[HarmonyPatch]
internal static class CustomAddressablesPatches
{
    [HarmonyPatch(typeof(AddressablesImpl), nameof(AddressablesImpl.ResolveInternalId))]
    private static class ResolveInternalIdPatch
    {
        private static void Postfix(string id, ref string __result)
        {
            if (id.StartsWith($"{{{CustomAddressableRegistration.ModsDirectoryPropertyName}}}") &&
                id.EndsWith(".bundle"))
            {
                __result = CustomAddressableRegistration.ResolveModdedInternalId(__result);
            }
        }
    }

    [HarmonyPatch(typeof(AssetReference), nameof(AssetReference.RuntimeKeyIsValid))]
    private static class AssetReferenceRuntimeKeyIsValidPatch
    {
        private static bool Prefix(AssetReference __instance, ref bool __result)
        {
            if (!CustomAddressableRegistration.IsModdedKey(__instance.RuntimeKey.ToString()))
            {
                return true;
            }

            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(AssetReference), nameof(AssetReference.IsValid))]
    private static class AssetReferenceIsValidPatch
    {
        private static bool Prefix(AssetReference __instance, ref bool __result)
        {
            if (!CustomAddressableRegistration.IsModdedKey(__instance.RuntimeKey.ToString()))
            {
                return true;
            }

            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(Resources.LoadLoadedItem), nameof(Resources.LoadLoadedItem.Load))]
    internal static class LoadLoadedItemPatch
    {
        private static bool Prefix(Resources.LoadLoadedItem __instance, ref AsyncOperationHandle __result)
        {
            if (!CustomAddressableRegistration.IsModdedKey(__instance.Key) ||
                string.IsNullOrWhiteSpace(__instance.Key))
            {
                return true;
            }

            __instance._finishedLoading = AsyncOperationStatus.None;
            __instance._loadHandle = Addressables.LoadAssetAsync<Object>((String)__instance.Key);

            __result = __instance._loadHandle;
            return false;
        }
    }
}
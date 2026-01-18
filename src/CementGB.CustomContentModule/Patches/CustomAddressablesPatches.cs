using HarmonyLib;
using MelonLoader;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CementGB.Modules.CustomContent.Patches;

[HarmonyPatch]
internal static class CustomAddressablesPatches
{
    [HarmonyPatch(typeof(GameObject), nameof(GameObject.SetActive))]
    private static class GameObjectSetActivePatch
    {
        private static void Postfix(GameObject __instance, bool value)
        {
            if (value)
                MelonCoroutines.Start(AddressableShaderCache.ReloadAddressableShaders(__instance, false));
        }
    }

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
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(AssetReference), nameof(AssetReference.IsValid))]
    private static class AssetReferenceIsValidPatch
    {
        private static bool Prefix(AssetReference __instance, ref bool __result)
        {
            __result = true;
            return false;
        }
    }
}
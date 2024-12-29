using CementGB.Mod.Utilities;
using HarmonyLib;
using UnityEngine.AddressableAssets;

namespace CementGB.Mod.Patches;

[HarmonyPatch]
internal static class CustomAddressablesPatch
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
}
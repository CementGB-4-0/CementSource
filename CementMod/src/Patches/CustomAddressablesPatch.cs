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
using CementGB.Mod.Utilities;
using HarmonyLib;
using Il2CppGB.Config;
using Il2CppGB.UI;
using Il2CppSystem.Collections.Generic;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace CementGB.Mod.Patches;

internal static class GBConfigLoaderPatch
{
    [HarmonyPatch(typeof(MenuHandlerMaps), nameof(MenuHandlerMaps.GetCurrentSelectedLevels))]
    private static class LoadRotationConfigPatch
    {
        private static void Postfix(MenuHandlerMaps __instance, ref List<string> __result)
        {
            if (__instance.mapList[__instance.currentMapIndex].ToLower() != "random")
                return;

            foreach (var scene in AssetUtilities.GetAllModdedResourceLocationsOfType<SceneInstance>())
            {
                __result.Add(scene.PrimaryKey);
            }
        }
    }
}
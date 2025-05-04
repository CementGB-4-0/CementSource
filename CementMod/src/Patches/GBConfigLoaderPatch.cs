using CementGB.Mod.CustomContent;
using CementGB.Mod.Utilities;
using GBMDK;
using HarmonyLib;
using Il2CppGB.UI;
using Il2CppSystem.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace CementGB.Mod.Patches;

internal static class GBConfigLoaderPatch
{
    [HarmonyPatch(typeof(MenuHandlerMaps), nameof(MenuHandlerMaps.GetCurrentSelectedLevels))]
    private static class GetCurrentSelectedLevelsPatch
    {
        private static void Postfix(MenuHandlerMaps __instance, ref List<string> __result)
        {
            var masterMenuHandlers = Object.FindObjectsOfType<MenuHandlerGamemodes>();

            foreach (var masterMenuHandler in masterMenuHandlers)
            {
                if (__instance.mapList[__instance.currentMapIndex].ToLower() != "random" ||
                    masterMenuHandler.type != MenuHandlerGamemodes.MenuType.Local)
                {
                    continue; // Either map is not set to random or game is not local; don't do patch
                }

                foreach (var scene in CustomAddressableRegistration.CustomMaps)
                {
                    var result = scene.sceneInfo;
                    
                    if (!result.allowedGamemodes.Get().HasFlag(masterMenuHandler.CurrentGamemode))
                    {
                        // Custom scene data has no waves data attached; this is not a waves map (or the gamemode isn't melee or waves)
                        continue;
                    }

                    __result.Add(scene.SceneData._sceneRef.RuntimeKey.ToString());
                }
            }
        }
    }
}
using HarmonyLib;
using Il2CppGB.UI;
using UnityEngine;

namespace CementGB.Modules.CustomContent.Patches;

[HarmonyPatch]
internal static class GBConfigLoaderPatch
{
    [HarmonyPatch(typeof(MenuHandlerMaps), nameof(MenuHandlerMaps.GetCurrentSelectedLevels))]
    private static class GetCurrentSelectedLevelsPatch
    {
        private static void Postfix(MenuHandlerMaps __instance, ref bool random,
            ref Il2CppSystem.Collections.Generic.List<string> __result)
        {
            if (__instance.mapList[__instance.currentMapIndex].ToLower() !=
                "modded")
            {
                return; // map is not set to modded; don't do patch
            }

            __result.Clear();
            random = true;

            var masterMenuHandlers = Resources.FindObjectsOfTypeAll<MenuHandlerGamemodes>();

            foreach (var masterMenuHandler in masterMenuHandlers)
            {
                foreach (var scene in CustomAddressableRegistration.CustomMaps)
                {
                    var sceneInfo = scene.SceneInfo;
                    var gamemode = masterMenuHandler.CurrentGamemode;

                    if (sceneInfo.allowedGamemodes?.Get().HasFlag(gamemode) != true)
                        continue;

                    __result.Add(scene.SceneName);
                }
            }
        }
    }
}
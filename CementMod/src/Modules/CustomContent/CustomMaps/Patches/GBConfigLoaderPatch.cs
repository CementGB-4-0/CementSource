using HarmonyLib;
using Il2CppGB.Gamemodes;
using Il2CppGB.UI;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using Index = System.Index;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace CementGB.Mod.Modules.CustomContent.CustomMaps.Patches;

internal static class GBConfigLoaderPatch
{
    [HarmonyPatch(typeof(MenuHandlerMaps), nameof(MenuHandlerMaps.GetCurrentSelectedLevels))]
    private static class GetCurrentSelectedLevelsPatch
    {
        private static void Postfix(MenuHandlerMaps __instance, ref List<string> __result)
        {
            if (((String)__instance.mapList[(Index)__instance.currentMapIndex]).ToLower() != "random")
                return; // map is not set to random; don't do patch

            var masterMenuHandlers = Object.FindObjectsOfType<MenuHandlerGamemodes>();

            foreach (var masterMenuHandler in masterMenuHandlers)
            {
                if (masterMenuHandler.type == MenuHandlerGamemodes.MenuType.Online)
                    continue;

                foreach (var scene in CustomAddressableRegistration.CustomMaps)
                {
                    var result = scene.SceneInfo;
                    if (!result && masterMenuHandler.CurrentGamemode != GameModeEnum.Melee)
                        continue;

                    if (result && !result.allowedGamemodes.Get().HasFlag(masterMenuHandler.CurrentGamemode))
                        continue;

                    __result.Insert(Random.Range(0, __result.Count - 1), scene.SceneName);
                }
            }
        }
    }
}
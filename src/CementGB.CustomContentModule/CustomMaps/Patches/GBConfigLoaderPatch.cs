using HarmonyLib;
using Il2CppGB.Gamemodes;
using Il2CppGB.UI;
using Object = UnityEngine.Object;

namespace CementGB.Modules.CustomContent.Patches;

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

            var masterMenuHandlers = Object.FindObjectsOfType<MenuHandlerGamemodes>();

            foreach (var masterMenuHandler in masterMenuHandlers)
            {
                foreach (var scene in CustomAddressableRegistration.CustomMaps)
                {
                    var result = scene.SceneInfo;

                    if ((!result && !masterMenuHandler.CurrentGamemode.HasFlag(GameModeEnum.Melee)) || result == null ||
                        result.allowedGamemodes == null ||
                        !result.allowedGamemodes.Get().HasFlag(masterMenuHandler.CurrentGamemode))
                    {
                        continue;
                    }

                    __result.Add(scene.SceneName);
                }
            }
        }
    }
}
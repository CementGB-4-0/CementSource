using HarmonyLib;
using Il2CppGB.UI;

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
            var mapSelectionCode = __instance.mapList[__instance.currentMapIndex].ToLower();
            
            if (mapSelectionCode !=
                "modded")
            {
                __instance.mapList.Remove("modded"); // Remove modded option to avoid it being selected as a map in itself
                return;
            }

            __result.Clear();
            random = true;

            var gamemodesHandler = __instance.transform.parent.GetComponentInChildren<MenuHandlerGamemodes>();
            foreach (var scene in CustomAddressableRegistration.CustomMaps)
            {
                var sceneInfo = scene.SceneInfo;
                var gamemode = gamemodesHandler.CurrentGamemode;

                if (sceneInfo.allowedGamemodes?.Get().HasFlag(gamemode) != true)
                    continue;

                __result.Add(scene.SceneName);
            }

            if (__result.Count == 0)
            {
                __instance.mapList[__instance.currentMapIndex] = "random";
                __result = __instance.GetCurrentSelectedLevels(out random);
            }
        }
    }
}
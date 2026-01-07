using CementGB.Modules.CustomContent.Utilities;
using GBMDK;
using HarmonyLib;
using Il2CppGB.UI;
using Object = UnityEngine.Object;

namespace CementGB.Modules.CustomContent.Patches;

internal static class GBConfigLoaderPatch
{
    [HarmonyPatch(typeof(MenuHandlerMaps), nameof(MenuHandlerMaps.GetCurrentSelectedLevels))]
    private static class GetCurrentSelectedLevelsPatch
    {
        private static void Postfix(MenuHandlerMaps __instance,
            ref Il2CppSystem.Collections.Generic.List<string> __result)
        {
            if (__instance.mapList[__instance.currentMapIndex].ToLower() !=
                "random")
            {
                return; // map is not set to random; don't do patch
            }

            var masterMenuHandlers = Object.FindObjectsOfType<MenuHandlerGamemodes>();

            foreach (var masterMenuHandler in masterMenuHandlers)
            {
                foreach (var scene in CustomAddressableRegistration.CustomMaps)
                {
                    var resultHandle = scene.SceneInfoHandle;
                    if (resultHandle == null || !resultHandle.HandleSynchronousAddressableOperation())
                        continue;
                    var result = resultHandle.Result.Cast<CustomMapInfo>();
                    if (result.allowedGamemodes == null ||
                        !result.allowedGamemodes.Get().HasFlag(masterMenuHandler.CurrentGamemode)) continue;

                    __result.Add(scene.SceneName);
                }
            }
        }
    }
}
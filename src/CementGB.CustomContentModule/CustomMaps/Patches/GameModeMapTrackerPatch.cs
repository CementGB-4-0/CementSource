using CementGB.Utilities;
using HarmonyLib;
using Il2CppGB.Gamemodes;
using Il2CppGB.UI;
using ConsoleColor = System.ConsoleColor;

namespace CementGB.Modules.CustomContent.Patches;

internal static class GameModeMapTrackerPatch
{
    private static readonly List<GameModeMapTracker> _instancesAlreadyExecuted = [];

    private static bool SceneNameAlreadyExists(GameModeMapTracker __instance, string sceneName)
    {
        foreach (var map in __instance.AvailableMaps)
        {
            if (map.MapName == sceneName)
            {
                return true;
            }
        }

        return false;
    }

    [HarmonyPatch(typeof(MenuHandlerGamemodes), nameof(MenuHandlerGamemodes.GenerateUI))]
    private static class MenuHandlerGamemodesGenerateUIPatch
    {
        private static void Postfix(MenuHandlerGamemodes __instance)
        {
            if (CustomAddressableRegistration.CustomMaps.Count == 0) return;
            if (__instance.mapSetup.mapList.Contains("Modded")) return; // Duplicate sanity check

            __instance.mapSetup.mapList.Insert(1, "Modded");
            __instance.mapSetup.UpdateMapList(__instance.mapSetup.mapList);
            ExtendedStringLoader.Register("STAGE_MODDED", "Modded", false);
        }
    }

    [HarmonyPatch(typeof(GameModeMapTracker), nameof(GameModeMapTracker.GetMapsFor))]
    private static class GetMapsForPatch
    {
        private static ModeMapStatus LoadMapInfo(CustomMapRefHolder mapRef)
        {
            var info = mapRef.SceneInfo;
            var modeMapStatus = new ModeMapStatus(mapRef.SceneName, true)
            {
                AllowedModesLocal = info != null && info.allowedGamemodes != null
                    ? info.allowedGamemodes.Get()
                    : GameModeEnum.Melee,
                AllowedModesOnline = info != null && info.allowedGamemodes != null
                    ? info.allowedGamemodes.Get()
                    : GameModeEnum.Melee
            };

            return modeMapStatus;
        }

        private static void Prefix(GameModeMapTracker __instance)
        {
            if (!_instancesAlreadyExecuted.Contains(__instance))
            {
                _instancesAlreadyExecuted.Add(__instance);

                foreach (var mapRef in CustomAddressableRegistration.CustomMaps)
                {
                    if (!mapRef.IsValid ||
                        SceneNameAlreadyExists(__instance, mapRef.SceneName))
                        continue;

                    ExtendedStringLoader.Register($"STAGE_{mapRef.SceneName.ToUpper()}", mapRef.SceneName);

                    __instance.AvailableMaps.Add(LoadMapInfo(mapRef));
                    CustomContentModule.Logger?.VerboseLog(
                        ConsoleColor.DarkGreen,
                        $"Registered allowed gamemodes and UI selector for custom scene \"{mapRef.SceneName}\"!");
                }
            }
        }
    }
}
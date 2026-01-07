using CementGB.Modules.CustomContent.Utilities;
using CementGB.Utilities;
using GBMDK;
using HarmonyLib;
using Il2CppGB.Gamemodes;
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

    [HarmonyPatch(typeof(GameModeMapTracker), nameof(GameModeMapTracker.GetMapsFor))]
    private static class GetMapsForPatch
    {
        private static ModeMapStatus LoadMapInfo(CustomMapRefHolder mapRef)
        {
            var infoHandle = mapRef.SceneInfoHandle;
            CustomMapInfo? info = null;
            if (infoHandle != null && infoHandle.HandleSynchronousAddressableOperation())
                info = infoHandle.Result.Cast<CustomMapInfo>();
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

                    __instance.AvailableMaps.Add(LoadMapInfo(mapRef));
                    CustomContentModule.Logger?.VerboseLog(
                        ConsoleColor.DarkGreen,
                        $"Registered allowed gamemodes and UI selector for custom scene \"{mapRef.SceneName}\"!");
                }
            }
        }
    }
}
using System.Collections.Generic;
using CementGB.Mod.CustomContent;
using CementGB.Mod.Utilities;
using GBMDK;
using HarmonyLib;
using Il2CppGB.Gamemodes;
using Il2CppSystem;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using ConsoleColor = System.ConsoleColor;

namespace CementGB.Mod.Patches;

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
        private static void LoadMapInfo(GameModeMapTracker __instance, string mapKey)
        {
            var infoHandle = Addressables.LoadAsset<Object>($"{mapKey}-Info").Acquire();
            infoHandle.WaitForCompletion();

            ModeMapStatus modeMapStatus;
            if (infoHandle.Status != AsyncOperationStatus.Succeeded)
            {
                LoggingUtilities.VerboseLog(ConsoleColor.DarkYellow,
                    $"Unable to load \"{mapKey}-Info\" for gamemode selection : OperationException ${infoHandle.OperationException.ToString()}");

                modeMapStatus = new ModeMapStatus(mapKey, true)
                {
                    AllowedModesLocal = GameModeEnum.Melee,
                    AllowedModesOnline = GameModeEnum.Melee
                };
            }
            else
            {
                var info = infoHandle.Result.Cast<CustomMapInfo>();
                modeMapStatus = new ModeMapStatus(mapKey, true)
                {
                    AllowedModesLocal = info != null ? info.allowedGamemodes.Get() : GameModeEnum.Melee,
                    AllowedModesOnline = GameModeEnum.Melee
                };
            }

            __instance.AvailableMaps.Add(modeMapStatus);
            LoggingUtilities.VerboseLog(ConsoleColor.DarkGreen,
                $"Registered allowed gamemodes and UI selector for custom scene \"{mapKey}\"!");
        }

        private static void Prefix(GameModeMapTracker __instance)
        {
            if (!_instancesAlreadyExecuted.Contains(__instance))
            {
                _instancesAlreadyExecuted.Add(__instance);

                foreach (var mapRef in CustomAddressableRegistration.CustomMaps)
                {
                    if (SceneNameAlreadyExists(__instance, mapRef.SceneData._sceneRef.RuntimeKey.ToString()))
                        continue;

                    ExtendedStringLoader.Register($"STAGE_{mapRef.SceneName.ToUpper()}", mapRef.SceneName);

                    LoadMapInfo(__instance, mapRef.SceneName);
                }
            }
        }
    }
}
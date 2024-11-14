using CementGB.Mod.Utilities;
using HarmonyLib;
using Il2CppGB.Gamemodes;
using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace CementGB.Mod.Patches;

internal static class GameModeMapTrackerPatch
{
    private static readonly List<GameModeMapTracker> _instancesAlreadyExecuted = new();

    private static bool SceneNameAlreadyExists(GameModeMapTracker __instance, string sceneName)
    {
        foreach (var map in __instance.AvailableMaps)
        {
            if (map.MapName == sceneName) return true;
        }
        return false;
    }

    [HarmonyPatch(typeof(GameModeMapTracker), nameof(GameModeMapTracker.GetMapsFor))]
    private static class GetMapsForPatch
    {
        private static void Prefix(GameModeMapTracker __instance)
        {
            if (!_instancesAlreadyExecuted.Contains(__instance))
            {
                try
                {
                    _instancesAlreadyExecuted.Add(__instance);
                    var mapLocations = AddressableUtilities.GetAllModdedResourceLocationsOfType<SceneInstance>();

                    foreach (var mapLocation in mapLocations)
                    {
                        if (SceneNameAlreadyExists(__instance, mapLocation.PrimaryKey)) continue;
                        ExtendedStringLoader.Register($"STAGE_{mapLocation.PrimaryKey.ToUpper()}", mapLocation.PrimaryKey);

                        ModeMapStatus newMapStatus = new(mapLocation.PrimaryKey, true)
                        {
                            AllowedModesLocal = GameModeEnum.Melee,
                            AllowedModesOnline = GameModeEnum.Melee // TODO: support additional gamemodes
                        };

                        __instance.AvailableMaps.Add(newMapStatus);
                    }
                }
                catch (Exception e)
                {
                    LoggingUtilities.Logger.Error(e);
                }
            }
        }
    }
}
using CementGB.Mod.Utilities;
using GBMDK;
using HarmonyLib;
using Il2CppGB.Gamemodes;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace CementGB.Mod.Patches;

internal static class GameModeMapTrackerPatch
{
    private static readonly List<GameModeMapTracker> _instancesAlreadyExecuted = [];

    private static bool SceneNameAlreadyExists(GameModeMapTracker __instance, string sceneName)
    {
        foreach (var map in __instance.AvailableMaps)
            if (map.MapName == sceneName) return true;

        return false;
    }

    [HarmonyPatch(typeof(GameModeMapTracker), nameof(GameModeMapTracker.GetMapsFor))]
    private static class GetMapsForPatch
    {
        private static void LoadMapInfo(GameModeMapTracker __instance, IResourceLocation mapLocation)
        {
            var infoHandle = Addressables.LoadAsset<Il2CppSystem.Object>($"{mapLocation.PrimaryKey}-Info").Acquire();
            infoHandle.WaitForCompletion();

            ModeMapStatus modeMapStatus;
            if (infoHandle.Status != UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                LoggingUtilities.VerboseLog(System.ConsoleColor.DarkYellow, $"Unable to load \"{mapLocation.PrimaryKey}-Info\" for gamemode selection : OperationException ${infoHandle.OperationException.ToString()}");

                modeMapStatus = new(mapLocation.PrimaryKey, true)
                {
                    AllowedModesLocal = GameModeEnum.Melee,
                    AllowedModesOnline = GameModeEnum.Melee
                };
            }
            else
            {
                var info = infoHandle.Result.Cast<CustomMapInfo>();
                modeMapStatus = new(mapLocation.PrimaryKey, true)
                {
                    AllowedModesLocal = info != null ? info.allowedGamemodes.Get() : GameModeEnum.Melee,
                    AllowedModesOnline = GameModeEnum.Melee
                };
            }

            __instance.AvailableMaps.Add(modeMapStatus);
            LoggingUtilities.VerboseLog(System.ConsoleColor.DarkGreen, $"Registered allowed gamemodes and UI selector for custom scene \"{mapLocation.PrimaryKey}\"!");
        }

        private static void Prefix(GameModeMapTracker __instance)
        {
            if (!_instancesAlreadyExecuted.Contains(__instance))
            {
                _instancesAlreadyExecuted.Add(__instance);
                var mapLocations = AssetUtilities.GetAllModdedResourceLocationsOfType<SceneInstance>();

                foreach (var mapLocation in mapLocations)
                {
                    if (SceneNameAlreadyExists(__instance, mapLocation.PrimaryKey)) continue;
                    ExtendedStringLoader.Register($"STAGE_{mapLocation.PrimaryKey.ToUpper()}", mapLocation.PrimaryKey);

                    LoadMapInfo(__instance, mapLocation);
                }
            }
        }
    }
}
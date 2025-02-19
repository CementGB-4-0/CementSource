using CementGB.Mod.Utilities;
using GBMDK;
using HarmonyLib;
using Il2CppGB.Gamemodes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AddressableAssets;
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

                    var mapInfoArray = AssetUtilities.GetAllModdedResourceLocationsOfType<CustomMapInfo>()
                         .Where(new System.Func<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation, bool>((loc) => { return loc.PrimaryKey == $"{mapLocation.PrimaryKey}-Info"; }))
                         .ToArray();

                    CustomMapInfo info = null;
                    if (mapInfoArray.Length > 0)
                    {
                        var mapInfoLoc = mapInfoArray.First();

                        var infoHandle = Addressables.LoadAsset<CustomMapInfo>(mapInfoLoc).Acquire();
                        infoHandle.WaitForCompletion();

                        if (infoHandle.Status != UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                        {
                            LoggingUtilities.VerboseLog(System.ConsoleColor.DarkRed, $"Failed to load CustomMapInfo from key \"{mapLocation.PrimaryKey}-Info\" : OperationException \"{infoHandle.OperationException.ToString()}\"");
                        }

                        if (infoHandle.Result == null)
                        {
                            LoggingUtilities.VerboseLog(System.ConsoleColor.DarkRed, $"Failed to load CustomMapInfo from key \"{mapLocation.PrimaryKey}-Info\" : Result returned null");
                        }

                        info = infoHandle.Result;
                    }

                    ModeMapStatus newMapStatus = new(mapLocation.PrimaryKey, true)
                    {
                        AllowedModesLocal = info != null ? info.allowedGamemodes.Get() : GameModeEnum.Melee,
                        AllowedModesOnline = GameModeEnum.Melee
                    };

                    __instance.AvailableMaps.Add(newMapStatus);
                }
            }
        }
    }
}
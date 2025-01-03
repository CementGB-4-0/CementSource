using CementGB.Mod.Utilities;
using HarmonyLib;
using Il2CppGB.Data.Loading;
using Il2CppGB.UI;
using Il2CppSystem.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace CementGB.Mod.Patches;

internal static class GBConfigLoaderPatch
{
    [HarmonyPatch(typeof(MenuHandlerMaps), nameof(MenuHandlerMaps.GetCurrentSelectedLevels))]
    private static class LoadRotationConfigPatch
    {
        private static void Postfix(MenuHandlerMaps __instance, ref List<string> __result)
        {
            var masterMenuHandlers = UnityEngine.Object.FindObjectsOfType<MenuHandlerGamemodes>();

            foreach (var masterMenuHandler in masterMenuHandlers)
            {
                if (__instance.mapList[__instance.currentMapIndex].ToLower() != "random" || masterMenuHandler.type != MenuHandlerGamemodes.MenuType.Local)
                    continue; // Either map is not set to random or game is not local; don't do patch

                foreach (var scene in AssetUtilities.GetAllModdedResourceLocationsOfType<SceneInstance>())
                {
                    var handle = Addressables.LoadAsset<SceneData>(scene.PrimaryKey + "-Data").Acquire();
                    handle.WaitForCompletion();

                    if (handle.Status != UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                    {
                        Mod.Logger.Error($"Handle loading custom SceneData with key \"{scene.PrimaryKey}-Data\" for Random map rotation injection did not succeed. OperationException: {handle.OperationException}");
                        handle.Release();
                        continue;
                    }

                    if (handle.Result == null)
                    {
                        Mod.Logger.Error($"Handle loading custom SceneData with key \"{scene.PrimaryKey}-Data\" for Random map rotation injection did not return a result. This typically indicates an incorrect Addressables configuration in the modded project you're exporting from. OperationException: {handle.OperationException}");
                        handle.Release();
                        continue;
                    }

                    if (handle.Result._wavesData == null && masterMenuHandler.CurrentGamemode == Il2CppGB.Gamemodes.GameModeEnum.Waves)
                    {
                        // Custom scene data has no waves data attached; this is not a waves map
                        handle.Release();
                        continue;
                    }

                    handle.Release();
                    __result.Add(scene.PrimaryKey);
                }
            }
        }
    }
}
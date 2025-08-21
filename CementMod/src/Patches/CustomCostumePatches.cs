using CementGB.Mod.CustomContent;
using CementGB.Mod.Utilities;
using Il2CppCostumes;
using Il2CppSystem.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace CementGB.Mod.Patches;

internal static class CustomCostumePatches
{
    [HarmonyLib.HarmonyPatch(typeof(CostumeDatabase._Load_d__12), nameof(CostumeDatabase._Load_d__12.MoveNext))]
    private static class CostumeDatabase_Load_Patch
    {
        private static bool ranAlready = false;

        private static bool Prefix(CostumeDatabase._Load_d__12 __instance)
        {
            if (ranAlready)
            {
                return false;
            }
            ranAlready = true;

            var resList = new List<CostumeObject>();
            var moddedCostumeLocs = new List<IResourceLocation>();

            foreach (var costumeObjectLoc in CustomAddressableRegistration.GetAllModdedResourceLocationsOfType<CostumeObject>())
            {
                moddedCostumeLocs.Add(costumeObjectLoc);
            }

            var loadHandleVanilla = Addressables.LoadAssetsAsync<CostumeObject>("Costume", null);
            var loadHandleModded = Addressables.LoadAssetsAsync<CostumeObject>(moddedCostumeLocs.Cast<IList<IResourceLocation>>(), null);

            if (!loadHandleVanilla.HandleSynchronousAddressableOperation() || !loadHandleModded.HandleSynchronousAddressableOperation())
                return true;

            resList.AddRange(loadHandleVanilla.Result.Cast<IEnumerable<CostumeObject>>());
            resList.AddRange(loadHandleModded.Result.Cast<IEnumerable<CostumeObject>>());
            __instance.__4__this.ParseCostumeOperationResult(resList.Cast<IList<CostumeObject>>());
            __instance.__4__this.IsLoaded = true;
            return false;
        }
    }
}
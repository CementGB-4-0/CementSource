using CementGB.Mod.CustomContent;
using CementGB.Mod.Utilities;
using HarmonyLib;
using Il2Cpp;
using Il2CppCostumes;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using Il2CppSystem.Net;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace CementGB.Mod.Patches;

internal static class CustomCostumePatches
{
    [HarmonyPatch(typeof(ActorCostume._LoadCostumeItems_d__52), nameof(ActorCostume._LoadCostumeItems_d__52.MoveNext))]
    [HarmonyPrefix]
    private static void Prefix(ActorCostume._LoadCostumeItems_d__52 __instance)
    {
        if (__instance.__1__state != 1)
            return;

        foreach (var item in __instance._ops_5__2)
        {
            foreach (var obj in item.Cast<List<GameObject>>())
            {
                obj.MakePersistent();
            }
        }
    }

    [HarmonyPatch(typeof(RigCombine), nameof(RigCombine.InstGameOjbects))]
    [HarmonyPostfix]
    private static void Postfix(RigCombine __instance, ref GameObject[] __result)
    {
        foreach (var item in __result)
        {
            LoggingUtilities.VerboseLog(item.name);
            item.MakePersistent();
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(CostumeDatabase._Load_d__12), nameof(CostumeDatabase._Load_d__12.MoveNext))]
    private static class CostumeDatabase_Load_Patch
    {
        private static bool ranAlready = false;

        private static bool Prefix(CostumeDatabase._Load_d__12 __instance, ref bool __result)
        {
            if (__instance.__1__state != 1 || ranAlready)
                return true;
            ranAlready = true;

            var resList = new List<CostumeObject>();
            var moddedCostumeLocs = new List<IResourceLocation>();

            foreach (var costumeObjectLoc in CustomAddressableRegistration.GetAllModdedResourceLocationsOfType<CostumeObject>())
            {
                moddedCostumeLocs.Add(costumeObjectLoc);
            }

            var loadHandleModded = Addressables.LoadAssetsAsync<CostumeObject>(moddedCostumeLocs.Cast<IList<IResourceLocation>>(), null);
            var loadHandleVanilla = Addressables.LoadAssetsAsync<CostumeObject>("Costume", null);

            if (!loadHandleVanilla.HandleSynchronousAddressableOperation() || !loadHandleModded.HandleSynchronousAddressableOperation())
                return true;

            foreach (var costumeObject in loadHandleModded.Result.Cast<List<CostumeObject>>())
            {
                costumeObject._uid = __instance.__4__this.NewUID();
            }

            resList.AddRange(loadHandleVanilla.Result.Cast<IEnumerable<CostumeObject>>());
            resList.AddRange(loadHandleModded.Result.Cast<IEnumerable<CostumeObject>>());
            __instance.__4__this.ParseCostumeOperationResult(resList.Cast<IList<CostumeObject>>());
            __instance.__4__this.IsLoaded = true;

            __result = false;
            return false;
        }
    }
}
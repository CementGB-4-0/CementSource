using CementGB.Modules.CustomContent.Utilities;
using CementGB.Utilities;
using HarmonyLib;
using Il2CppCostumes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace CementGB.Modules.CustomContent.Patches;

internal static class CustomCostumePatches
{
    [HarmonyPatch(typeof(RigCombine), nameof(RigCombine.InstGameOjbects))]
    [HarmonyPostfix]
    private static void Postfix(RigCombine __instance, ref Il2CppReferenceArray<GameObject> __result)
    {
        foreach (var item in __result)
        {
            CustomContentModule.Logger?.VerboseLog(item.name);
            item.MakePersistent();
        }
    }
}

[HarmonyPatch(typeof(CostumeDatabase._Load_d__12), nameof(CostumeDatabase._Load_d__12.MoveNext))]
internal static class CostumeDatabase_Load_Patch
{
    private static bool ranAlready;

    private static bool Prefix(bool __result, CostumeDatabase._Load_d__12 __instance)
    {
        if (__instance.__1__state != 1 || ranAlready)
            return true;
        ranAlready = true;

        var resList = new Il2CppSystem.Collections.Generic.List<CostumeObject>();
        var moddedCostumeLocs = new Il2CppSystem.Collections.Generic.List<IResourceLocation>();

        foreach (var costumeObjectLoc in CustomAddressableRegistration
                     .GetAllModdedResourceLocationsOfType<CostumeObject>())
        {
            moddedCostumeLocs.Add(costumeObjectLoc);
        }

        var loadHandleVanilla = Addressables.LoadAssetsAsync<CostumeObject>("Costume", null, true);
        var loadHandleModded =
            Addressables.LoadAssetsAsync<CostumeObject>(
                moddedCostumeLocs.Cast<Il2CppSystem.Collections.Generic.IList<IResourceLocation>>(), null);

        if (!loadHandleVanilla.HandleSynchronousAddressableOperation())
            return true;

        if (!loadHandleModded.HandleSynchronousAddressableOperation())
            return true;

        foreach (var vanillaCO in loadHandleVanilla.Result.Cast<Il2CppSystem.Collections.Generic.List<CostumeObject>>())
        {
            resList.Add(vanillaCO);
        }

        foreach (var costumeObject in loadHandleModded.Result
                     .Cast<Il2CppSystem.Collections.Generic.List<CostumeObject>>())
        {
            costumeObject._uid = __instance.__4__this.NewUID();
            costumeObject.MakePersistent();
            resList.Add(costumeObject);
        }

        __instance.__4__this.ParseCostumeOperationResult(resList
            .Cast<Il2CppSystem.Collections.Generic.IList<CostumeObject>>());
        __instance.__4__this.IsLoaded = true;

        __result = false;
        return false;
    }
}
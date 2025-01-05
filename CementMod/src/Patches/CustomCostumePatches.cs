using System;
using CementGB.Mod.Utilities;
using HarmonyLib;
using Il2CppCostumes;
using UnityEngine.AddressableAssets;

namespace CementGB.Mod.Patches;

internal static class CustomCostumePatches
{
    private static ushort NewUID(this CostumeDatabase __instance)
    {
        ushort num;
        try
        {
            num = (ushort)(__instance.GetAllCostumeObjects().Count + 1);
        }
        catch (OverflowException)
        {
            Mod.Logger.Error("There are too many CostumeObject UIDs!");
            num = 0;
        }
        return num;
    }

    [HarmonyPatch(typeof(CostumeDatabase), nameof(CostumeDatabase.Load))]
    private static class CostumeDatabasePatch
    {
        private static void Postfix(CostumeDatabase __instance)
        {
            foreach (var location in AssetUtilities.GetAllModdedResourceLocationsOfType<CostumeObject>())
            {
                var handle = Addressables.LoadAsset<CostumeObject>(location).Acquire();
                handle.WaitForCompletion();

                if (handle.Status != UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    Mod.Logger.Error($"Failed to load custom Addressable CostumeObject : Key \"{location.PrimaryKey}\" : OperationException {handle.OperationException?.ToString() ?? "null"}");
                    handle.Release();
                    continue;
                }

                if (handle.Result == null)
                {
                    Mod.Logger.Error($"Handle loading Custom CostumeObject completed with no result : Key \"{location.PrimaryKey}\" : OperationException {handle.OperationException?.ToString() ?? "null"}");
                    handle.Release();
                    continue;
                }

                var res = handle.Result;
                res._uid = __instance.NewUID();
                __instance.CostumeObjects.Add(res);

                handle.Release();
            }
        }
    }
}
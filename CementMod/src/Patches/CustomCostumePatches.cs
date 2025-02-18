using System;
using System.Diagnostics;
using CementGB.Mod.Utilities;
using HarmonyLib;
using Il2CppCostumes;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CementGB.Mod.Patches;

internal static class CustomCostumePatches
{
    private static ushort NewUID(this CostumeDatabase __instance)
    {
        ushort num;
        try
        {
            num = (ushort)(__instance.searchSpeeder.GetKey(__instance.searchSpeeder.Count - 1) + 1);
        }
        catch (OverflowException)
        {
            Mod.Logger.Error("There are too many CostumeObject UIDs!");
            num = 0;
        }
        return num;
    }

    [HarmonyPatch(typeof(CostumeDatabase._Load_d__12), nameof(CostumeDatabase._Load_d__12.MoveNext))]
    private static class CostumeDatabasePatch
    {
        private static void Postfix(bool __result, CostumeDatabase._Load_d__12 __instance)
        {
            if (__result) return;

            Mod.Logger.Msg("Injecting custom Addressable CostumeObjects into database. . .");
            var timeTakenStopwatch = new Stopwatch();
            timeTakenStopwatch.Start();

            foreach (var location in AssetUtilities.GetAllModdedResourceLocationsOfType<CostumeObject>())
            {
                var handle = Addressables.LoadAsset<CostumeObject>(location).Acquire();
                handle.WaitForCompletion();

                if (handle.Status != AsyncOperationStatus.Succeeded)
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
                res._uid = __instance.__4__this.NewUID();
                __instance.__4__this.CostumeObjects.Add(res);
                __instance.__4__this.searchSpeeder.Add(res._uid, res);

                handle.Release();
                Mod.Logger.Msg(ConsoleColor.DarkGreen, $"New custom CostumeObject registered : Key \"{location.PrimaryKey}\"");
            }

            timeTakenStopwatch.Stop();
            Mod.Logger.Msg(ConsoleColor.Green, $"Custom CostumeObject injection completed! Total time taken: {timeTakenStopwatch.Elapsed}");
        }
    }
}
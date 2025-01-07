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
        private static void Postfix(ref Il2CppSystem.Collections.IEnumerator __result)
        {
            if (__result.Current != null) return;

            Mod.Logger.Msg("Injecting custom Addressable CostumeObjects into database. . .");
            var timeTakenStopwatch = new Stopwatch();
            var totalTimeTaken = new TimeSpan();
            timeTakenStopwatch.Start();

            foreach (var location in AssetUtilities.GetAllModdedResourceLocationsOfType<CostumeObject>())
            {
                var handle = Addressables.LoadAsset<CostumeObject>(location).Acquire();
                handle.WaitForCompletion();

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Mod.Logger.Error($"Failed to load custom Addressable CostumeObject : Key \"{location.PrimaryKey}\" : OperationException {handle.OperationException?.ToString() ?? "null"}");
                    handle.Release();
                    timeTakenStopwatch.Restart();
                    continue;
                }

                if (handle.Result == null)
                {
                    Mod.Logger.Error($"Handle loading Custom CostumeObject completed with no result : Key \"{location.PrimaryKey}\" : OperationException {handle.OperationException?.ToString() ?? "null"}");
                    handle.Release();
                    timeTakenStopwatch.Restart();
                    continue;
                }

                var res = handle.Result;
                res._uid = CostumeDatabase.Instance.NewUID();
                CostumeDatabase.Instance.CostumeObjects.Add(res);

                handle.Release();
                totalTimeTaken.Add(timeTakenStopwatch.Elapsed);
                Mod.Logger.Msg(ConsoleColor.DarkGreen, $"New custom CostumeObject registered : Key \"{location.PrimaryKey}\" : Time Taken {timeTakenStopwatch.Elapsed}");
                timeTakenStopwatch.Restart();
            }

            timeTakenStopwatch.Stop();
            Mod.Logger.Msg(ConsoleColor.Green, $"Custom CostumeObject injection completed! Total time taken: {totalTimeTaken}");
        }
    }
}
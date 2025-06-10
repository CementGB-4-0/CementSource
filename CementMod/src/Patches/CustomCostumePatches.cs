using System;
using System.Diagnostics;
using CementGB.Mod.CustomContent;
using HarmonyLib;
using Il2Cpp;

namespace CementGB.Mod.Patches;
internal static class CustomCostumePatches
{
    [HarmonyPatch(typeof(CostumesAssetBundleLoader._LoadCostumeDatabases_d__17), nameof(CostumesAssetBundleLoader._LoadCostumeDatabases_d__17.MoveNext))]
    private static class CostumeDatabasePatch
    {
        static readonly Stopwatch timeTakenStopwatch = new Stopwatch();

        private static void Postfix(bool __result, CostumesAssetBundleLoader._LoadCostumeDatabases_d__17 __instance)
        {
            if (__result) return;
            
            Mod.Logger.Msg("Injecting custom Addressable CostumeObjects into vanilla databases. . .");
            timeTakenStopwatch.Start();
            CustomAddressableRegistration.InitializeCostumeReferences();

            foreach (var costumeRef in CustomAddressableRegistration.CustomCostumes)
            {
                __instance.__4__this.CostumeDatabase.CostumeObjects.Add(costumeRef.Data);
                __instance.__4__this.CostumeDatabase.searchSpeeder[costumeRef.Data._uid] = costumeRef.Data;
            }
            
            timeTakenStopwatch.Stop();
            Mod.Logger.Msg(ConsoleColor.Green, $"Done injecting custom CostumeObjects! Took {timeTakenStopwatch.ElapsedMilliseconds}ms");
        }
    }
}
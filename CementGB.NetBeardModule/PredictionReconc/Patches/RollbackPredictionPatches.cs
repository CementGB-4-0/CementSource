using HarmonyLib;
using Il2CppCoreNet.StateSync.Syncs;
using Il2CppFemur;
using Il2CppGB.NetworkedInput;

namespace CementGB.Modules.NetBeardModule.Patches;

[HarmonyPatch]
internal static class RollbackPredictionPatches
{
    [HarmonyPatch(typeof(BaseSync), nameof(BaseSync.UpdateImpl))]
    [HarmonyPrefix]
    private static void ConfigureRigidbody(BaseSync __instance)
    {
        var sync = __instance as RigidbodySync;
        if (sync == null) return;

        sync._forceKinematicOnClient = !NetBeardModule.RollbackFlag;
        sync._ignoreKinematicChanges = NetBeardModule.RollbackFlag;
    }

    [HarmonyPatch(typeof(Actor), nameof(Actor.Update))]
    [HarmonyPostfix]
    private static void ActorUpdate(Actor __instance)
    {
        if (!NetBeardModule.RollbackFlag) return;
        //__instance.controlHandeler.ForceUpdate = NetBeardModule.RollbackFlag;
    }

    [HarmonyPatch(typeof(Actor), nameof(Actor.Setup))]
    [HarmonyPostfix]
    private static void ActorSetup(Actor __instance)
    {
        if (!NetBeardModule.RollbackFlag) return;
        __instance.gameObject.AddComponent<InputDriver>().Initialise(__instance);
    }
}
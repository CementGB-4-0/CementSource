using CementGB.Mod.Modules.NetBeard;
using HarmonyLib;
using Il2CppCoreNet.Contexts;
using Il2CppCoreNet.Objects;
using Il2CppCoreNet.Utils;
using Il2CppGB.Game;

namespace CementGB.Mod.Patches;

[HarmonyPatch(typeof(GameManagerNew), nameof(GameManagerNew.SetupGameMode))]
internal static class GameManagerNewPatches
{
    private static void Postfix(GameManagerNew __instance)
    {
        if (ServerManager.IsServer)
            __instance.ActiveGameMode.localSingleGang = false;
    }
}
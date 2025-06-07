using HarmonyLib;
using Il2CppGB.Game;

namespace CementGB.Mod.Patches;

[HarmonyPatch(typeof(GameManagerNew), nameof(GameManagerNew.CheckAndStartGame))]
internal static class AntiSlowKick
{
    internal static bool Prefix(bool bypassMMPlayerCheck, ref bool kickSlowLoaders)
    {
        kickSlowLoaders = false;
        return true;
    }
}

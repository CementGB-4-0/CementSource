using CementGB.Modules.NetBeard;
using HarmonyLib;
using Il2CppCoreNet.Contexts;
using Il2CppGB.Game;
using Il2CppGB.Networking.Objects;
using Il2CppGB.Networking.Utils;

namespace CementGB.Modules.NetBeard.Patches;

[HarmonyPatch(typeof(GameMode), nameof(GameMode.InitBeast))]
internal static class GameModeInitBeastPatch
{
    private static void Postfix(GameMode __instance)
    {
        if (!ServerManager.IsServer)
        {
            return;
        }

        if (NetMemberContext.GetPlayers<NetBeast>().Length == 1)
        {
            __instance.localSingleGang = true;
            return;
        }

        __instance.localSingleGang = false;

        var num = 0;
        foreach (var netBeast in NetMemberContext.GetPlayers<NetBeast>())
        {
            if (netBeast.GameOver || !netBeast.Alive)
            {
                continue;
            }

            if (netBeast.GangId != num)
            {
                GBNetUtils.RemoveBeastFromGang(netBeast);
            }

            netBeast.GangId = num;
            GBNetUtils.SetBeastsGang(netBeast);
            num++;

            // Added rollover as a safety net to fix an impossible gang
            // Players will now be forced into gangs if necessary?
            num %= GBNetUtils.Model.GetCollection<NetGang>("NET_GANGS").Count;
        }
    }
}

[HarmonyPatch(typeof(GameMode_Survival), nameof(GameMode_Survival.IsGameValid))]
internal static class GameModeValidPatch
{
    private static void Postfix(ref bool __result)
    {
        if (ServerManager.IsServer)
        {
            __result = true;
        }
    }
}
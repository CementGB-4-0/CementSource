using CementGB.Mod.Modules.NetBeard;
using HarmonyLib;
using Il2CppCoreNet.Objects;
using Il2CppCoreNet.Utils;
using Il2CppGB.Game;
using Il2CppGB.Networking.Objects;
using Il2CppGB.Networking.Utils;

namespace CementGB.Mod.Patches;

[HarmonyPatch(typeof(GameMode), nameof(GameMode.InitBeast))]
internal static class GameModeInitBeastPatch
{
    private static void Postfix(GameMode __instance)
    {
        if (!ServerManager.IsServer)
            return;
        
        var collection = __instance._Model.GetCollection<NetMember>("NET_MEMBERS");
        if (collection.Count == 1)
        {
            __instance.localSingleGang = true;
            return;
        }
        int num = 0;
        foreach (NetMember netMember in collection)
        {
            if (netMember.Spectating) continue;
            foreach (NetBeast netBeast in NetUtils.GetPlayers<NetBeast>(netMember))
            {
                if (netBeast.GameOver) continue;
                GBNetUtils.RemoveBeastFromGang(netBeast);
                netBeast.GangId = num;
                GBNetUtils.SetBeastsGang(netBeast);
                num++;
            }
        }

        __instance.localSingleGang = false;
    }
}

[HarmonyPatch(typeof(GameMode_Survival), nameof(GameMode_Survival.IsGameValid))]
internal static class GameModeValidPatch
{
    private static void Postfix(ref bool __result)
    {
        if (ServerManager.IsServer)
            __result = true;
    }
}

[HarmonyPatch(typeof(GameMode_Survival), nameof(GameMode_Survival.IsRoundOver))]
internal static class GameModeOverPatch
{
    private static void Postfix(ref bool __result)
    {
        if (ServerManager.IsServer)
            __result = GameMode.GetNumRemainingGangsAlive() < 2 && GBNetUtils.GetParticipatingPlayers().Count != 1;
    }
}
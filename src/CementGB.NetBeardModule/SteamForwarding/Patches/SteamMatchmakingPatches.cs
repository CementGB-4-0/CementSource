using HarmonyLib;
using Il2CppCoatsink.UnityServices.Matchmaking;
using Il2CppGB.Platform.Lobby;
using Il2CppGB.UnityServices.Matchmaking;
using MelonLoader;
using Steamworks.ServerList;

namespace CementGB.NetBeardModule.Patches;

[HarmonyPatch]
internal static class SteamMatchmakingPatches
{
    private static Base? serverList;
    private static MatchmakingResult? result;

    [HarmonyPatch(typeof(MatchmakingClientMonobehaviour), nameof(MatchmakingClientMonobehaviour.CancelMatchmaking))]
    [HarmonyPostfix]
    private static void CancelMatchmaking()
    {
        serverList?.Dispose();
        serverList = null;
    }

    [HarmonyPatch(typeof(MatchmakingClientMonobehaviour), nameof(MatchmakingClientMonobehaviour.StartMatchmaking))]
    [HarmonyPrefix]
    private static bool StartMatchmaking(MatchmakingClientMonobehaviour __instance, MatchmakingTicketModel request)
    {
        serverList = new LocalNetwork();
        serverList.AddFilter("map", "unset");
        MelonEvents.OnUpdate.Subscribe(SuccessCheck);
        serverList.RunQueryAsync();

        __instance.currentTicket = request;
        __instance.OnMatchmakingStateChanged?.Invoke(MatchmakingState.Started);

        return false;
    }

    private static void SuccessCheck()
    {
        if (serverList == null || serverList.Responsive.Count == 0) return;
        var info = serverList.Responsive.First();
        result = new MatchmakingResult
        {
            State = MatchmakingState.Success,
            IpAddress = info.Address.ToString(),
            Port = info.ConnectionPort,
            Message = info.Name
        };
        LobbyManager.Instance.LobbyStates.MatchmakingComplete(result);
        serverList.Dispose();
        serverList = null;
        MelonEvents.OnUpdate.Unsubscribe(SuccessCheck);
    }
}
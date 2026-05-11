using HarmonyLib;
using Il2CppCoreNet.Objects;
using Il2CppCostumes;
using Il2CppFemur;
using Il2CppGB.Game;
using Il2CppGB.Networking.Objects;
using Il2CppGB.Networking.Utils.Spawn;

namespace CementGB.Modules.CustomContent.Patches;

[HarmonyPatch]
internal static class WavesFixPatches
{
    [HarmonyPatch(typeof(GameMode), nameof(GameMode.Init))]
    [HarmonyPostfix]
    private static void SetSecondaryColorPatch(GameMode_Waves __instance)
    {
        GameMode.add_OnBeastSpawned((Il2CppSystem.Action<NetBeast, Actor>)((beast, _) =>
        {
            FixColor(beast, __instance._waveInfomation.GetRandomColour());
        }));
    }

    private static void FixColor(NetBeast? beast, int colorId)
    {
        if (GameManagerNew.Instance == null ||
            GameManagerNew.Instance.ActiveGameMode.GetType() != typeof(GameMode_Waves)) return;

        if (beast == null || beast.playerType == NetPlayer.PlayerType.Player) return;

        var colorObject = CostumePool.I.PlayerColorDatabase.GetColorOjectWithID((ushort)colorId);
        var chosenColor = colorObject.Colors.First();

        beast.PrimaryColor = chosenColor;
        beast.CostumeColor = chosenColor;
    }

    [HarmonyPatch(typeof(GBSpawnPoint), nameof(GBSpawnPoint.Use))]
    [HarmonyPostfix]
    private static void SpawnPointEnablePatch(GBSpawnPoint __instance)
    {
        __instance._teamIndex = -1;
        __instance._groupIndex = -1;
        __instance.Locked = false;
    }
}
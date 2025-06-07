using CementGB.Mod.Modules.NetBeard;
using HarmonyLib;
using MelonLoader.Utils;
using Unity.Services.Multiplay;
using UnityEngine;

namespace CementGB.Mod.Patches;

[HarmonyPatch(typeof(ServerConfigReader), nameof(ServerConfigReader.LoadServerConfigInServer))]
internal static class ServerConfigPatch
{
    private static bool Prefix(ServerConfigReader __instance, ref ServerConfig __result)
    {
        if (!ServerManager.IsServer) return true;
        if (__instance.GetServerConfigHomeFilePath(out var path)) return true;
        __result = new ServerConfig(Random.Range(1, 25565), "cmt", 3999, 5999, "127.0.0.1",
            MelonEnvironment.MelonLoaderLogsDirectory);
        return false;
    }
}
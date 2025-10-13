using CementGB.Modules.NetBeard;
using HarmonyLib;
using Il2CppCoatsink.Platform;
using Il2CppCoatsink.Platform.Systems.UI;
using Il2CppCS.CorePlatform;
using Il2CppCS.CorePlatform.CSPlatform;
using Il2CppGB.Core;

namespace CementGB.Modules.NetBeard.Patches;

internal static class MultiRunPatches
{
    [HarmonyPatch(typeof(CStoCorePlatform), nameof(CStoCorePlatform.OnInitializeComplete))]
    private static class OnInitializeCompletePatch
    {
        private static bool Prefix(CStoCorePlatform __instance)
        {
            if (!ServerManager.IsServer)
            {
                return true;
            }

            Users.MaxUsers = Global.NetworkMaxPlayers;
            UI.PopUpUI = __instance._dialogUI.Cast<IUIPopUpManager>();
            BasePlatformManager._InitializedPlatformAPI = true;
            BasePlatformManager.Initialized = true;
            __instance.PassEntitlement();
            __instance._online.CheckSetup();
            __instance._network.CheckSetup();
            return false;
        }
    }
}
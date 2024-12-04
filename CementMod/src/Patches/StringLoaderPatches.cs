using CementGB.Mod.Utilities;
using Il2Cpp;
using Il2CppGB.Core.Loading;
using Il2CppGB.Data;
using Il2CppTMPro;
using UnityEngine.InputSystem;

namespace CementGB.Mod.Patches;

[HarmonyLib.HarmonyPatch(typeof(StringLoader), nameof(StringLoader.LoadString))]
internal class LoadStringPatch
{
    private static void Postfix(string key, ref string __result)
    {
        if (__result == null)
        {
            if (!ExtendedStringLoader.items.ContainsKey(key))
            {
                __result = key;
                return;
            }
            __result = ExtendedStringLoader.items[key];
            return;
        }
    }
}

[HarmonyLib.HarmonyPatch(typeof(StringLoader), nameof(StringLoader.LoadRawString))]
internal class LoadRawStringPatch
{
    private static void Postfix(string key, ref string __result)
    {
        if (__result == null)
        {
            if (!ExtendedStringLoader.items.ContainsKey(key))
            {
                return;
            }
            __result = ExtendedStringLoader.items[key];
            return;
        }
    }
}

[HarmonyLib.HarmonyPatch(typeof(StringLoader), nameof(StringLoader.TryLoadStringByPlatform))]
internal class TryLoadStringPatch
{
    private static void Postfix(ref string pulledString, string key, ref bool __result)
    {
        if (!__result)
        {
            if (!ExtendedStringLoader.items.ContainsKey(key))
            {
                return;
            }
            pulledString = ExtendedStringLoader.items[key];
            __result = true;
        }
    }
}

[HarmonyLib.HarmonyPatch(typeof(LoadScreenDisplayHandler), nameof(LoadScreenDisplayHandler.SetSubTitle))]
internal static class SelectSubTitlePath
{
    private static void Postfix(LoadScreenDisplayHandler __instance, string name)
    {
        var tmpInstance = __instance._subTitle.GetComponent<TextReplacer>();
        if (tmpInstance == null /* || !AssetUtilities.IsModdedKey(name) */) return;

        LoggingUtilities.VerboseLog(System.ConsoleColor.DarkGreen, "SetSubTitle Postfix called!");

        __instance._subTitle.enabled = false;
        tmpInstance.enabled = false;

        tmpInstance.text = name;
    }
}

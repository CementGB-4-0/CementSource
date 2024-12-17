using CementGB.Mod.Utilities;
using Il2CppGB.Core.Loading;
using Il2CppGB.Data;
using Il2CppTMPro;
using UnityEngine.Localization.Components;

namespace CementGB.Mod.Patches;

[HarmonyLib.HarmonyPatch(typeof(StringLoader), nameof(StringLoader.LoadString))]
internal class LoadStringPatch
{
    private static void Postfix(string key, ref string __result)
    {
        if (__result == null || __result.StartsWith("No translation"))
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
        if (__result == null || __result.StartsWith("No translation"))
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

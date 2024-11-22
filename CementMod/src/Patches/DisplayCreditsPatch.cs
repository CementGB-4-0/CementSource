using CementGB.Mod.Utilities;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace CementGB.Mod.Patches;

internal static class DisplayCreditsPatch
{
    [HarmonyPatch(typeof(DisplayCredits), nameof(DisplayCredits.ApplyText))]
    private static class ApplyText
    {
        static TextAsset textAsset = null;
        private static void Prefix(DisplayCredits __instance)
        {
            if (textAsset == null)
            {
                textAsset = new TextAsset($"{EmbeddedUtilities.ReadEmbeddedText(Melon<Mod>.Instance.MelonAssembly.Assembly, "CementGB.Mod.Assets.CreditsText.txt")}\n\n{__instance.textFile.text}");
                __instance.textFile = textAsset;
            }
        }
    }
}

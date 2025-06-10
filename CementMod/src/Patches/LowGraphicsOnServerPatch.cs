using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CementGB.Mod.Modules.NetBeard;
using HarmonyLib;
using Il2Cpp;

namespace CementGB.Mod.Patches;

[HarmonyPatch(typeof(GraphicsManager), nameof(GraphicsManager.LoadSettings))]
internal class LowGraphicsOnServerPatch
{
    public static void Postfix(GraphicsManager __instance)
    {
        if (!(ServerManager.IsServer && ServerManager.LowGraphicsMode)) return;

        Mod.Logger.Msg(ConsoleColor.Magenta, "Server asked for low graphics. Applying. . .");

        __instance.settings.Graphics = new()
        {
            AmbientOcclusion = false,
            AnisotropicFiltering = false,
            ChromaticAberration = false,
            PostAntialiasing = UnityEditor.PostProcessing.GraphicsSettings.URPAntialiasingSetting.Off,
            Bloom = false,
            DepthOfField = false,
            FramerateCap = 60,
            Grain = false,
            ScreenSpaceReflection = UnityEditor.PostProcessing.GraphicsSettings.ScreenSpaceReflectionSetting.Off,
            Shadows = UnityEditor.PostProcessing.GraphicsSettings.ShadowSetting.Off,
            TextureQuality = UnityEditor.PostProcessing.GraphicsSettings.TextureQualitySetting.Low,
            Vignette = false,
            VSync = false
        };
    }
}

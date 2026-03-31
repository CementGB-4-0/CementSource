using HarmonyLib;
using Il2Cpp;
using UnityEditor.PostProcessing;
using UnityEngine;

namespace CementGB.Modules.NetBeardModule.Patches;

[HarmonyPatch(typeof(GraphicsManager), nameof(GraphicsManager.LoadSettings))]
internal class LowGraphicsOnServerPatch
{
    public static void Postfix(GraphicsManager __instance)
    {
        if (!NetBeardModule.LowGraphicsMode)
        {
            return;
        }

        NetBeardModule.Logger?.Msg("Asked for low graphics. Applying. . .");

        // Unity doesn't play nice with 'new' constructors; use ScriptableObject.CreateInstance instead.
        var newGraphicsSettings = ScriptableObject.CreateInstance<GraphicsSettings>();
        newGraphicsSettings.AmbientOcclusion = false;
        newGraphicsSettings.AnisotropicFiltering = false;
        newGraphicsSettings.ChromaticAberration = false;
        newGraphicsSettings.PostAntialiasing = GraphicsSettings.URPAntialiasingSetting.Off;
        newGraphicsSettings.Bloom = false;
        newGraphicsSettings.DepthOfField = false;
        newGraphicsSettings.FramerateCap = 60;
        newGraphicsSettings.Grain = false;
        newGraphicsSettings.ScreenSpaceReflection = GraphicsSettings.ScreenSpaceReflectionSetting.Off;
        newGraphicsSettings.Shadows = GraphicsSettings.ShadowSetting.Off;
        newGraphicsSettings.TextureQuality = GraphicsSettings.TextureQualitySetting.Low;
        newGraphicsSettings.Vignette = false;
        newGraphicsSettings.VSync = false;

        __instance.settings.Graphics = newGraphicsSettings;
    }
}
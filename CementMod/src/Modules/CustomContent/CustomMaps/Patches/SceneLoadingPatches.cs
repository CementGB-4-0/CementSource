using HarmonyLib;
using Il2CppGB.Core.Loading;
using Il2CppGB.Data.Loading;
using Il2CppTMPro;
using UnityEngine.AddressableAssets;
using ConsoleColor = System.ConsoleColor;
using Resources = Il2CppGB.Core.Resources;

namespace CementGB.Mod.Modules.CustomContent.CustomMaps.Patches;

[HarmonyPatch(typeof(SceneLoader), nameof(SceneLoader.OnSceneListComplete))]
internal static class OnSceneListCompletePatch
{
    private static void Postfix(SceneLoader __instance)
    {
        var sceneList = __instance._sceneList.TryCast<AddressableDataCache>();

        if (!sceneList)
            return;

        foreach (var mapRef in CustomAddressableRegistration.CustomMaps)
        {
            if (mapRef.SceneData.AudioConfig)
                mapRef.SceneData.AudioConfig.audioMixer = MixerFinder.MainMixer;

            var sceneDataRef = new AssetReference(mapRef.SceneData.name);

            Resources._assetList.Add(
                new Resources.LoadLoadedItem(sceneDataRef)
                {
                    Key = mapRef.SceneData.name
                });

            sceneList._assets.Add(new AddressableDataCache.AssetData
            {
                Asset = sceneDataRef,
                Key = mapRef.SceneName
            });

            Mod.Logger.Msg(ConsoleColor.DarkGreen,
                $"New custom stage registered in SceneLoader : Key: {mapRef.SceneName}");
        }
    }
}

[HarmonyPatch(typeof(LoadScreenDisplayHandler), nameof(LoadScreenDisplayHandler.SetSubTitle))]
internal static class SetSubTitlePatch
{
    private static bool Prefix(LoadScreenDisplayHandler __instance, ref string name)
    {
        if (!CustomAddressableRegistration.IsModdedKey(name)) return true;
        __instance._subTitle.GetComponent<TextMeshProUGUI>().text = name;

        return false;
    }
}
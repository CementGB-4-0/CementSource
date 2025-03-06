using CementGB.Mod.Utilities;
using HarmonyLib;
using Il2Cpp;
using Il2CppSystem.IO;
using MelonLoader.Utils;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.ResourceManagement.AsyncOperations;
using Resources = Il2CppGB.Core.Resources;

namespace CementGB.Mod.Patches;

[HarmonyPatch]
internal static class CustomAddressablesPatches
{
    private const string ModsDirectoryPropertyName = "MelonLoader.Utils.MelonEnvironment.ModsDirectory";

    // Game has failsafes in order to prevent loading invalid assets, bypass them
    [HarmonyPatch(typeof(AssetReference), "RuntimeKeyIsValid")]
    [HarmonyPrefix]
    private static bool LabelModdedKeysAsValid(AssetReference __instance, ref bool __result)
    {
        if (!AssetUtilities.IsModdedKey(__instance.RuntimeKey.ToString()))
        {
            return true;
        }

        __result = true;
        return false;
    }

    [HarmonyPatch(typeof(Resources.LoadLoadedItem), nameof(Resources.LoadLoadedItem.Load))]
    internal static class LoadLoadedItemPatch
    {
        private static bool Prefix(Resources.LoadLoadedItem __instance, ref AsyncOperationHandle __result)
        {
            if (!AssetUtilities.IsModdedKey(__instance.Key))
            {
                return true;
            }

            __instance._finishedLoading = AsyncOperationStatus.None;
            __instance._loadHandle = Addressables.LoadAssetAsync<ScriptableObject>(__instance.Key);

            __result = __instance._loadHandle;
            return false;
        }
    }
  
    [HarmonyPatch(typeof(AddressablesRuntimeProperties), nameof(AddressablesRuntimeProperties.EvaluateProperty))]
    internal static class RuntimePropertiesPatch
    {
        private static bool Prefix(AddressablesImpl addressables, ObjectInitializationData providerData, string providerSuffix)
        {
            if (MelonEnvironment.ModsDirectory == Path.Combine(Application.dataPath, "..", "Mods") || !providerData.Id.Contains(Path.Combine(Application.dataPath, "..", "Mods")))
            {
                return true;
            }
            
            var indexOfExistingProvider = -1;
            var newProviderId = string.IsNullOrEmpty(providerSuffix) ? providerData.Id.Replace($"{Application.dataPath}/../Mods", MelonEnvironment.ModsDirectory) : (providerData.Id.Replace($"{Application.dataPath}/../Mods", MelonEnvironment.ModsDirectory) + providerSuffix);
            for (int i = 0; i < addressables.ResourceManager.ResourceProviders.Cast<ListWithEvents<IResourceProvider>>().Count; i++)
            {
                var rp = addressables.ResourceManager.ResourceProviders[i];
                if (rp.ProviderId != newProviderId)
                {
                    continue;
                }

                indexOfExistingProvider = i;
                break;
            }

            //if not re-initializing, just use the old provider
            if (indexOfExistingProvider >= 0 && string.IsNullOrEmpty(providerSuffix))
                return false;

            var provider = providerData.CreateInstance<IResourceProvider>(newProviderId);
            if (provider != null)
            {
                if (indexOfExistingProvider < 0 || !string.IsNullOrEmpty(providerSuffix))
                {
                    Addressables.LogFormat("Addressables - added provider {0} with id {1}.", provider.Cast<Il2CppSystem.Object>(), provider.ProviderId);
                    addressables.ResourceManager.ResourceProviders.Cast<ListWithEvents<IResourceProvider>>().Add(provider);
                }
                else
                {
                    Addressables.LogFormat("Addressables - replacing provider {0} at index {1}.", provider.Cast<Il2CppSystem.Object>(), indexOfExistingProvider);
                    addressables.ResourceManager.ResourceProviders[indexOfExistingProvider] = provider;
                }
            }
            else
            {
                Addressables.LogWarningFormat("Addressables - Unable to load resource provider from {0}.", providerData);
            }

            return false;
        }
    }
}
using System.Collections;
using System.Diagnostics;
using CementGB.Modules.CustomContent.Utilities;
using CementGB.Utilities;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Linq;
using MelonLoader;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CementGB.Modules.CustomContent;

public static class AddressableShaderCache
{
    private static readonly Dictionary<string, Shader> CachedShaders = [];

    private static readonly string[] BlacklistedShaderNames =
    [
        "Universal Render Pipeline/Lit",
        "Universal Render Pipeline/Unlit"
    ]; // TEMPORARY until a better solution for standard Unity shaders is found

    private static IEnumerator ReloadAddressableShaders(GameObject? parent = null)
    {
        yield return new WaitForEndOfFrame();
        yield return InitCacheShaders();
        CustomContentModule.Logger?.VerboseLog(ConsoleColor.DarkYellow, "Reloading Addressable shaders. . .");
        Il2CppArrayBase<MeshRenderer> renderers;
        if (!parent || parent == null)
        {
            renderers = Object.FindObjectsOfType<MeshRenderer>();
        }
        else
        {
            var rendList = new List<MeshRenderer>();
            rendList.AddRange(parent.GetComponents<MeshRenderer>());
            rendList.AddRange(parent.GetComponentsInChildren<MeshRenderer>());

            renderers = new Il2CppReferenceArray<MeshRenderer>([.. rendList]);
        }

        foreach (var meshRenderer in renderers)
        {
            foreach (var material in meshRenderer.sharedMaterials)
            {
                if (material is null) continue;
                if (CachedShaders.TryGetValue(material.shader.name, out var shader) &&
                    !BlacklistedShaderNames.Contains(material.shader.name))
                    material.shader = shader;
            }
        }

        CustomContentModule.Logger?.VerboseLog(ConsoleColor.DarkGreen, "Reloaded Addressable shaders!");
    }

    private static IEnumerator InitCacheShaders()
    {
        CustomContentModule.Logger?.Msg("Caching Addressable game shaders, please wait. . .");
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        foreach (var locator in Addressables.ResourceLocators.ToArray())
        {
            var locatorKeys = locator.Keys.ToList();
            foreach (var locatorKey in locatorKeys)
            {
                var result = locator.Locate(locatorKey, Il2CppType.Of<Shader>(),
                    out Il2CppSystem.Collections.Generic.IList<IResourceLocation> list);
                if (!result) continue;

                foreach (var location in list.Cast<Il2CppReferenceArray<IResourceLocation>>())
                {
                    var assetHandle = Addressables.LoadAssetAsync<Shader>(location);
                    yield return assetHandle.HandleAsynchronousAddressableOperation();

                    if (!AssetUtilities.IsHandleSuccess(assetHandle))
                    {
                        CustomContentModule.Logger?.VerboseLog(
                            ConsoleColor.DarkYellow,
                            $"Addressable shader of key {location.PrimaryKey} could not be loaded, not caching. . .");
                        continue;
                    }

                    assetHandle.Result.MakePersistent();
                    CachedShaders[assetHandle.Result.name] = assetHandle.Result;

                    assetHandle.Release();
                }
            }
        }

        stopwatch.Stop();
        CustomContentModule.Logger?.Msg(
            ConsoleColor.Green,
            $"Caching Addressable game shaders done! Total time taken: {stopwatch.ElapsedMilliseconds}ms");
    }

    private static void StartShaderReload(Scene scene, LoadSceneMode mode)
    {
        _ = MelonCoroutines.Start(ReloadAddressableShaders());
    }

    internal static void Initialize()
    {
        _ = MelonCoroutines.Start(InitCacheShaders());
        SceneManager.add_sceneLoaded((UnityAction<Scene, LoadSceneMode>)StartShaderReload);
    }
}
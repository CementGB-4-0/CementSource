using CementGB.Mod.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace CementGB.Mod.Modules;

[MelonLoader.RegisterTypeInIl2Cpp]
public class GameObjectGrabber : MonoBehaviour
{
    private static readonly Dictionary<string, List<GameObject>> _cachedGameObjects = []; // { original scene: grabbed GameObjects }
    private static readonly Dictionary<string, List<string>> _pathsToCache = [];

    /// <summary>
    /// Marks a <see cref="GameObject"/>'s path within a given scene for storing/caching.
    /// Once the game's resources load, we load the <see cref="SceneInstance"/> from <paramref name="sceneName"/> and use <see cref="GameObject.Find"/> with <paramref name="goFindPath"/> to get the <see cref="GameObject"/>.
    /// </summary>
    public static void MarkForGrabbing(string sceneName, string goFindPath)
    {
        if (_pathsToCache.ContainsKey(sceneName))
            _pathsToCache[sceneName].Add(goFindPath);
        else
            _pathsToCache.Add(sceneName,
            [
                goFindPath
            ]);
    }

    private static void MarkCached(string sceneName, GameObject cached)
    {
        if (_cachedGameObjects.ContainsKey(sceneName))
            _cachedGameObjects[sceneName].Add(cached);
        else
            _cachedGameObjects.Add(sceneName,
            [
                cached
            ]);
    }

    // TODO: This method errors out even though "Try" methods shouldn't do that
    private static bool TryFindSceneInstanceByAddress(out SceneInstance sceneInstance, string sceneAddress)
    {
        var loadHandle = Addressables.LoadSceneAsync(sceneAddress, UnityEngine.SceneManagement.LoadSceneMode.Additive, false).Acquire();
        sceneInstance = loadHandle.WaitForCompletion();

        loadHandle.Release();
        return sceneInstance != null;
    }

    private static void DisposeOfScene(ref SceneInstance sceneInstance)
    {
        var unloadHandle = Addressables.UnloadSceneAsync(sceneInstance).Acquire();
        unloadHandle.WaitForCompletion();
        unloadHandle.Release();
    }

    private void Update()
    {
        if (!CommonHooks.GlobalInitialized) return;
        foreach (var toCache in _pathsToCache)
        {
            if (!TryFindSceneInstanceByAddress(out var sceneInstance, toCache.Key))
            {
                LoggingUtilities.VerboseLog(ConsoleColor.DarkRed, $"Could not find or load SceneInstance from address \"{toCache.Key}.");
                _pathsToCache.Remove(toCache.Key);
                continue;
            }

            foreach (var goPath in toCache.Value)
            {
                var foundGo = GameObject.Find(goPath);
                if (foundGo == null)
                {
                    LoggingUtilities.VerboseLog(ConsoleColor.DarkRed, $"Could not find GameObject, path \"{toCache.Value}\", from scene {sceneInstance.Scene.name}. Disposing of loaded scene and returning. . .");
                    DisposeOfScene(ref sceneInstance);
                    _pathsToCache.Remove(toCache.Key);
                    continue;
                }

                var foundGoClone = UnityEngine.Object.Instantiate(foundGo);
                UnityEngine.Object.DontDestroyOnLoad(foundGoClone);
                MarkCached(toCache.Key, foundGoClone);
            }
        }
    }
}

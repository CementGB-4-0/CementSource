using System;
using System.Collections.Generic;
using GBMDK;
using Il2CppGB.Data.Loading;

namespace CementGB.Mod.CustomContent;

/// <summary>
/// Holds references to scene data loaded on mod init to prevent garbage collection and to cache loading results.
/// </summary>
public class CustomSceneRefHolder
{
    private static List<CustomSceneRefHolder> _instances = [];

    public static CustomSceneRefHolder Get(string sceneName, bool mustBeValid = false)
    {
        foreach (var instance in _instances)
        {
            if (!instance.IsValid && mustBeValid) 
                continue;

            if (instance.sceneData.name.StartsWith(sceneName))
                return instance;
        }

        return null;
    }

    public SceneData sceneData;
    public CustomMapInfo sceneInfo;

    public bool IsValid
    {
        get
        {
            return sceneData && sceneInfo;
        }
    }
    
    public CustomSceneRefHolder(SceneData sceneData, CustomMapInfo customMapInfo)
    {
        this.sceneData = sceneData;
        this.sceneInfo = customMapInfo;
    }
}
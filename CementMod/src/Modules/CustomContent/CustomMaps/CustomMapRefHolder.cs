using System;
using CementGB.Mod.Utilities;
using GBMDK;
using Il2CppGB.Data.Loading;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace CementGB.Mod.CustomContent;

/// <summary>
/// Holds references to scene data loaded on mod init to prevent garbage collection and to cache loading results.
/// </summary>
public class CustomMapRefHolder(IResourceLocation sceneDataLoc, CustomMapInfo customMapInfo)
{
    public string SceneName => sceneDataLoc.PrimaryKey;

    public SceneData SceneData
    {
        get
        {
            if (_sceneData) return _sceneData;
            
            var sceneDataHandle = Addressables.LoadAssetAsync<SceneData>(sceneDataLoc);
            if (!sceneDataHandle.HandleSynchronousAddressableOperation())
                throw new Exception("Failed to load scene data from cached ResourceLocation!");
                
            _sceneData = sceneDataHandle.Result;

            return _sceneData;
        }
    }

    private SceneData _sceneData;
    
    public CustomMapInfo sceneInfo = customMapInfo;

    public bool IsValid
    {
        get
        {
            return sceneDataLoc != null && sceneInfo;
        }
    }
}
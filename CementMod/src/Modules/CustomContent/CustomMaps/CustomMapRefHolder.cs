using System;
using CementGB.Mod.Utilities;
using GBMDK;
using Il2CppGB.Data.Loading;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace CementGB.Mod.CustomContent;

/// <summary>
/// Holds references to scene data loaded on mod init to hold onto and cache loading results.
/// </summary>
public class CustomMapRefHolder(IResourceLocation sceneDataLoc, CustomMapInfo customMapInfo)
{
    /// <summary>
    /// The name of the map, parsed from the loaded SceneData.
    /// </summary>
    public string SceneName => sceneDataLoc.PrimaryKey.Split("-Data")[0];

    /// <summary>
    /// Provides data for each gamemode, along with a reference to the custom scene itself.
    /// Loads the SceneData of the map from sceneDataLoc and returns it.
    /// </summary>
    /// <exception cref="Exception">Throws if the SceneData failed to load.</exception>
    public SceneData SceneData
    {
        get
        {
            if (_sceneData) return _sceneData;
            
            var sceneDataHandle = Addressables.LoadAssetAsync<SceneData>(sceneDataLoc);
            if (!sceneDataHandle.HandleSynchronousAddressableOperation())
                throw new Exception("Failed to load scene data from cached ResourceLocation!");
                
            _sceneData = sceneDataHandle.Result;
            sceneDataHandle.Release();
            return _sceneData;
        }
    }

    private SceneData _sceneData;
    
    /// <summary>
    /// Provides gamemode selection info for the map. May be null depending on how the map was created in GBMDK.
    /// </summary>
    public readonly CustomMapInfo SceneInfo = customMapInfo;

    /// <summary>
    /// Checks if the ref holder has all it needs to function properly in patches.
    /// </summary>
    /// <seealso cref="SceneData"/>
    /// <seealso cref="SceneInfo"/>
    public bool IsValid
    {
        get
        {
            return sceneDataLoc != null;
        }
    }
}
using System;
using CementGB.Mod.Utilities;
using GBMDK;
using Il2CppGB.Data.Loading;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace CementGB.Mod.CustomContent;

/// <summary>
///     Holds references to scene data loaded on mod init to hold onto and cache loading results.
/// </summary>
public class CustomMapRefHolder(IResourceLocation sceneDataLoc, CustomMapInfo customMapInfo)
{
    private SceneData _sceneData;

    public CustomMapInfo sceneInfo = customMapInfo;
    public string SceneName => sceneDataLoc.PrimaryKey.Split("-Data")[0];

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

    public bool IsValid => sceneDataLoc != null;
}
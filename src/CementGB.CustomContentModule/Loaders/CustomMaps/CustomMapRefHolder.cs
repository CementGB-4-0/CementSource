using GBMDK;
using Il2CppGB.Data.Loading;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace CementGB.Modules.CustomContent;

public class CustomMapRefHolder(IResourceLocation sceneDataLoc, IResourceLocation? sceneInfoLoc = null)
    : CustomContentRefHolder(sceneDataLoc, sceneInfoLoc)
{
    /// <summary>
    ///     The name of the map, parsed from SceneData addressable key.
    /// </summary>
    public readonly string SceneName = sceneDataLoc.PrimaryKey.Replace("-Data", "");

    public AsyncOperationHandle<CustomMapInfo>? SceneInfoHandle =>
        sceneInfoLoc != null ? Addressables.LoadAssetAsync<CustomMapInfo>(sceneInfoLoc) : null;

    public override Type[] AssetTypes => [typeof(SceneData), typeof(CustomMapInfo)];
    public override string MainContentName => SceneName;
}
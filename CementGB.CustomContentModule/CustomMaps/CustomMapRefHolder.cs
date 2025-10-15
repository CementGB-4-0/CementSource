using CementGB.Modules.CustomContent;
using GBMDK;
using Il2CppGB.Data.Loading;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace CementGB.Modules.CustomContent;

/// <summary>
///     Holds references to scene data loaded on mod init to hold onto and cache loading results.
/// </summary>
public class CustomMapRefHolder(IResourceLocation sceneDataLoc, IResourceLocation? mapInfoLoc = null) : CustomContentRefHolder(sceneDataLoc, mapInfoLoc)
{
    /// <summary>
    ///     The name of the map, parsed from the loaded SceneData.
    /// </summary>
    public readonly string SceneName = sceneDataLoc.PrimaryKey.Split("-Data")[0];

    /// <summary>
    ///     Provides gamemode selection info for the map.
    /// </summary>
    public CustomMapInfo SceneInfo => (CustomMapInfo?)RetrieveAssetOfKey(mapInfoLoc?.PrimaryKey, typeof(CustomMapInfo)) ?? CustomMapInfo.CreateDefault(SceneName);

    public SceneData? SceneData => RetrieveAssetOfKey(sceneDataLoc.PrimaryKey, typeof(SceneData))?.Cast<SceneData>();

    public override Type[] AssetTypes => [typeof(SceneData), typeof(CustomMapInfo)];
    public override string CustomContentTypeString => "CustomMaps";
    public override string MainContentName => SceneName;
}
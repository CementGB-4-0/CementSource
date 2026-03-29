using CementGB.Modules.CustomContent.Utilities;
using GBMDK;
using Il2CppGB.Data.Loading;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace CementGB.Modules.CustomContent;

/// <summary>
///     Holds references to scene data loaded on mod init to hold onto and cache loading results.
/// </summary>
public class CustomMapRefHolder(IResourceLocation sceneDataLoc, IResourceLocation? mapInfoLoc = null)
    : CustomContentRefHolder(sceneDataLoc, mapInfoLoc)
{
    public readonly AssetReferenceT<SceneData> SceneData = new(sceneDataLoc.PrimaryKey);

    /// <summary>
    ///     The name of the map, parsed from the loaded SceneData's addressable key.
    /// </summary>
    public readonly string SceneName = sceneDataLoc.PrimaryKey.Split("-Data")[0];

    /// <summary>
    ///     Provides gamemode selection info for the map.
    /// </summary>
    public CustomMapInfo SceneInfo =>
        AssetUtilities.RetrieveAssetOfKey(mapInfoLoc?.PrimaryKey, typeof(CustomMapInfo))?.Cast<CustomMapInfo>() ??
        CustomMapInfo.CreateDefault(SceneName);

    public override Type[] AssetTypes => [typeof(SceneData), typeof(CustomMapInfo)];
    public override string CustomContentTypeString => "CustomMaps";
    public override string MainContentName => SceneName;
}
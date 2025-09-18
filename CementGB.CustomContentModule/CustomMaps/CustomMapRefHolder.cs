using System;
using CementGB.Modules.CustomContent.Utilities;
using CementGB.Mod.Utilities;
using CementGB.Modules.CustomContent;
using GBMDK;
using Il2CppGB.Data.Loading;
using Il2CppInterop.Runtime;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.SceneManagement;

namespace CementGB.Mod.CustomContent;

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
    ///     Provides gamemode selection info for the map. May be null depending on how the map was created in GBMDK.
    /// </summary>
    public CustomMapInfo SceneInfo => (CustomMapInfo?)RetrieveAssetOfKey(mapInfoLoc?.PrimaryKey, typeof(CustomMapInfo)) ?? CustomMapInfo.CreateDefault(SceneName);

    public SceneData? SceneData => (SceneData?)RetrieveAssetOfKey(sceneDataLoc.PrimaryKey, typeof(SceneData));

    public override Type[] AssetTypes => [typeof(SceneData), typeof(CustomMapInfo)];
    public override string CustomContentTypeString { get; }
    public override string MainContentName { get; }

    /// <summary>
    ///     Checks if the ref holder has all it needs to function properly in patches.
    /// </summary>
    /// <seealso cref="SceneData" />
    /// <seealso cref="SceneInfo" />
    public new bool IsValid => SceneInfo != null && SceneData != null;
}
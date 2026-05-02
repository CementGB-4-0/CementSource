using System.Collections;
using CementGB.Modules.CustomContent.Utilities;
using GBMDK;
using Il2CppGB.Data.Loading;
using MelonLoader;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = UnityEngine.Object;

namespace CementGB.Modules.CustomContent;

/// <summary>
///     Holds references to scene data loaded on mod init to hold onto and cache loading results.
/// </summary>
public class CustomMapRefHolder : CustomContentRefHolder
{
    private readonly IResourceLocation? _mapInfoLoc;
    public readonly AssetReferenceT<SceneData> SceneData;

    /// <summary>
    ///     The name of the map, parsed from the loaded SceneData's addressable key.
    /// </summary>
    public readonly string SceneName;

    /// <summary>
    ///     Holds references to scene data loaded on mod init to hold onto and cache loading results.
    /// </summary>
    public CustomMapRefHolder(IResourceLocation sceneDataLoc, IResourceLocation? mapInfoLoc = null)
    {
        _mapInfoLoc = mapInfoLoc;
        SceneData = new AssetReferenceT<SceneData>(sceneDataLoc.PrimaryKey);
        SceneName = sceneDataLoc.PrimaryKey.Split("-Data")[0];
        SceneInfo = CustomMapInfo.CreateDefault(SceneName);

        MelonCoroutines.Start(
            GetProvidedSceneInfo()); // This fucking sucks but for some reason its causing issues when I just load SceneInfo synchronously
    }

    /// <summary>
    ///     Provides gamemode selection info for the map.
    /// </summary>
    public CustomMapInfo? SceneInfo { get; private set; }

    public override Type[] AssetTypes => [typeof(SceneData), typeof(CustomMapInfo)];
    public override string CustomContentTypeString => "CustomMaps";
    public override string MainContentName => SceneName;

    private IEnumerator GetProvidedSceneInfo()
    {
        if (_mapInfoLoc == null) yield break;
        var handle = Addressables.LoadAssetAsync<Object>(_mapInfoLoc);
        yield return handle;
        if (AssetUtilities.IsHandleSuccess(handle))
            SceneInfo = handle.Result.Cast<CustomMapInfo>();
    }
}
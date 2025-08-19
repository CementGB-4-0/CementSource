using CementGB.Mod.Utilities;
using GBMDK;
using Il2CppGB.Data.Loading;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace CementGB.Mod.CustomContent;

/// <summary>
///     Holds references to scene data loaded on mod init to hold onto and cache loading results.
/// </summary>
public class CustomMapRefHolder(IResourceLocation sceneDataLoc, IResourceLocation? mapInfoLoc = null)
{
    /// <summary>
    ///     The name of the map, parsed from the loaded SceneData.
    /// </summary>
    public readonly string? SceneName = sceneDataLoc.PrimaryKey?.Split("-Data")[0];

    /// <summary>
    ///     Provides gamemode selection info for the map. May be null depending on how the map was created in GBMDK.
    /// </summary>
    public CustomMapInfo? SceneInfo { get; private set; }
    public SceneData? SceneData { get; private set; }

    /// <summary>
    ///     Checks if the ref holder has all it needs to function properly in patches.
    /// </summary>
    /// <seealso cref="SceneData" />
    /// <seealso cref="SceneInfo" />
    public bool IsValid => SceneInfo != null && SceneData != null;

    internal void LoadReferences()
    {
        if (SceneName == null)
            return;

        var sceneDataHandle = Addressables.LoadAssetAsync<SceneData>(sceneDataLoc);
        var sceneInfo = CustomMapInfo.CreateDefault(SceneName);
        if (mapInfoLoc != null)
        {
            var mapInfoHandle = Addressables.LoadAssetAsync<CustomMapInfo>(mapInfoLoc);
            if (mapInfoHandle.HandleSynchronousAddressableOperation())
            {
                sceneInfo = mapInfoHandle.Result;
                sceneInfo.MakePersistent();
            }
        }

        if (!sceneDataHandle.HandleSynchronousAddressableOperation())
            throw new System.Exception($"Scene data failed to load from ResourceLocation! | PrimaryKey: {sceneDataLoc.PrimaryKey}");

        var sceneData = sceneDataHandle.Result;
        sceneData.MakePersistent();

        SceneInfo = sceneInfo;
        SceneData = sceneData;
    }
}
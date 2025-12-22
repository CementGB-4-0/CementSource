using System.Text;
using CementGB.Modules.CustomContent.Utilities;
using CementGB.Utilities;
using Il2CppInterop.Runtime;
using MelonLoader;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = UnityEngine.Object;

namespace CementGB.Modules.CustomContent;

public abstract class CustomContentRefHolder
{
    private static readonly Type RequiredBaseType = typeof(Object);

    private readonly HashSet<Object> _cachedAssets = [];

    protected readonly IResourceLocation?[] ResourceLocations;

    protected CustomContentRefHolder(params IResourceLocation?[] resourceLocations)
    {
        ResourceLocations = resourceLocations;
        _ = LoadUncachedAssets();
    }

    private static MelonLogger.Instance? Logger => CustomContentModule.Logger;

    public abstract Type[] AssetTypes { get; }
    public abstract string CustomContentTypeString { get; }
    public abstract string MainContentName { get; }

    public bool IsValid => AssetTypes.Length > 0
                           && AssetTypes.All(assetType => assetType.IsAssignableTo(RequiredBaseType));

    public event Action? AfterAllAssetsLoaded;
    public event Action<Object>? AfterAssetLoad;

    public Object? RetrieveAssetOfKey(string? key, Type? assetType = null)
    {
        assetType ??= RequiredBaseType;
        var cachedAsset = _cachedAssets.FirstOrDefault(asset =>
            asset.GetType().IsCastableTo(assetType) && asset.Cast<Object>().name == key);
        if (cachedAsset != null) return cachedAsset;

        if (key == null)
        {
            Logger?.VerboseLog(ConsoleColor.DarkRed, $"Argument {nameof(key)} is null!");
            return null;
        }

        var handle = Addressables.LoadAssetAsync<Il2CppSystem.Object>(key);
        if (!handle.HandleSynchronousAddressableOperation())
            throw new Exception(
                $"Failed to load asset of key: {key} (Is the object name different from the Addressable key?) | {nameof(assetType)} : {assetType}");

        cachedAsset = handle.Result.Cast<Object>();
        CacheLoadedAsset(cachedAsset);

        return cachedAsset;
    }

    private Object[] LoadUncachedAssets(bool cache = true)
    {
        var ret = new List<Object>();
        foreach (var location in ResourceLocations)
        {
            if (location == null) continue;
            var asset = LoadUncachedAsset(location);
            if (asset == null) continue;
            AfterAssetLoad?.Invoke(asset);
            if (cache)
                CacheLoadedAsset(asset);
            ret.Add(asset);
        }

        AfterAllAssetsLoaded?.Invoke();
        return [.. ret];
    }

    private Object? LoadUncachedAsset(IResourceLocation location)
    {
        if (ResourceLocations.All(loc => loc != location) ||
            AssetTypes.All(type => Il2CppType.From(type) != location.ResourceType) ||
            _cachedAssets.Any(ase => ase.Cast<Object>().name == location.PrimaryKey)) return null;
        var handle = Addressables.LoadAssetAsync<Object>(location);
        return handle.HandleSynchronousAddressableOperation()
            ? handle.Result
            : throw new Exception($"Failed to load asset of key \"{location.PrimaryKey}\"!");
    }

    private void CacheLoadedAsset(Object obj)
    {
        obj.Cast<Object>().MakePersistent();
        _ = _cachedAssets.Add(obj);
    }

    private string DumpRequiredAssetTypes()
    {
        var builder = new StringBuilder();
        for (var index = 0; index < AssetTypes.Length; index++)
        {
            var requiredAssetType = AssetTypes[index];
            builder = builder.AppendLine($"{index} : {requiredAssetType.FullName}");
        }

        return builder.ToString();
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CementGB.Mod.Modules.CustomContent.Utilities;
using CementGB.Mod.Utilities;
using Il2CppInterop.Runtime;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace CementGB.Mod.Modules.CustomContent;

public abstract class CustomContentRefHolder
{
    private static readonly Type RequiredBaseType = typeof(UnityEngine.Object);

    public abstract Type[] AssetTypes { get; }
    public abstract string CustomContentTypeString { get; }
    public abstract string MainContentName { get; }

    public virtual bool IsValid => AssetTypes.Length > 0
                                   && AssetTypes.All(assetType => assetType.IsCastableTo(RequiredBaseType))
                                   && ResourceLocations.All(loc =>
                                       loc != null && loc.ResourceType.IsCastableTo(Il2CppType.From(RequiredBaseType)));

    public event Action? AfterAllAssetsLoaded;
    public event Action<Il2CppSystem.Object>? AfterAssetLoad;

    protected readonly IResourceLocation?[] ResourceLocations;

    private readonly HashSet<Il2CppSystem.Object> _cachedAssets = [];

    protected CustomContentRefHolder(params IResourceLocation[] resourceLocations)
    {
        ResourceLocations = resourceLocations;
        _ = LoadUncachedAssets();
    }

    public Il2CppSystem.Object? RetrieveAssetOfKey(string? key, Type? assetType = null)
    {
        if (!IsValid)
        {
            LoggingUtilities.VerboseLog(ConsoleColor.DarkYellow,
                $"{nameof(RetrieveAssetOfKey)} called on invalid {nameof(CustomContentRefHolder)}! | CustomContentTypeString: {CustomContentTypeString} | MainContentName: {MainContentName} | Provided {nameof(assetType)}: {assetType} | {nameof(AssetTypes)}: {DumpRequiredAssetTypes()}");
            return null;
        }

        assetType ??= RequiredBaseType;
        var cachedAsset = _cachedAssets.FirstOrDefault(asset => asset.GetType().IsCastableTo(assetType) && asset.Cast<UnityEngine.Object>().name == key);
        if (cachedAsset != null) return cachedAsset;

        var handle = Addressables.LoadAssetAsync<Il2CppSystem.Object>(key);
        if (!handle.HandleSynchronousAddressableOperation())
            throw new Exception($"Failed to load asset of key: {key} (Is the object name different from the Addressable key?) | {nameof(assetType)} : {assetType}");

        cachedAsset = handle.Result;
        CacheLoadedAsset(cachedAsset);

        return cachedAsset;
    }

    private Il2CppSystem.Object[] LoadUncachedAssets(bool cache = true)
    {
        var ret = new List<Il2CppSystem.Object>();
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

    private Il2CppSystem.Object? LoadUncachedAsset(IResourceLocation location)
    {
        if (ResourceLocations.All(loc => loc != location) || AssetTypes.All(type => Il2CppType.From(type) != location.ResourceType) || _cachedAssets.Any(ase => ase.Cast<UnityEngine.Object>().name == location.PrimaryKey)) return null;
        var handle = Addressables.LoadAssetAsync<Il2CppSystem.Object>(location);
        return handle.HandleSynchronousAddressableOperation() ? handle.Result : throw new Exception($"Failed to load asset of key \"{location.PrimaryKey}\"!");
    }

    private void CacheLoadedAsset(Il2CppSystem.Object obj)
    {
        obj.Cast<UnityEngine.Object>().MakePersistent();
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
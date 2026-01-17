using CementGB.Utilities;
using MelonLoader;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = UnityEngine.Object;

namespace CementGB.Modules.CustomContent;

public abstract class CustomContentRefHolder
{
    private static readonly Type RequiredBaseType = typeof(Object);
    protected readonly IResourceLocation?[] ResourceLocations;

    protected CustomContentRefHolder(params IResourceLocation?[] resourceLocations)
    {
        ResourceLocations = resourceLocations;
    }

    private static MelonLogger.Instance? Logger => CustomContentModule.Logger;

    public abstract Type[] AssetTypes { get; }
    public abstract string MainContentName { get; }

    public bool IsValid => AssetTypes.Length > 0
                           && AssetTypes.All(assetType => assetType.IsAssignableTo(RequiredBaseType));

    public AssetReference? RetrieveReferenceOfKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Logger?.VerboseLog(ConsoleColor.DarkRed, $"Argument {nameof(key)} is empty!");
            return null;
        }

        var assetLoc = ResourceLocations.FirstOrDefault(loc => loc != null && loc.PrimaryKey == key);
        if (assetLoc == null) return null;
        var assetRef = new AssetReference(assetLoc.PrimaryKey);
        return assetRef;
    }
}
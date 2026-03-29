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

    public abstract Type[] AssetTypes { get; }
    public abstract string CustomContentTypeString { get; }
    public abstract string MainContentName { get; }

    public bool IsValid => AssetTypes.Length > 0
                           && AssetTypes.All(assetType => assetType.IsAssignableTo(RequiredBaseType));
}
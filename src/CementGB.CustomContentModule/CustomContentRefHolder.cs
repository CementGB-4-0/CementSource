namespace CementGB.Modules.CustomContent;

public abstract class CustomContentRefHolder
{
    public abstract Type[] AssetTypes { get; }
    public abstract string CustomContentTypeString { get; }
    public abstract string MainContentName { get; }
}
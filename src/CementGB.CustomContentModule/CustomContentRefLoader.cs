namespace CementGB.Modules.CustomContent;

public abstract class CustomContentRefLoader
{
    public abstract string CustomContentTypeString { get; }

    public abstract CustomContentRefHolder[] Load();
}
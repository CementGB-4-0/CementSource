using Tomlet.Attributes;

namespace CementGB.Modules.NetBeardModule;

public class NetBeardConfig
{
    private const string DefaultIP = "127.0.0.1";
    private const int DefaultPort = 5999;

    public static readonly NetBeardConfig
        Default = new();

    public bool AutoJoin = false;
    public bool Dedicated = false;
    public bool Fwd = false;

    public string IP = DefaultIP;
    public bool Joiner = true;
    public int Port = DefaultPort;

    public string ServerName = "NetBeard Server";

    [TomlInlineComment("Case-insensitive.")]
    public string StageName = "Random";

    public bool UpnpEnabled = false;
}
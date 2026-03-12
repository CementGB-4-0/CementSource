using System.Net;
using Tomlet.Attributes;

namespace CementGB.Modules.NetBeardModule;

public class NetBeardConfig
{
    public static readonly NetBeardConfig
        Default = new();
    
    public bool AutoJoin = false;
    public bool Dedicated = false;
    public bool Fwd = false;

    public string IP = NetBeardModule.DefaultIP;
    public int Port = NetBeardModule.DefaultPort;
    public bool Joiner = true;

    public string ServerName = "NetBeard Server";
    [TomlInlineComment("Case-insensitive.")]
    public string StageName = "Random";

    public bool UpnpEnabled = false;
}
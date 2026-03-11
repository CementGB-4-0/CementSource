using System.Net;

namespace CementGB.Modules.NetBeardModule;

public class NetBeardConfig
{
    public static readonly NetBeardConfig
        Default = new();

    public bool AutoJoin = false;
    public bool Dedicated = false;
    public bool Fwd = false;

    public IPAddress IP = IPAddress.Loopback;
    public int Port = NetBeardModule.DefaultPort;

    public string ServerName = "NetBeard Server";
    public string StageName = "random";

    public bool UpnpEnabled = false;
}
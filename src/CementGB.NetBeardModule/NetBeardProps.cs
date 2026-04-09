using System.Net;
using Il2CppGB.Platform.Lobby.Utils;

namespace CementGB.Modules.NetBeardModule;

public static class NetBeardProps
{
    public static bool IsServer => NetBeardConfig.Current.Dedicated;
    public static bool IsFwd => !IsServer && NetBeardConfig.Current.Fwd;
    public static bool PortForward => (IsServer || IsFwd) && NetBeardConfig.Current.UpnpEnabled;
    public static bool DontAutoStart => !NetBeardConfig.Current.AutoJoin;
    public static string IP => NetBeardConfig.Current.IP;
    public static int Port => NetBeardConfig.Current.Port;
    public static bool LowGraphicsMode => Environment.GetCommandLineArgs().Contains(CliFlagConstants.LowGraphicsArg);
    public static IPAddress? LocalExternalIP { get; } = IPAddress.Parse(IPAddressFetcher.GetIPAddress());
}
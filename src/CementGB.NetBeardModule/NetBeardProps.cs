using System.Net;

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
    public static IPAddress? LocalExternalIP { get; private set; }

    internal static async void Init()
    {
        LocalExternalIP = await GetLocalExternalIP();
    }

    private static async Task<IPAddress?> GetLocalExternalIP()
    {
        var externalIpString = (await new HttpClient().GetStringAsync("https://ipv4.icanhazip.com"))
            .Replace(@"\r\n", "").Replace("\\n", "").Trim();
        return !IPAddress.TryParse(externalIpString, out var ipAddress) ? null : ipAddress;
    }
}
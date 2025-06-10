using System;
using System.IO.Pipes;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CementGB.Mod.Modules.NetBeard;

internal static class ServerChecker
{
    public static NamedPipeServerStream ServerStream { get; private set; }
    public static IPAddress UserIP { get; private set; }


    public static async void Awake()
    {
        UserIP = await GetExternalIpAddress();
        if (ServerManager.IsServer)
        {
            ServerStream = new NamedPipeServerStream("GBServer", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            UnityEngine.Application.add_quitting(new Action(() => ServerStream?.Dispose()));
        }
    }

    internal static async Task<IPAddress> GetExternalIpAddress()
    {
        var externalIpString = (await new HttpClient().GetStringAsync("http://icanhazip.com"))
            .Replace("\\r\\n", "").Replace("\\n", "").Trim();
        if (!IPAddress.TryParse(externalIpString, out var ipAddress)) return null;
        return ipAddress;
    }

    public static bool IsServerRunning()
    {
        try
        {
            using (NamedPipeClientStream serverChecker = new("GBServer", "GBClient", PipeDirection.InOut))
            {
                serverChecker.Connect(100);
                return true;
            }
        }

        catch
        {
            return false;
        }
    }
}

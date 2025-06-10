using System;
using System.IO.Pipes;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CementGB.Mod.Modules.NetBeard;

internal static class ServerChecker
{
    public static Mutex ServerMutex { get; private set; }
    public static IPAddress UserIP { get; private set; }


    public static async void Awake()
    {
        UserIP = await GetExternalIpAddress();
        
        if (ServerManager.IsServer)
        {
            ServerMutex = new Mutex(true, "Global\\GBServer", out _);
            UnityEngine.Application.add_quitting(new Action(() => ServerMutex?.Dispose()));
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
            Mod.Logger.Msg(ConsoleColor.Magenta, "Finding server Mutex. . .");
            using Mutex foundMutex = Mutex.OpenExisting("Global\\GBServer");
            Mod.Logger.Msg(ConsoleColor.Magenta, "Server Mutex found");
            return true;
        }
        catch (Exception ex)
        {
            Mod.Logger.Msg(ConsoleColor.Magenta, $"Failed to find server Mutex\n{ex}");
            return false;
        }
    }
}

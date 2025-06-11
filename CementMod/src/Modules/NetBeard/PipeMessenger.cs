using Il2CppCoatsink.Platform.Systems.Progression;
using Il2CppGB.UnityServices.Matchmaking;
using Newtonsoft.Json;
using System;
using System.IO.Pipes;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CementGB.Mod.Modules.NetBeard;

internal static class PipeMessenger
{
    public static Mutex ServerMutex { get; private set; }
    public static PipeMessager ServerPipe { get; private set; }
    public static PipeMessager ClientPipe { get; private set; }
    public static IPAddress UserIP { get; private set; }


    public static async void Awake()
    {
        UnityEngine.Application.add_quitting(new Action(() =>
        {
            ServerMutex?.Dispose();
            ServerPipe?.Dispose();
            ClientPipe?.Dispose();
        }));

        if (ServerManager.IsServer)
        {
            // Still relying on mutex for checking if server exists
            // As pipes have weird low-level kernel issues if a connection fails causing a hanging thread
            ServerMutex = new Mutex(true, "Global\\GBServer", out _);

#pragma warning disable CA1416 // Only supports windows, this is fine because our mods only support windows anyways
            ServerPipe = new(new NamedPipeServerStream("GBServer", PipeDirection.InOut, 1, PipeTransmissionMode.Message), HandleServerMessage);
#pragma warning restore CA1416
        }

        else
        {
            UserIP = await GetExternalIpAddress();
            ClientPipe = new(new NamedPipeClientStream("GBServer", "GBClient", PipeDirection.InOut, PipeOptions.Asynchronous), HandleClientMessage);
            await ClientPipe.ConnectToServer();
        }
    }

    public static void HandleServerMessage(string identifier, string payload)
    {
        if (identifier == "GBGameData")
        {
            Mod.Logger.Error($"Ahahaha {payload}");
        }
    }

    public static void HandleClientMessage(string identifier, string payload)
    {

    }

    internal static async Task<IPAddress> GetExternalIpAddress()
    {
        var externalIpString = (await new HttpClient().GetStringAsync("http://icanhazip.com"))
            .Replace("\\r\\n", "").Replace("\\n", "").Trim();
        if (!IPAddress.TryParse(externalIpString, out var ipAddress)) return null;
        return ipAddress;
    }

    internal static bool IsServerRunning()
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

    internal static void SendLobbyDataToServer(GBGameData data)
    {
        if (!ClientPipe.NamedPipe.IsConnected) return;

        string serializedData = JsonConvert.SerializeObject(data);
        ClientPipe.WriteString(serializedData, "GBGameData");
    }
}

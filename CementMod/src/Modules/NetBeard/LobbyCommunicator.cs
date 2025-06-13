using Il2CppCoatsink.Platform.Systems.Progression;
using Il2CppGB.UnityServices.Matchmaking;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CementGB.Mod.Modules.NetBeard;

internal static class LobbyCommunicator
{
    public static IPAddress UserIP { get; private set; }


    public static async void Awake()
    {
        ClientServerCommunicator.Init();

        if (!ServerManager.IsServer)
        {
            UserIP = await GetExternalIpAddress();
        }

        else
        {
            ClientServerCommunicator.OnServerReceivedMessage += (prefix, payload) =>
            {
                if (prefix == "gamedata")
                {
                    GBGameData gameData = JsonConvert.DeserializeObject<GBGameData>(payload);
                    Mod.Logger.Msg(ConsoleColor.Magenta, payload);
                }
            };
        }
    }

    internal static void SendLobbyDataToServer(GBGameData data)
    {
        string serializedData = JsonConvert.SerializeObject(data);
        ClientServerCommunicator.QueueMessage("gamedata", serializedData);
    }

    internal static async Task<IPAddress> GetExternalIpAddress()
    {
        var externalIpString = (await new HttpClient().GetStringAsync("http://icanhazip.com"))
            .Replace("\\r\\n", "").Replace("\\n", "").Trim();
        if (!IPAddress.TryParse(externalIpString, out var ipAddress)) return null;
        return ipAddress;
    }
}

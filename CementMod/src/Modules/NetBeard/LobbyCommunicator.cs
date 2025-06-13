using Il2Cpp;
using Il2CppGB.Config;
using Il2CppGB.Core;
using Il2CppGB.Game;
using Il2CppGB.Gamemodes;
using Il2CppGB.UnityServices.Matchmaking;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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
                    Mod.Logger.Msg(ConsoleColor.Blue, "Received new modded session data");

                    if (gameData.MapName.ToLower() == "random")
                    {
                        GameManagerNew.Instance.tracker = MonoSingleton<Global>.Instance.Resources.GetData<GameModeSetupConfiguration>("GameModeSetupConfiguration");

                        GameModeEnum gameModeEnum = GameModeHelpers.GamemodeIDToEnum(gameData.Gamemode);
                        var mapsFor = GameManagerNew.Instance.tracker.Maps.GetMapsFor(gameModeEnum, false);

                        List<string> maps = new List<string>();
                        foreach (ModeMapStatus modeMapStatus in mapsFor)
                        {
                            maps.Add(modeMapStatus.MapName);
                        }

                        RotationConfig rotationConfig = GBConfigLoader.CreateRotationConfig(
                            maps.ToArray(), gameModeEnum,
                            gameData.NumberOfWins, true,
                            gameData.StageTimeLimit, 0);

                        GameManagerNew.Instance.ChangeRotationConfig(rotationConfig, 0);
                    }

                    else
                    {
                        RotationConfig rotationConfig = GBConfigLoader.CreateRotationConfig(
                            gameData.MapName, gameData.Gamemode,
                            gameData.NumberOfWins, gameData.StageTimeLimit);

                        GameManagerNew.Instance.ChangeRotationConfig(rotationConfig, 0);

                        GameManagerNew.Instance.gameManagerSetup = true;
                        GameManagerNew.Instance.authPassed = false;
                        GameManagerNew.Instance.joinTimer.Start(60f);
                        GameManagerNew.Instance._SceneManager.expectedNumPlayers = (int)gameData.TotalPlayerCountExclLocal;
                    }
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

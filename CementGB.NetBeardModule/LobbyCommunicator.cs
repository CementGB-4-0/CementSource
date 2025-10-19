using System.Collections;
using System.Net;
using Il2Cpp;
using Il2CppGB.Config;
using Il2CppGB.Core;
using Il2CppGB.Game;
using Il2CppGB.Gamemodes;
using Il2CppGB.UnityServices.Matchmaking;
using MelonLoader;
using Newtonsoft.Json;
using Random = UnityEngine.Random;

namespace CementGB.Modules.NetBeardModule;

internal static class LobbyCommunicator
{
    public static GBGameData? gameData;
    public static IPAddress? UserExternalIP { get; set; }

    private static MelonLogger.Instance? Logger => InstancedCementModule.GetModule<NetBeardModule>()?.Logger;

    public static async void Awake()
    {
        if (NetBeardModule.IsServer)
        {
            TCPCommunicator.OnServerReceivedMessage += (prefix, payload) =>
            {
                if (prefix == "gamedata")
                {
                    _ = MelonCoroutines.Start(HandleGBGameData(payload));
                }
            };
        }

        UserExternalIP = await GetExternalIpAddress();
    }

    private static IEnumerator HandleGBGameData(string payload)
    {
        // Absolutely 100% make sure we're running on the main thread
        // Hacky I know dont hate me I'll refactor :(
        yield return null;

        GameManagerNew.Instance.EndGameSession("DISCONNECT_GAME_COMPLETE");

        gameData = JsonConvert.DeserializeObject<GBGameData>(payload);
        if (gameData == null) yield break;

        Logger?.Msg(ConsoleColor.Blue, "Received new modded session data");

        if (gameData.MapName.ToLower() == "random")
        {
            GameManagerNew.Instance.tracker =
                MonoSingleton<Global>.Instance.Resources.GetData<GameModeSetupConfiguration>(
                    "GameModeSetupConfiguration");

            var gameModeEnum = GameModeHelpers.GamemodeIDToEnum(gameData.Gamemode);
            var mapsFor = GameManagerNew.Instance.tracker.Maps.GetMapsFor(gameModeEnum, false);

            var maps = new List<string>(mapsFor.Count);

            foreach (var _ in mapsFor)
            {
                var mapIndex = Random.Range(0, mapsFor.Count - 1);
                maps.Add(mapsFor[mapIndex].MapName);
            }

            var rotationConfig = GBConfigLoader.CreateRotationConfig(
                maps.ToArray(), gameModeEnum,
                gameData.NumberOfWins, true,
                gameData.StageTimeLimit);

            GameManagerNew.Instance.ChangeRotationConfig(rotationConfig);
        }
        else
        {
            var rotationConfig = GBConfigLoader.CreateRotationConfig(
                gameData.MapName, gameData.Gamemode,
                gameData.NumberOfWins, gameData.StageTimeLimit);

            GameManagerNew.Instance.ChangeRotationConfig(rotationConfig);
        }

        GameManagerNew.Instance.gameManagerSetup = true;
        GameManagerNew.Instance.authPassed = false;
        GameManagerNew.Instance.joinTimer.Start(60f);
        GameManagerNew.Instance._SceneManager.expectedNumPlayers = (int)gameData.TotalPlayerCountExclLocal;
    }

    internal static void SendLobbyDataToServer(GBGameData data)
    {
        var serializedData = JsonConvert.SerializeObject(data);
        TCPCommunicator.QueueMessage("gamedata", serializedData);
    }

    private static async Task<IPAddress?> GetExternalIpAddress()
    {
        var externalIpString = (await new HttpClient().GetStringAsync("https://ipv4.icanhazip.com"))
            .Replace(@"\r\n", "").Replace("\\n", "").Trim();
        return !IPAddress.TryParse(externalIpString, out var ipAddress) ? null : ipAddress;
    }
}
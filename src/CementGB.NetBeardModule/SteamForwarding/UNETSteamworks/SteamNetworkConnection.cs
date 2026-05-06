using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Steamworks;
using UnityEngine.Networking;

namespace CementGB.NetBeardModule.SteamForwarding.UNETSteamworks;

public class SteamNetworkConnection : NetworkConnection
{
    public SteamId steamId;

    public SteamNetworkConnection()
    {
    }

    public SteamNetworkConnection(SteamId steamId)
    {
        this.steamId = steamId;
    }

    public override bool TransportSend(Il2CppStructArray<byte> bytes, int numBytes, int channelId, out byte error)
    {
        if (steamId == SteamClient.SteamId)
        {
            // sending to self. short circuit
            TransportReceive(bytes, numBytes, channelId);
            error = 0;
            return true;
        }

        var eP2PSendType = P2PSend.Reliable;

        var qos = UNETSteamGlobals.hostTopology.DefaultConfig.Channels[channelId].QOS;
        if (qos == QosType.Unreliable || qos == QosType.UnreliableFragmented || qos == QosType.UnreliableSequenced)
        {
            eP2PSendType = P2PSend.Unreliable;
        }

        // Send packet to peer through Steam
        if (SteamNetworking.SendP2PPacket(steamId, bytes, numBytes, 0, eP2PSendType))
        {
            error = 0;
            return true;
        }

        error = 1;
        return false;
    }

    public void CloseP2PSession()
    {
        SteamNetworking.CloseP2PSessionWithUser(steamId);
        steamId = default;
    }
}
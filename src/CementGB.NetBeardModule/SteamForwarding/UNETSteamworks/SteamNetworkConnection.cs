using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Steamworks;
using UnityEngine.Networking;

namespace CementGB.NetBeardModule.SteamForwarding.UNETSteamworks;

public class SteamNetworkConnection : NetworkConnection
{
    public CSteamID steamId;

    public SteamNetworkConnection()
    {
    }

    public SteamNetworkConnection(CSteamID steamId)
    {
        this.steamId = steamId;
    }

    public override bool TransportSend(Il2CppStructArray<byte> bytes, int numBytes, int channelId, out byte error)
    {
        if (steamId.m_SteamID == SteamUser.GetSteamID().m_SteamID)
        {
            // sending to self. short circuit
            TransportReceive(bytes, numBytes, channelId);
            error = 0;
            return true;
        }

        var eP2PSendType = EP2PSend.k_EP2PSendReliable;

        var qos = SteamNetworkManager.hostTopology.DefaultConfig.Channels[channelId].QOS;
        if (qos == QosType.Unreliable || qos == QosType.UnreliableFragmented || qos == QosType.UnreliableSequenced)
        {
            eP2PSendType = EP2PSend.k_EP2PSendUnreliable;
        }

        // Send packet to peer through Steam
        if (SteamNetworking.SendP2PPacket(steamId, bytes, (uint)numBytes, eP2PSendType, channelId))
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
        steamId = CSteamID.Nil;
    }
}
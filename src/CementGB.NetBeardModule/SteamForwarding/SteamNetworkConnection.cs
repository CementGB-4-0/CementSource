using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using Steamworks;
using UnityEngine.Networking;
using CSteamID = Steamworks.CSteamID;
using SteamUser = Steamworks.SteamUser;

namespace CementGB.Modules.NetBeardModule;

[RegisterTypeInIl2Cpp]
public class SteamNetworkConnection : NetworkConnection
{
    public CSteamID SteamID;

    public SteamNetworkConnection()
    {
    }

    public SteamNetworkConnection(CSteamID steamID)
    {
        SteamID = steamID;
    }

    public override bool TransportSend(Il2CppStructArray<byte> bytes, int numBytes, int channelId, out byte error)
    {
        if (SteamID.m_SteamID == SteamUser.GetSteamID().m_SteamID)
        {
            // sending to self. short circuit
            TransportReceive(bytes, numBytes, channelId);
            error = 0;
            return true;
        }

        var eP2PSendType = EP2PSend.k_EP2PSendReliable;

        var qos = SteamNetworkManager.hostTopology.DefaultConfig.Channels[channelId].QOS;
        if (qos is QosType.Unreliable or QosType.UnreliableFragmented or QosType.UnreliableSequenced)
        {
            eP2PSendType = EP2PSend.k_EP2PSendUnreliable;
        }

        // Send packet to peer through Steam
        if (SteamNetworking.SendP2PPacket(SteamID, bytes, (uint)numBytes, eP2PSendType, channelId))
        {
            error = 0;
            return true;
        }

        error = 1;
        return false;
    }

    public void CloseP2PSession()
    {
        SteamNetworking.CloseP2PSessionWithUser(SteamID);
        SteamID = CSteamID.Nil;
    }
}
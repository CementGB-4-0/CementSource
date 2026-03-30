using Il2CppGB.Core;
using UnityEngine.Networking;

namespace CementGB.Modules.NetBeardModule;

public class SteamNetworkManager
{
    private static HostTopology? m_hostTopology;

    public static HostTopology hostTopology
    {
        get
        {
            if (m_hostTopology != null) return m_hostTopology;

            var config = new ConnectionConfig();
            config.AddChannel(QosType.ReliableSequenced);
            config.AddChannel(QosType.Unreliable);
            m_hostTopology = new HostTopology(config, Global.NetworkMaxPlayers);

            return m_hostTopology;
        }
    }
}
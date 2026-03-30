using UnityEngine.Networking;

namespace CementGB.Modules.NetBeardModule;

public class SteamNetworkClient : NetworkClient
{
    public SteamNetworkClient(NetworkConnection conn) : base(conn)
    {
    }

    public SteamNetworkConnection? SteamConnection => connection as SteamNetworkConnection;

    public string Status => m_AsyncConnect.ToString();

    public void Connect()
    {
        // Connect to localhost and trick UNET by setting ConnectState state to "Connected", which triggers some initialization and allows data to pass through TransportSend
        Connect("localhost", 0);
        m_AsyncConnect = ConnectState.Connected;

        // manually init connection
        connection.ForceInitialize();

        // send Connected message
        connection.InvokeHandlerNoData(MsgType.Connect);
    }

    public override void Disconnect()
    {
        m_AsyncConnect = ConnectState.Disconnected;

        if (m_Connection is { isConnected: true })
        {
            m_Connection.InvokeHandlerNoData(MsgType.Disconnect);

            SteamConnection?.CloseP2PSession();
            m_Connection.hostId = -1;
            m_Connection.Disconnect();
            m_Connection.Dispose();
            m_Connection = null;
        }
    }
}
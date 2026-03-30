using UnityEngine.Networking;

namespace CementGB.Modules.NetBeardModule;

public static class UNetExtensions
{
    private static int nextConnectionId = -1;

    /// Because we fake the UNET connection, connection initialization is not handled by UNET internally. 
    /// Connections must be manually initialized with this function.
    public static void ForceInitialize(this NetworkConnection conn)
    {
        var id = ++nextConnectionId;
        conn.Initialize("localhost", id, id, SteamNetworkManager.hostTopology);
    }
}
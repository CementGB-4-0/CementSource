using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace CementGB.Mod.Modules.NetBeard;

internal static class HandshakeManager
{
    public static TcpListener ServerStream { get; private set; }


    public static void Awake()
    {
        if (ServerManager.IsServer) ServerSetup();
    }

    public static async void ServerSetup()
    {
        ServerStream = new(System.Net.IPAddress.Loopback, 6942);
        ServerStream.Start();

        while (true)
        {
            using (TcpClient client = await ServerStream.AcceptTcpClientAsync())
            {
                if (client == null) continue;
                Mod.Logger.Msg(ConsoleColor.Yellow, "Handshake!");

                using NetworkStream clientStream = client.GetStream();
                clientStream.WriteByte(5);
            }
        }
    }

    public static async Task<bool> LookForHandshake()
    {
        try
        {
            using (TcpClient client = new())
            {
                await client.ConnectAsync(System.Net.IPAddress.Loopback, 6942);
                Mod.Logger.Msg(ConsoleColor.Magenta, "Server found");

                NetworkStream stream = client.GetStream();

                int attempts = 0;
                byte[] buffer = new byte[1];

                while (attempts < 3)
                {
                    attempts++;

                    Mod.Logger.Msg(ConsoleColor.Magenta, $"Looking for handshake: Attempt {attempts}");
                    int bufferChange = await stream.ReadAsync(buffer, default);

                    if (buffer[0] == 5 && bufferChange > 0)
                    {
                        Mod.Logger.Msg(ConsoleColor.Magenta, $"Handshake complete! (byte {buffer[0]} received)");
                        return true;
                    }

                    await Task.Delay(1000);
                }

                Mod.Logger.Msg(ConsoleColor.Red, "Server could not be found. Handshake failed");
                return false;
            }
        }

        catch (Exception ex)
        {
            Mod.Logger.Error("Error when looking for handshake. Server not listening? " + ex);
            return false;
        }
    }
}

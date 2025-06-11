using System;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CementGB.Mod.Modules.NetBeard;

internal class PipeMessager : IDisposable
{
    public PipeStream NamedPipe { get; set; }
    private CancellationTokenSource killSource = new();

    private Action<string, string> OnMessageReceived;


    public PipeMessager(PipeStream namedPipe, Action<string, string> messageCallback)
    {
        NamedPipe = namedPipe;

        OnMessageReceived = messageCallback;
        Task.Run(ReadLoop);
    }


    private async void ReadLoop()
    {
        try
        {
            while (!killSource.Token.IsCancellationRequested)
            {
                if (NamedPipe is NamedPipeServerStream serverPipe && !serverPipe.IsConnected)
                    await serverPipe.WaitForConnectionAsync();

                if (NamedPipe.IsConnected)
                {
                    string[] msg = ReadString();
                    if (msg != null && !string.IsNullOrWhiteSpace(msg[0]) && !string.IsNullOrWhiteSpace(msg[1])) OnMessageReceived(msg[0], msg[1]);
                }
            }
        }

        catch (Exception ex)
        {
            Mod.Logger.Error($"PipeMessager failed during ReadLoop\n{ex}");
        }

        killSource.Dispose();
    }

    public void WriteString(string data, string identifier)
    {
        if (!NamedPipe.IsConnected) return;

        data = identifier + ";" + data;
        byte[] byteData = Encoding.UTF8.GetBytes(data);

        NamedPipe.Write(byteData, 0, byteData.Length);
        NamedPipe.Flush(); // Send data and clear for next write
    }

    private string[] ReadString()
    {
        if (!NamedPipe.IsConnected) return null;

        byte[] dataBuffer = new byte[256];
        string fullMessage = "";

        while (!NamedPipe.IsMessageComplete)
        {
            int readBytes = NamedPipe.Read(dataBuffer, 0, dataBuffer.Length);
            fullMessage += Encoding.UTF8.GetString(dataBuffer, 0, readBytes);
        }

        string identifier = fullMessage.Split(";")[0];
        string data = fullMessage.Substring(fullMessage.IndexOf(";") + 1);

        return [identifier, data];
    }

    public async Task<bool> ConnectToServer(bool suppressInitialCheckLog = true)
    {
        if (NamedPipe is not NamedPipeClientStream || NamedPipe.IsConnected || !PipeMessenger.IsServerRunning())
        {
            if (!suppressInitialCheckLog)
                Mod.Logger.Msg(ConsoleColor.Yellow, "Invalid request for NamedPipe to connect to server");

            return false;
        }

        try
        {
            await (NamedPipe as NamedPipeClientStream).ConnectAsync(50);
            Mod.Logger.Msg(ConsoleColor.Yellow, "NamedPipe connected to server successfully");

            return true;
        }

        catch (Exception ex)
        {
            Mod.Logger.Msg(ConsoleColor.Yellow, $"NamedPipe failed to connect to server \n{ex}");
            return false;
        }
    }

    public void Dispose()
    {
        killSource.Cancel();
        OnMessageReceived = null;
        NamedPipe.Dispose();
    }
}

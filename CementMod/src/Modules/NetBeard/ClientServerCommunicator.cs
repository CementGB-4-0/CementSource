using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CementGB.Mod.Modules.NetBeard;

internal static class ClientServerCommunicator
{
    public static Mutex ServerMutex { get; private set; }
    public static TcpListener Server { get; private set; }
    public static TcpClient Client { get; set; }

    public delegate void MessageData(string prefix, string payload);

    public static event MessageData OnClientReceivedMessage;
    public static event MessageData OnServerReceivedMessage;

    private static ConcurrentQueue<string> queuedMessages = new();


    internal static async void Init()
    {
        UnityEngine.Application.add_quitting(new Action(() =>
        {
            ServerMutex?.Dispose();
            Server?.Stop();
            Client?.Close();
        }));

        if (ServerManager.IsServer)
        {
            ServerMutex = new Mutex(true, "Global\\GBServer", out _);

            Server = new(IPAddress.Loopback, 5999);
            Server.Start();

            while (true)
            {
                TcpClient client = await Server.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClient(client));
            }
        }

        else
        {
            _ = Task.Run(ClientLoop);
        }
    }

    internal static void QueueMessage(string prefix, string payload) => queuedMessages.Enqueue($"{prefix};{payload}");

    private static async void ClientLoop()
    {
        while (true)
        {
            if (!IsServerRunning())
            {
                // Server has either just stopped or wasn't running to begin with
                // Connection impossible, terminate an existing client if there is one
                // And keep checking for if it comes alive
                Client?.Dispose();
                continue;
            }

            if (Client == null) Client = new("127.0.0.1", 5999);
            if (!Client.Connected)
            {
                try
                {
                    await Client.ConnectAsync("127.0.0.1", 5999);
                }

                catch (Exception ex)
                {
                    Mod.Logger.Error($"Exception when trying to speak to modded server \n{ex}");
                    await Task.Delay(1000);
                    continue; // Redo connection, as we cannot do anything without it
                }
            }

            bool connectionAlive = await HandleStream(false, Client);

            if (!connectionAlive)
            {
                // Dispose of it so the loop creates a new one and reconnects if possible
                Client.Close();
            }
        }
    }

    private static async void HandleClient(TcpClient client)
    {
        while (true)
        {
            bool shouldContinue = await HandleStream(true, client);

            if (!shouldContinue)
            {
                // Client connection is dead, close the task and stop doing useless stuff
                client.Close();
                return;
            }
        }
    }

    private static async Task<bool> HandleStream(bool isServer, TcpClient client)
    {
        using NetworkStream clientStream = client.GetStream();
        if (clientStream == null) return false;

        using StreamWriter streamWriter = new StreamWriter(clientStream) { AutoFlush = true };
        using StreamReader streamReader = new StreamReader(clientStream);

        try
        {
            Task sendTask = Task.Run(async () =>
            {
                while (client.Connected)
                {
                    while (queuedMessages.TryDequeue(out string msg))
                        await streamWriter.WriteLineAsync(msg);

                    await Task.Delay(10);
                }
            });

            while (true)
            {
                string nextMessage = await streamReader.ReadLineAsync();

                // Surprisingly enough, this actually means the client was shut down
                // ReadLineAsync yields progression of the method until a message can be read or received
                if (nextMessage == null) break;

                string[] messageContents = DataFromMessage(nextMessage);

                if (!string.IsNullOrWhiteSpace(messageContents[0]) && !string.IsNullOrWhiteSpace(messageContents[1]))
                {
                    if (isServer) OnServerReceivedMessage?.Invoke(messageContents[0], messageContents[1]);
                    else OnClientReceivedMessage?.Invoke(messageContents[0], messageContents[1]);
                }
            }
        }

        catch (Exception ex)
        {
            // Connection either died or some unknown exception occured
            Mod.Logger.Error($"Stream handling error {ex}");
            return false;
        }
        return true;
    }

    private static string[] DataFromMessage(string message)
    {
        int split = message.IndexOf(';');
        if (split == -1) return ["", ""];
        return [message[..split], message[(split + 1)..]];

    }

    internal static bool IsServerRunning()
    {
        try
        {
            using Mutex foundMutex = Mutex.OpenExisting("Global\\GBServer");
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}


internal class LineStepper
{
    private int currentLine = 0;
    private string toLogOnStep;
    private ConsoleColor logColor;


    internal LineStepper(string log, ConsoleColor color)
    {
        toLogOnStep = log;
        logColor = color;
    }

    internal void Step(string identifier = "")
    {
        currentLine++;
        Mod.Logger.Msg(logColor, string.Format(toLogOnStep, currentLine) + " " + identifier);
        Thread.Sleep(1000);
    }

    internal void Reset()
    {
        Mod.Logger.Msg("LineStepper: Reset");
        currentLine = 0;
    }
}
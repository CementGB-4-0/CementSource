using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using MelonLoader;
using UnityEngine;

namespace CementGB.Modules.NetBeard;

internal static class ClientServerCommunicator
{
    
    public delegate void MessageData(string prefix, string payload);

    private static MelonLogger.Instance? Logger => InstancedCementModule.GetModule<ServerManager>()?.Logger;
    private static readonly ConcurrentQueue<string> queuedMessages = new();
    private static bool hasHookedQuit;
    private static Task currentClientLoop;
    public static Mutex ServerMutex { get; private set; }
    public static TcpListener Server { get; private set; }
    public static TcpClient Client { get; set; }

    public static event MessageData OnClientReceivedMessage;
    public static event MessageData OnServerReceivedMessage;


    internal static async void Init()
    {
        if (!hasHookedQuit)
        {
            Application.add_quitting(new Action(() =>
            {
                ServerMutex?.Dispose();
                Server?.Stop();
                Client?.Close();
            }));

            hasHookedQuit = true;
        }

        if (ServerManager.IsServer)
        {
            ServerMutex = new Mutex(true, "Global\\GBServer", out _);

            Server = new TcpListener(IPAddress.Loopback, ServerManager.DefaultPort);
            Server.Start();

            while (true)
            {
                var client = await Server.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClient(client));
            }
        }

        if (currentClientLoop == null || currentClientLoop?.IsCompleted == true)
        {
            currentClientLoop = Task.Run(ClientLoop);
        }
    }

    internal static void QueueMessage(string prefix, string payload)
    {
        queuedMessages.Enqueue($"{prefix};{payload}");
    }

    private static async void ClientLoop()
    {
        while (true)
        {
            if (!IsServerRunning())
            {
                // Server has either just stopped or wasn't running to begin with
                // Connection impossible, terminate an existing client if there is one
                Client?.Dispose();
                break; // If no modded server was detected what's the point in constantly looking? Just check when we're readying up
            }

            if (Client == null) Client = new TcpClient("127.0.0.1", ServerManager.DefaultPort);
            if (!Client.Connected)
            {
                try
                {
                    await Client.ConnectAsync("127.0.0.1", ServerManager.DefaultPort);
                }

                catch (Exception ex)
                {
                    Logger?.Error($"Exception when trying to speak to modded server \n{ex}");
                    await Task.Delay(1000);
                    continue; // Redo connection, as we cannot do anything without it
                }
            }

            var connectionAlive = await HandleStream(false, Client);

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
            var shouldContinue = await HandleStream(true, client);

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
        using var clientStream = client.GetStream();
        if (clientStream == null) return false;

        using var streamWriter = new StreamWriter(clientStream) { AutoFlush = true };
        using var streamReader = new StreamReader(clientStream);

        try
        {
            var sendTask = Task.Run(async () =>
            {
                while (client.Connected)
                {
                    while (queuedMessages.TryDequeue(out var msg))
                        await streamWriter.WriteLineAsync(msg);

                    await Task.Delay(10);
                }
            });

            while (true)
            {
                var nextMessage = await streamReader.ReadLineAsync();

                // Surprisingly enough, this actually means the client was shut down
                // ReadLineAsync yields progression of the method until a message can be read or received
                if (nextMessage == null) break;

                var messageContents = DataFromMessage(nextMessage);

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
            CementGB.Mod.Logger.Error($"Stream handling error {ex}");
            return false;
        }

        return true;
    }

    private static string[] DataFromMessage(string message)
    {
        var split = message.IndexOf(';');
        if (split == -1) return ["", ""];
        return [message[..split], message[(split + 1)..]];
    }

    internal static bool IsServerRunning()
    {
        try
        {
            using var foundMutex = Mutex.OpenExisting("Global\\GBServer");
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
    private readonly ConsoleColor logColor;
    private readonly string toLogOnStep;
    private int currentLine;


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
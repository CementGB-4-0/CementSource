using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using CementGB.Mod.Utilities;
using MelonLoader;
using UnityEngine;

namespace CementGB.Mod.Modules.NetBeard;

public static class TCPCommunicator
{
    private static readonly IPAddress TCPServerIP = IPAddress.Loopback;
    private static int TCPPort => ServerManager.Port + 1;

    public delegate void MessageData(string prefix, string payload);

    private static readonly ConcurrentQueue<string> QueuedMessages = new();

    private static bool _firstInitCall = true;

    public static TcpListener? Server { get; set; }
    public static TcpClient? Client { get; set; }

    public static event MessageData? OnClientReceivedMessage;
    public static event MessageData? OnServerReceivedMessage;

    public static void QueueMessage(string prefix, string payload)
    {
        QueuedMessages.Enqueue($"{prefix};{payload}");
    }

    internal static void Init()
    {
        if (_firstInitCall)
        {
            _firstInitCall = false;
            Application.add_quitting(new Action(() =>
            {
                Server?.Stop();
                Client?.Close();
            }));

            MelonEvents.OnUpdate.Subscribe(OnUpdate);
        }

        if (!ServerManager.IsServer || Server != null) return;

        Server = new TcpListener(IPAddress.Loopback, TCPPort);
        Server.Start();
    }

    private static void OnUpdate()
    {
        _ = ServerManager.IsServer && Server != null ? Task.Run(ServerLoop) : Task.Run(ClientLoop);
    }

    private static async Task ClientLoop()
    {
        try
        {
            Client ??= new TcpClient(TCPServerIP.ToString(), TCPPort);
            await HandleStream(Client);
        }
        catch (SocketException)
        {
        }
    }

    private static async Task ServerLoop()
    {
        if (Server == null)
            return;

        try
        {
            Client ??= await Server.AcceptTcpClientAsync();
            await HandleStream(Client);
        }
        catch (SocketException e)
        {
            LoggingUtilities.VerboseLog(ConsoleColor.DarkRed, $"TCP server connection error: {e}");
        }
    }

    private static async Task HandleWriting(NetworkStream stream)
    {
        var streamWriter = new StreamWriter(stream)
        {
            AutoFlush = true
        };
        while (QueuedMessages.TryDequeue(out var msg))
        {
            await streamWriter.WriteLineAsync(msg);
        }

        await Task.Delay(10);
    }

    private static async Task HandleReading(NetworkStream stream)
    {
        var streamReader = new StreamReader(stream);
        var nextMessage = await streamReader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(nextMessage)) return;

        var messageContents = DataFromMessage(nextMessage);

        if (!string.IsNullOrWhiteSpace(messageContents[0]) && !string.IsNullOrWhiteSpace(messageContents[1]))
        {
            if (ServerManager.IsServer && Server != null) OnServerReceivedMessage?.Invoke(messageContents[0], messageContents[1]);
            else OnClientReceivedMessage?.Invoke(messageContents[0], messageContents[1]);
        }
    }

    private static Task HandleStream(TcpClient client)
    {
        var stream = client.GetStream();

        _ = HandleWriting(stream);
        _ = HandleReading(stream);
        return Task.CompletedTask;
    }

    private static string[] DataFromMessage(string message)
    {
        var split = message.IndexOf(';');
        return split == -1 ? ["", ""] : [message[..split], message[(split + 1)..]];
    }
}
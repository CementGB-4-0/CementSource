﻿using UnityEngine.Networking;
using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using CementGB.Mod.Utilities;

namespace CementGB.Mod.Modules.NetBeard;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class HandleMessageFromClient : Attribute
{
    public ushort msgCode;
    public string modId;
    public HandleMessageFromClient(string modId, ushort msgCode)
    {
        this.msgCode = msgCode;
        this.modId = modId;
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class HandleMessageFromServer : Attribute
{
    public ushort msgCode;
    public string modId;
    public HandleMessageFromServer(string modId, ushort msgCode)
    {
        this.modId = modId;
        this.msgCode = msgCode;
    }
}


[RegisterTypeInIl2Cpp]
public class NetBeard : MonoBehaviour
{

    private static bool madeFromServer = false;
    private static bool madeFromClient = false;
    private static readonly List<ushort> fromClientHandlers = new List<ushort>();
    private static readonly List<ushort> fromServerHandlers = new List<ushort>();

    private static readonly Dictionary<string, Type> serverModIds = new();
    private static readonly Dictionary<string, Type> clientModIds = new();

    private static readonly Dictionary<string, ushort> serverModOffsets = new();
    private static readonly Dictionary<string, ushort> clientModOffsets = new();

    private static bool calculatedServerOffsets = false;
    private static bool calculatedClientOffsets = false;

    private void Update()
    {
        if (!madeFromServer && NetworkServer.active)
        {
            CalculateServerOffsets();
            InitFromClientHandlers();
            madeFromServer = true;
        }
        else if (!NetworkServer.active)
        {
            madeFromServer = false;
        }

        if (!madeFromClient && NetworkClient.active)
        {
            CalculateClientOffsets();
            InitFromServerHandlers();
            madeFromClient = true;
        }
        else if (!NetworkClient.active)
        {
            madeFromClient = false;
        }
    }

    public static ushort GetServerOffset(string modId)
    {
        return serverModOffsets[modId];
    }

    public static ushort GetClientOffset(string modId)
    {
        return clientModOffsets[modId];
    }

    // register your message codes
    public static void RegisterServerCodes(string modId, Type codes)
    {
        clientModIds[modId] = codes;
    }

    public static void RegisterClientCodes(string modId, Type codes)
    {
        serverModIds[modId] = codes;
    }

    private static void CalculateServerOffsets()
    {
        if (calculatedServerOffsets) return;

        string[] keys = serverModIds.Keys.ToArray();
        Array.Sort(keys);
        foreach (string key in keys)
        {
            CalculateServerOffsetsForModId(key, serverModIds[key]);
        }

        calculatedServerOffsets = true;
    }

    private static void CalculateClientOffsets()
    {
        if (calculatedClientOffsets) return;

        string[] keys = clientModIds.Keys.ToArray();
        Array.Sort(keys);
        foreach (string key in keys)
        {
            CalculateClientOffsetsForModId(key, clientModIds[key]);
        }

        calculatedClientOffsets = true;
    }

    private static ushort previousServerOffset = 3000;
    private static void CalculateServerOffsetsForModId(string modId, Type codes)
    {
        ushort max = 0;
        foreach (ushort val in Enum.GetValues(codes))
        {
            max = Math.Max(val, max);
        }
        serverModOffsets[modId] = previousServerOffset;
        previousServerOffset += max;
    }

    private static ushort previousClientOffset = 3000;
    private static void CalculateClientOffsetsForModId(string modId, Type codes)
    {
        ushort max = 0;
        foreach (ushort val in Enum.GetValues(codes))
        {
            max = Math.Max(val, max);
        }
        clientModOffsets[modId] = previousClientOffset;
        previousClientOffset += max;
    }

    private static bool IsValidMethod(MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        if (parameters.Length != 1) return false;
        return parameters[0].ParameterType == typeof(NetworkMessage) && method.IsStatic;
    }

    public static void InitFromServerHandlers()
    {
        // TODO: This does not work for every assembly registered. Use MelonAssembly.LoadedAssemblies for that
        Assembly assembly = Assembly.GetExecutingAssembly();
        foreach (Type type in assembly.GetTypes())
        {
            foreach (MethodInfo method in type.GetMethods())
            {
                HandleMessageFromServer? attribute = (HandleMessageFromServer?)Attribute.GetCustomAttribute(method, typeof(HandleMessageFromServer));
                if (attribute != null)
                {
                    if (IsValidMethod(method))
                    {
                        ushort code = (ushort)(attribute.msgCode + clientModOffsets[attribute.modId]);
                        fromServerHandlers.Add(code);
                        NetworkManager.singleton.client.RegisterHandler((short)code, (NetworkMessageDelegate)delegate (NetworkMessage message)
                        {
                            method.Invoke(null, new object[] { message });
                        });
                        LoggingUtilities.VerboseLog($"Registered handler for '{method.Name}'");
                    }
                    else
                    {
                        LoggingUtilities.VerboseLog($"Invalid message handler '{method.Name}'. Message handlers should only take in one argument of type 'NetworkMessage'");
                    }
                }
            }
        }
        LoggingUtilities.VerboseLog("Initialised from server handlers!");
    }

    public static void InitFromClientHandlers()
    {
        // TODO: This does not work for every assembly registered. Use MelonAssembly.LoadedAssemblies for that
        Assembly assembly = Assembly.GetExecutingAssembly();
        foreach (Type type in assembly.GetTypes())
        {
            foreach (MethodInfo method in type.GetMethods())
            {
                HandleMessageFromClient? attribute = (HandleMessageFromClient?)Attribute.GetCustomAttribute(method, typeof(HandleMessageFromClient));
                if (attribute != null)
                {
                    if (IsValidMethod(method))
                    {
                        ushort code = (ushort)(attribute.msgCode + serverModOffsets[attribute.modId]);
                        fromClientHandlers.Add(code);
                        NetworkServer.RegisterHandler((short)code, (NetworkMessageDelegate)delegate (NetworkMessage message)
                        {
                            method.Invoke(null, new object[] { message });
                        });
                        LoggingUtilities.VerboseLog($"Registered handler for '{method.Name}'");
                    }
                    else
                    {
                        LoggingUtilities.VerboseLog($"Invalid message handler '{method.Name}'. Message handlers should only take in one argument of type 'NetworkMessage'");
                    }
                }
            }
        }
        LoggingUtilities.VerboseLog("Initialised from client handlers!");
    }

    public static void SendToServer(string modId, ushort msgCode, MessageBase message)
    {
        if (!NetworkClient.active)
        {
            LoggingUtilities.Logger.Error("Couldn't send a message to the server, because NetworkClient is not active.");
            return;
        }
        NetworkWriter writer = new NetworkWriter();
        writer.StartMessage((short)(msgCode + clientModOffsets[modId]));
        message.Serialize(writer);
        writer.FinishMessage();
        NetworkManager.singleton.client.SendWriter(writer, 0);
    }

    public static void SendToClient(NetworkConnection conn, ushort msgCode, MessageBase message)
    {
        if (!NetworkServer.active)
        {
            LoggingUtilities.Logger.Error("Couldn't send the message to client, because NetworkServer is not active.");
            return;
        }
        NetworkWriter writer = new NetworkWriter();
        writer.StartMessage((short)msgCode);
        message.Serialize(writer);
        writer.FinishMessage();
        conn.SendWriter(writer, 0);
    }

    public static void SendWriterToClient(NetworkConnection conn, NetworkWriter writer)
    {
        if (!NetworkServer.active)
        {
            LoggingUtilities.Logger.Error("Couldn't send writer to client, because NetworkServer is not active.");
            return;
        }
        conn.SendWriter(writer, 0);
    }

    public static void SendToAllClients(string modId, ushort msgCode, MessageBase message, bool includeSelf = true)
    {
        if (!NetworkServer.active)
        {
            LoggingUtilities.Logger.Error("Couldn't send message to clients, because NetworkServer is not active.");
            return;
        }
        NetworkWriter writer = new NetworkWriter();
        writer.StartMessage((short)(msgCode + serverModOffsets[modId]));
        message.Serialize(writer);
        writer.FinishMessage();
        for (int i = includeSelf ? 0 : 1; i < NetworkServer.connections.Count; ++i)
        {
            if (NetworkServer.connections[i] == null)
            {
                LoggingUtilities.Logger.Warning("Null connection while sending to all clients. Skipping. This is probably normal.");
                continue;
            }
            SendWriterToClient(NetworkServer.connections[i], writer);
        }
    }

    public static void ReinitFromServerHandlers()
    {
        foreach (ushort code in fromServerHandlers)
        {
            NetworkServer.UnregisterHandler((short)code);
        }
        fromServerHandlers.Clear();
        madeFromServer = false;
    }

    public static void ReinitFromClientHandlers()
    {
        foreach (ushort code in fromServerHandlers)
        {
            NetworkManager.singleton.client.UnregisterHandler((short)code);
        }
        fromClientHandlers.Clear();
        madeFromClient = false;
    }
}

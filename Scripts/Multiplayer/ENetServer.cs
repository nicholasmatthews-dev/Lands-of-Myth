using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System;
using Godot;
using System.Reflection.Metadata;
using System.Diagnostics;
using System.Text;

namespace LOM.Multiplayer;

public class ENetServer {
    private readonly ENetConnection connection = new();
    private Thread connectionThread;
    private ConcurrentDictionary<int, HashSet<WeakReference<ENetPacketListener>>> packetListeners = new();

    public ENetServer(int port){
        Error error = connection.CreateHostBound("*", port, maxPeers : 32, maxChannels : ENetCommon.channels);
        Debug.Print("ENetServer: Error from creating server is " + error);
        connection.Compress(ENetCommon.compressionMode);
        connectionThread = new(Process)
        {
            IsBackground = false
        };
        connectionThread.Start();
    }

    private void Process(){
        int cycles = 0;
        while (cycles < 100){
            Godot.Collections.Array results = connection.Service(1000);
            HandleResults(results);
            cycles++;
        }
    }

    private void HandleResults(Godot.Collections.Array results){
        //Debug.Print("ENetServer: Handling results " + results);
        ENetConnection.EventType eventType = results[0].As<ENetConnection.EventType>();
        ENetPacketPeer peer = results[1].As<ENetPacketPeer>();
        if (eventType == ENetConnection.EventType.Connect){
            if (peer is not null){
                peer.Send(1, Encoding.ASCII.GetBytes("Hello world!"), (int)ENetPacketPeer.FlagReliable);
            }
        }
    }
}
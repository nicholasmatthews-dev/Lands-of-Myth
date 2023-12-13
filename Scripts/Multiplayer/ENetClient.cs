using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Godot;

namespace LOM.Multiplayer;

public class ENetClient {
    private readonly ENetConnection connection = new();
    private Thread connectionThread;
    private ConcurrentDictionary<int, HashSet<WeakReference<ENetPacketListener>>> packetListeners = new();
    ENetPacketPeer peerSelf;
    public ENetClient(string address, int port){
        connection.CreateHost(maxPeers : 32, maxChannels : ENetCommon.channels);
        peerSelf = connection.ConnectToHost(address, port, ENetCommon.channels);
        connection.Compress(ENetCommon.compressionMode);
        connectionThread = new(Process){
            IsBackground = false
        };
        connectionThread.Start();
    }

    private void Process(){
        int cycles = 0;
        while(cycles < 100){
            //Debug.Print("ENetClient: Peerstate is: " + peerSelf.GetState().ToString());
            Godot.Collections.Array results = connection.Service(1000);
            HandleResults(results);
            cycles++;
        }
    }

    private void HandleResults(Godot.Collections.Array results){
        Debug.Print("ENetClient: Handling results: " + results);
        ENetConnection.EventType eventType = results[0].As<ENetConnection.EventType>();
        //Debug.Print("ENetClient: Event type is " + eventType.ToString("D"));
        ENetPacketPeer peer = results[1].As<ENetPacketPeer>();
        int channel = results[3].As<int>();
        if (eventType == ENetConnection.EventType.Receive){
            //BroadCastToListeners(channel, peer);
            byte[] packet = peer.GetPacket();
            Debug.Print("ENetClient: Message received \"" + Encoding.ASCII.GetString(packet) + "\"");
        }
    }

    private void BroadCastToListeners(int channel, ENetPacketPeer peer){
        if (!packetListeners.ContainsKey(channel)){
            throw new ArgumentException(
                "ENetClient: Channel " + channel + " does not correspond to a valid set of listeners."
            );
        }
        byte[] packet = peer.GetPacket();
        List<WeakReference<ENetPacketListener>> deadReferences = new();
        foreach (WeakReference<ENetPacketListener> reference in packetListeners[channel]){
            if (reference.TryGetTarget(out ENetPacketListener listener)){
                listener.ReceivePacket(packet, peer);
            }
            else{
                deadReferences.Add(reference);
            }
        }
        foreach(WeakReference<ENetPacketListener> reference in deadReferences){
            packetListeners[channel].Remove(reference);
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Godot;

namespace LOM.Multiplayer;

public partial class ENetClient : RefCounted {
    private bool _Disposed = false;
    private ENetConnection connection = new();
    private Thread connectionThread;
    private ConcurrentDictionary<int, HashSet<WeakReference<ENetPacketListener>>> packetListeners = new();
    ENetPacketPeer peerSelf;
    private int timeOutMillis = 250;
    private bool keepAlive = true;
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
        while(keepAlive){
            try{
                Godot.Collections.Array results = connection.Service(timeOutMillis);
                HandleResults(results);
            }
            catch (Exception e){
                Debug.Print("ENetClient: Error in process: " + e.Message);
                break;
            }
        }
        Debug.Print("ENetClient: Thread process exiting.");
    }

    private void HandleResults(Godot.Collections.Array results){
        ENetConnection.EventType eventType = results[0].As<ENetConnection.EventType>();
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
            return;
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

    public void AddPacketListener(int channel, ENetPacketListener listener){
        WeakReference<ENetPacketListener> reference = new(listener);
        if (!packetListeners.ContainsKey(channel)){
            HashSet<WeakReference<ENetPacketListener>> listenerSet = new()
            {
                reference
            };
            packetListeners.TryAdd(channel, listenerSet);
        }
        else{
            packetListeners[channel].Add(reference);
        }
    }

    protected override void Dispose(bool disposing){
        if (_Disposed){
            return;
        }
        if (disposing){
            Debug.Print("ENetClient: Disposing of this ENetClient.");
            keepAlive = false;
            connectionThread.Join();
            connection.Destroy();
            connection = null;
            _Disposed = true;
            base.Dispose(disposing);
        }
        
    }
}
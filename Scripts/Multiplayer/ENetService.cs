using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Godot;

namespace LOM.Multiplayer;

public abstract partial class ENetService : RefCounted {
    protected bool _Disposed = false;
    protected ENetConnection connection = new();
    protected Thread connectionThread;
    protected ConcurrentDictionary<int, HashSet<WeakReference<ENetPacketListener>>> packetListeners = new();
    protected int timeOutMillis = 250;
    protected bool keepAlive = true;

    protected void Start(){
        connection.Compress(ENetCommon.compressionMode);
        connectionThread = new(Process)
        {
            IsBackground = false
        };
        connectionThread.Start();
    }

    private void Process(){
        while (keepAlive){
            try{
                Godot.Collections.Array results = connection.Service(timeOutMillis);
                HandleResults(results);
            }
            catch (Exception e){
                Debug.Print(GetType() + ": Error in process " + e.Message);
                break;
            }
        }
        Debug.Print(GetType() + ": Thread process exiting.");
    }

    protected void BroadCastToListeners(int channel, ENetPacketPeer peer){
        byte[] packet = peer.GetPacket();
        if (!packetListeners.ContainsKey(channel)){
            Debug.Print(GetType() + ": No listener on channel " + channel);
            return;
        }
        List<WeakReference<ENetPacketListener>> deadReferences = new();
        foreach (WeakReference<ENetPacketListener> reference in packetListeners[channel]){
            if (reference.TryGetTarget(out ENetPacketListener listener)){
                Debug.Print(GetType() + ": Sending packet of length " + packet.Length + " to listener.");
                listener.ReceivePacket(packet, peer);
            }
            else{
                Debug.Print(GetType() + ": Found dead listener.");
                deadReferences.Add(reference);
            }
        }
        foreach(WeakReference<ENetPacketListener> reference in deadReferences){
            packetListeners[channel].Remove(reference);
        }
    }

    public void AddPacketListener(int channel, ENetPacketListener listener){
        Debug.Print(GetType() + ": Attempting to add listener on channel " + channel);
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

    protected abstract void HandleResults(Godot.Collections.Array results);

    protected override void Dispose(bool disposing){
        if (_Disposed){
            return;
        }
        if (disposing){
            Debug.Print(GetType() + ": Disposing of this instance.");
            keepAlive = false;
            connectionThread.Join();
            try {
                connection.Destroy();
            }
            catch (Exception e){
                Debug.Print(GetType() + ": Couldn't destroy connection error \"" + e.Message + "\"");
            }
            connection = null;
            _Disposed = true;
            base.Dispose(disposing);
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Godot;

namespace LOM.Multiplayer;

/// <summary>
/// Represents a class which will use and service an <c>ENetConnection</c>.
/// </summary>
public abstract partial class ENetService : RefCounted {
    private static bool Debugging = false;
    /// <summary>
    /// Whether or not this object has already been disposed of.
    /// </summary>
    protected bool _Disposed = false;
    /// <summary>
    /// The underlying connection that this <c>ENetService</c> will use.
    /// </summary>
    protected ENetConnection connection = new();
    /// <summary>
    /// The thread used to service the <c>ENetConnection</c>.
    /// </summary>
    protected Thread connectionThread;
    /// <summary>
    /// The <c>ENetPacketListeners</c> which will use this service.
    /// </summary>
    protected ConcurrentDictionary<int, HashSet<WeakReference<IENetPacketListener>>> packetListeners = new();
    /// <summary>
    /// A lock for accessing <see cref="connectionListeners"/> 
    /// </summary>
    protected object connectionListenersLock = new();
    /// <summary>
    /// A collection of all the <see cref="IENetConnectionListener"/>s that this service has subscribed. 
    /// </summary>
    protected HashSet<WeakReference<IENetConnectionListener>> connectionListeners = new();
    /// <summary>
    /// How long service calls will wait before timing out, in milliseconds.
    /// </summary>
    protected int timeOutMillis = 250;
    /// <summary>
    /// Whether or not to keep the connectionThread alive.
    /// </summary>
    protected bool keepAlive = true;

    /// <summary>
    /// Sets the compression mode for communication and initializes the service thread.
    /// <para>
    /// NOTE: The <c>ENetConnection</c> connection must be established on the implementing class
    /// before this method is called. This method should therefore likely be called at the end of the
    /// constructor in the implementing class.
    /// </para>
    /// </summary>
    protected void Start(){
        connection.Compress(ENetCommon.compressionMode);
        connectionThread = new(Process)
        {
            IsBackground = false
        };
        connectionThread.Start();
    }

    /// <summary>
    /// The method that represents the body of the process thread. This will repeatedly service the
    /// <c>ENetConnection</c> until the thread is terminated. The results of each service call will then
    /// be passed into <c>HandleResults</c>.
    /// </summary>
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
        if (Debugging) Debug.Print(GetType() + ": Thread process exiting.");
    }

    /// <summary>
    /// Broadcasts a received message to all available <see cref="IENetPacketListener"/>s.
    /// </summary>
    /// <param name="channel">The channel to broadcast on.</param>
    /// <param name="peer">The peer which sent the received message.</param>
    protected void BroadCastToListeners(int channel, ENetPacketPeer peer){
        byte[] packet = peer.GetPacket();
        if (!packetListeners.ContainsKey(channel)){
            if (Debugging) Debug.Print(GetType() + ": No listener on channel " + channel);
            return;
        }
        List<WeakReference<IENetPacketListener>> deadReferences = new();
        foreach (WeakReference<IENetPacketListener> reference in packetListeners[channel]){
            if (reference.TryGetTarget(out IENetPacketListener listener)){
                if (Debugging) Debug.Print(GetType() + ": Sending packet of length " + packet.Length + " to listener.");
                listener.ReceivePacket(packet, peer);
            }
            else{
                if (Debugging) Debug.Print(GetType() + ": Found dead listener.");
                deadReferences.Add(reference);
            }
        }
        foreach(WeakReference<IENetPacketListener> reference in deadReferences){
            packetListeners[channel].Remove(reference);
        }
    }

    /// <summary>
    /// Broadcasts a connection event to all <see cref="IENetConnectionListener"/>s. 
    /// </summary>
    /// <param name="connectedPeer">The peer which connected.</param>
    protected void BroadCastConnection(ENetPacketPeer connectedPeer){
        if (Debugging) Debug.Print(GetType() + ": Broadcasting connection event to listeners.");
        List<WeakReference<IENetConnectionListener>> deadReferences = new();
        lock (connectionListenersLock){
            foreach (WeakReference<IENetConnectionListener> reference in connectionListeners){
                if (reference.TryGetTarget(out IENetConnectionListener listener)){
                    listener.HandleConnection(connectedPeer);
                }
                else {
                    if (Debugging) Debug.Print(GetType() + ": Found dead connection listener.");
                    deadReferences.Add(reference);
                }
            }
            foreach (WeakReference<IENetConnectionListener> deadReference in deadReferences){
                connectionListeners.Remove(deadReference);
            }
        }
    }

    /// <summary>
    /// Adds a new <see cref="IENetPacketListener"/> on the given channel.
    /// <para>
    /// NOTE: Listeners are stored as <see cref="WeakReference"/>s and as such this object
    /// cannot be relied on to keep them alive.
    /// </summary>
    /// <param name="channel">The channel to listen on.</param>
    /// <param name="listener">The listener to be added.</param>
    public void AddPacketListener(int channel, IENetPacketListener listener){
        if (Debugging) Debug.Print(GetType() + ": Attempting to add listener on channel " + channel);
        WeakReference<IENetPacketListener> reference = new(listener);
        if (!packetListeners.ContainsKey(channel)){
            HashSet<WeakReference<IENetPacketListener>> listenerSet = new()
            {
                reference
            };
            packetListeners.TryAdd(channel, listenerSet);
        }
        else{
            packetListeners[channel].Add(reference);
        }
    }

    /// <summary>
    /// Adds a connection listener to listen to connection events.
    /// </summary>
    /// <param name="listener"></param>
    public void AddConnectionListener(IENetConnectionListener listener){
        if (Debugging) Debug.Print(GetType() + ": Attempting to add connection listener.");
        WeakReference<IENetConnectionListener> reference = new(listener);
        lock (connectionListenersLock){
            if (!connectionListeners.Contains(reference)){
                connectionListeners.Add(reference);
            }
        }
    }

    /// <summary>
    /// Handles the results from the service call on the <see cref="ENetConnection"/>. 
    /// </summary>
    /// <param name="results"></param>
    protected abstract void HandleResults(Godot.Collections.Array results);

    protected override void Dispose(bool disposing){
        if (_Disposed){
            return;
        }
        if (disposing){
            if (Debugging) Debug.Print(GetType() + ": Disposing of this instance.");
            keepAlive = false;
            connectionThread.Join();
            try {
                connection.Destroy();
            }
            catch (Exception e){
                if (Debugging) Debug.Print(GetType() + ": Couldn't destroy connection error \"" + e.Message + "\"");
            }
            connection = null;
            _Disposed = true;
            base.Dispose(disposing);
        }
    }
}
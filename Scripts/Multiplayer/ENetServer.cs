using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System;
using Godot;
using System.Reflection.Metadata;
using System.Diagnostics;
using System.Text;

namespace LOM.Multiplayer;

public partial class ENetServer : RefCounted{
    private bool _Disposed = false;
    private ENetConnection connection = new();
    private Thread connectionThread;
    private ConcurrentDictionary<int, HashSet<WeakReference<ENetPacketListener>>> packetListeners = new();
    private int timeOutMillis = 250;
    private bool keepAlive = true;

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
        while (keepAlive){
            try{
                Godot.Collections.Array results = connection.Service(timeOutMillis);
                HandleResults(results);
            }
            catch (Exception e){
                Debug.Print("ENetServer: Error in process " + e.Message);
                break;
            }
        }
        Debug.Print("ENetServer: Thread process exiting.");
    }

    private void HandleResults(Godot.Collections.Array results){
        ENetConnection.EventType eventType = results[0].As<ENetConnection.EventType>();
        ENetPacketPeer peer = results[1].As<ENetPacketPeer>();
        if (eventType == ENetConnection.EventType.Connect){
            peer?.Send(1, Encoding.ASCII.GetBytes("Hello!"), (int)ENetPacketPeer.FlagReliable);
        }
    }

    protected override void Dispose(bool disposing){
        if (_Disposed){
            return;
        }
        if (disposing){
            Debug.Print("ENetServer: Disposing of this ENetServer instance.");
            keepAlive = false;
            connectionThread.Join();
            connection.Destroy();
            connection = null;
            _Disposed = true;
            base.Dispose(disposing);
        }
        
    }
}
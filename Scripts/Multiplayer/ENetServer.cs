using Godot;
using System.Diagnostics;
using System.Text;

namespace LOM.Multiplayer;

public partial class ENetServer : ENetService{

    public ENetServer(int port) : base(){
        Error error = connection.CreateHostBound("*", port, maxPeers : 32, maxChannels : ENetCommon.channels);
        Debug.Print("ENetServer: Error from creating server is " + error);
    }

    protected override void HandleResults(Godot.Collections.Array results){
        ENetConnection.EventType eventType = results[0].As<ENetConnection.EventType>();
        ENetPacketPeer peer = results[1].As<ENetPacketPeer>();
        int channel = results[3].As<int>();
        if (peer is not null){
            Debug.Print("ENetServer: Received event with type " + eventType + " on channel " + channel + " from " + peer.GetRemoteAddress());
        }
        if (eventType == ENetConnection.EventType.Connect){
            peer?.Send(0, Encoding.ASCII.GetBytes("Hello!"), (int)ENetPacketPeer.FlagReliable);
        }
        if (eventType == ENetConnection.EventType.Receive){
            Debug.Print("ENetServer: Event type was received.");
            BroadCastToListeners(channel, peer);
        }
    }
}
using System.Diagnostics;
using System.Text;
using Godot;

namespace LOM.Multiplayer;

public partial class ENetClient : ENetService {
    public ENetClient(string address, int port) : base(){
        connection.CreateHost(maxPeers : 32, maxChannels : ENetCommon.channels);
        connection.ConnectToHost(address, port, ENetCommon.channels);
    }

    protected override void HandleResults(Godot.Collections.Array results){
        ENetConnection.EventType eventType = results[0].As<ENetConnection.EventType>();
        ENetPacketPeer peer = results[1].As<ENetPacketPeer>();
        int channel = results[3].As<int>();
        if (eventType == ENetConnection.EventType.Receive){
            //BroadCastToListeners(channel, peer);
            byte[] packet = peer.GetPacket();
            Debug.Print("ENetClient: Message received \"" + Encoding.ASCII.GetString(packet) + "\"");
        }
    }
}
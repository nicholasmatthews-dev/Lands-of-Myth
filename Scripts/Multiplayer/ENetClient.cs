using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace LOM.Multiplayer;

public partial class ENetClient : ENetService {
    ENetPacketPeer serverPeer;
    EventWaitHandle connectionWait = new(false, EventResetMode.AutoReset);
    public ENetClient(string address, int port) : base(){
        connection.CreateHost(maxPeers : 32, maxChannels : ENetCommon.channels);
        connection.ConnectToHost(address, port, ENetCommon.channels);
    }

    protected override void HandleResults(Godot.Collections.Array results){
        ENetConnection.EventType eventType = results[0].As<ENetConnection.EventType>();
        ENetPacketPeer peer = results[1].As<ENetPacketPeer>();
        int channel = results[3].As<int>();
        if (eventType == ENetConnection.EventType.Connect){
            Debug.Print("ENetClient: Connection established with " + peer.GetRemoteAddress());
            serverPeer = peer;
            connectionWait.Set();
        }
        if (eventType == ENetConnection.EventType.Receive){
            BroadCastToListeners(channel, peer);
            //byte[] packet = peer.GetPacket();
            //Debug.Print("ENetClient: Message received \"" + Encoding.ASCII.GetString(packet) + "\"");
        }
    }

    public void SendMessage(int channel, byte[] message){
        Task.Run(() => {
            connectionWait.WaitOne();
            Debug.Print("ENetClient: Sending message on channel " + channel + " to " + serverPeer.GetRemoteAddress());
            serverPeer.Send(channel, message, (int)ENetPacketPeer.FlagUnsequenced);
        });
    }
}
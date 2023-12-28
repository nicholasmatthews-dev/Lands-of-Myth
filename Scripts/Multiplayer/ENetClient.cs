using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace LOM.Multiplayer;

/// <summary>
/// Represents an ENet client connection meant to connect to an <see cref="ENetServer"/> 
/// </summary>
public partial class ENetClient : ENetService {
    private static bool Debugging = false;
    /// <summary>
    /// The peer which represents the server that this client is connected to.
    /// </summary>
    ENetPacketPeer serverPeer;
    /// <summary>
    /// A wait handle that signals whether or not a connection to the server has been
    /// established.
    /// </summary>
    EventWaitHandle connectionWait = new(false, EventResetMode.ManualReset);
    public ENetClient(string address, int port) : base(){
        connection.CreateHost(maxPeers : 32, maxChannels : ENetCommon.channels);
        connection.ConnectToHost(address, port, ENetCommon.channels);
        Start();
    }

    protected override void HandleResults(Godot.Collections.Array results){
        ENetConnection.EventType eventType = results[0].As<ENetConnection.EventType>();
        ENetPacketPeer peer = results[1].As<ENetPacketPeer>();
        int channel = results[3].As<int>();
        if (eventType == ENetConnection.EventType.Connect){
            Debug.Print("ENetClient: Connection established with " + peer.GetRemoteAddress());
            serverPeer = peer;
            BroadCastConnection(peer);
            connectionWait.Set();
        }
        if (eventType == ENetConnection.EventType.Receive){
            BroadCastToListeners(channel, peer);
        }
    }

    /// <summary>
    /// Sends a message to the connected server on the specified channel. This method will run
    /// asynchronously and wait until a connection has been established with the server before
    /// the message is sent out.
    /// </summary>
    /// <param name="channel">The channel to send the message on.</param>
    /// <param name="message">A byte array representing the message to be sent.</param>
    public void SendMessage(int channel, byte[] message){
        Task.Run(() => {
            connectionWait.WaitOne();
            if (Debugging){
                Debug.Print("ENetClient: Sending message on channel " + channel + " to " + serverPeer.GetRemoteAddress());
            }
            serverPeer.Send(channel, message, (int)ENetPacketPeer.FlagUnsequenced);
        });
    }
}
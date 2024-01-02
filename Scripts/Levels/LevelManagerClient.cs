using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using LOM.Multiplayer;

namespace LOM.Levels;

/// <summary>
/// Represents a virtual implementation of the <see cref="ILevelManager"/> for a given <see cref="Game.PlayerClient"/>. 
/// </summary>
public class LevelManagerClient : ILevelManager, IENetPacketListener
{
    private static bool Debugging = false;
    /// <summary>
    /// The <see cref="ILevelHost"/> this object retrieves <see cref="LevelCell"/>s from.  
    /// </summary>
    private ILevelHost levelHost;
    /// <summary>
    /// The server instance that this object uses.
    /// </summary>
    private ENetServer eNetServer;
    /// <summary>
    /// The channel that this object communicates on.
    /// </summary>
    private int communicationChannel = (int)ENetCommon.ChannelNames.Spaces;
    /// <summary>
    /// The <see cref="ENetPacketPeer"/> of the client that owns the <see cref="LevelManager"/> that
    /// this object is associated with.
    /// </summary>
    private ENetPacketPeer associatedPeer;

    public LevelManagerClient(ENetServer eNetServer, ENetPacketPeer associatedPeer){
        this.associatedPeer = associatedPeer;
        this.eNetServer = eNetServer;
        eNetServer.AddPacketListener(communicationChannel, this);
    }

    public void ConnectLevelHost(ILevelHost levelHost)
    {
        this.levelHost = levelHost;
        levelHost.ConnectManager(this);
    }

    public void DisconnectLevelHost(ILevelHost levelHost)
    {
        this.levelHost = null;
        levelHost.DisconnectManager(this);
    }

    /// <summary>
    /// Handles deserializing a received packet.
    /// </summary>
    /// <param name="packet">The packet to be deserialized.</param>
    /// <param name="peer">The peer which sent the packet.</param>
    private void HandlePacket(byte[] packet, ENetPacketPeer peer){
        LevelCellRequest request = LevelCellRequest.Deserialize(packet);
        if (Debugging) Debug.Print("LevelManagerClient: Received request " + request);
        RespondToRequest(request, peer);
    }

    /// <summary>
    /// Responds to a given <see cref="LevelCellRequest"/> from a given <see cref="ENetPacketPeer"/>.
    /// Will attempt to retrieve a <see cref="LevelCell"/> from <see cref="levelHost"/> and then send
    /// the fulfilled request to the given peer.
    /// </summary>
    /// <param name="request">The request to be processed.</param>
    /// <param name="peer">The peer which sent the request.</param>
    private void RespondToRequest(LevelCellRequest request, ENetPacketPeer peer){
        Task.Run(async () => {
            Task<LevelCellRequest> requestTask = levelHost.GetLevelCell(request);
            LevelCellRequest fulfilledRequest = await requestTask;
            if (Debugging) Debug.Print("LevelManagerClient: Responding to request with " + fulfilledRequest);
            byte[] packet = fulfilledRequest.Serialize();
            eNetServer.QueueMessage(communicationChannel, peer, packet);
        });
    }

    public void ReceivePacket(byte[] packet, ENetPacketPeer peer)
    {
        if (Debugging) Debug.Print("LevelManagerClient: Received packet of length " + packet.Length 
        + " from " + peer);
        if (associatedPeer.Equals(peer)){
            if (Debugging) Debug.Print("LevelManagerClient: Received request from appropriate peer.");
            HandlePacket(packet, peer);
        }
    }
}
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using LOM.Levels;
using LOM.Multiplayer;

namespace LOM.Spaces;

/// <summary>
/// Represents a <c>WorldSpace</c> which can be accessed by matching <see cref="WorldSpaceClient"/>s.
/// </summary>
public class WorldSpaceServer : ENetPacketListener
{
    private static bool Debugging = false;
    /// <summary>
    /// The server that this object uses to communicate over.
    /// </summary>
    private ENetServer eNetServer;
    /// <summary>
    /// The currently active space that this WorldSpaceServer is using.
    /// TODO: Expand to a collection to service multiple clients in different spaces.
    /// </summary>
    private WorldSpace activeSpace;
    /// <summary>
    /// The ENet channel that this object communicates over.
    /// </summary>
    private int communicationChannel = (int)ENetCommon.ChannelNames.Spaces;
    private TileSetManager tileSetManager;

    public WorldSpaceServer(ENetServer server, TileSetManager tileSetManager){
        this.tileSetManager = tileSetManager;
        activeSpace = new(tileSetManager);
        eNetServer = server;
        eNetServer.AddPacketListener(communicationChannel,this);
    }

    public void ReceivePacket(byte[] packet, ENetPacketPeer peer)
    {
        if (Debugging) Debug.Print("WorldSpaceServer: Packet received.");
        DecodePacket(packet, peer);
    }

    /// <summary>
    /// Attempts to decode the packet as a world request and then respond to that request.
    /// </summary>
    /// <param name="packet">A byte array representing the packet to be decoded.</param>
    /// <param name="peer">The peer which sent the packet.</param>
    private void DecodePacket(byte[] packet, ENetPacketPeer peer){
        try {
            WorldCellRequest request = WorldCellRequest.Deserialize(packet);
            if (Debugging) Debug.Print("WorldSpaceServer: Request received: " + request);
            RespondToRequest(request, peer);
        }
        catch (Exception e){
            Debug.Print("WorldSpaceServer: Error in deserializing packet: " + e.Message);
        }
    }

    /// <summary>
    /// Responds to a given WorldCellRequest by retrieving a LevelCell from the active world space
    /// and then sending a completed WorldCellRequest to the requesting peer.
    /// </summary>
    /// <param name="request">The WorldCellRequest to fulfill.</param>
    /// <param name="peer">The peer which sent the WorldCellRequest.</param>
    private void RespondToRequest(WorldCellRequest request, ENetPacketPeer peer){
        Task.Run(async () => {
                Task<LevelCell> levelCell = activeSpace.GetLevelCell(request.coords);
                await levelCell;
                request.payload = levelCell.Result.Serialize();
                request.status = WorldCellRequest.RequestStatus.Fulfilled;
                byte[] packet = request.Serialize();
                if (Debugging) Debug.Print("WorldSpaceServer: Responding to request: " + request);
                if (Debugging) Debug.Print("WorldSpaceServer: Packet size is " + packet.Length);
                peer.Send(communicationChannel, packet, (int)ENetPacketPeer.FlagReliable);
        });
    }
}
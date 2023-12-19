using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using LOM.Levels;
using LOM.Multiplayer;

namespace LOM.Spaces;

/// <summary>
/// Represents a <c>WorldSpace</c> via a connection to a <see cref="WorldSpaceServer"/>.
/// </summary>
public partial class WorldSpaceClient : Space, ENetPacketListener {
    private static bool Debugging = false;
    private bool _Disposed = false;
    /// <summary>
    /// The name of the space that this WorldSpaceClient represents.
    /// </summary>
    private string spaceName = "Overworld";
    /// <summary>
    /// The client that this object will use for communication.
    /// </summary>
    private ENetClient netClient;
    /// <summary>
    /// A dictionary encapsulating all the active requests for cells that this object has
    /// sent out. Each entry is indexed by its cell coordinates and contains a wait handle
    /// which can be waited on until the request is fulfilled, and a worldcellrequest which
    /// holds information about the request and will contain the payload when the request is
    /// fulfilled.
    /// </summary>
    private ConcurrentDictionary<CellPosition, (EventWaitHandle, WorldCellRequest)> requestHandles = new();
    /// <summary>
    /// The ENetChannel that this object communicates with the server on.
    /// </summary>
    private int communicationChannel = (int)ENetCommon.ChannelNames.Spaces;
    private TileSetManager tileSetManager;
    public WorldSpaceClient(ENetClient eNetClient, TileSetManager tileSetManager) : base(){
        this.tileSetManager = tileSetManager;
        netClient = eNetClient;
        netClient.AddPacketListener(communicationChannel, this);
    }

    public Task<LevelCell> GetLevelCell(CellPosition cellCoords)
    {
        return CreateRetrievalTask(cellCoords);
    }

    /// <summary>
    /// Creates a task to retrieve a <c>LevelCell</c> at the given coordinates. This will
    /// send a <c>WorldCellRequest</c> to the server and then await a response. The 
    /// <c>LevelCell</c> contained in the response will then be returned.
    /// </summary>
    /// <param name="cellCoords">The coordinates of the <c>LevelCell</c> to retrieve.</param>
    /// <returns>A task which will retrieve the <c>LevelCell</c> at the given coordinates.</returns>
    private Task<LevelCell> CreateRetrievalTask(CellPosition cellCoords){
        return Task.Run(() => {
            WorldCellRequest request = new WorldCellRequest(spaceName, cellCoords);
            EventWaitHandle waitHandle = new(false, EventResetMode.AutoReset);
            requestHandles.TryAdd(cellCoords, (waitHandle, request));
            if (Debugging){
                Debug.Print("WorldSpaceClient: Sending request to server: " + request);
            }
            netClient.SendMessage(communicationChannel, request.Serialize());
            waitHandle.WaitOne();
            if (Debugging){
                Debug.Print("WorldSpaceClient: Returning from wait with results?");
            }
            requestHandles.TryGetValue(cellCoords, out (EventWaitHandle, WorldCellRequest) updatedEntry);
            if (Debugging){
                Debug.Print("WorldSpaceClient: New request is: " + updatedEntry.Item2);
            }
            LevelCell output = LevelCell.Deserialize(updatedEntry.Item2.payload, tileSetManager);
            requestHandles.TryRemove(cellCoords, out updatedEntry);
            return output;
        });
    }

    public void ReceivePacket(byte[] packet, ENetPacketPeer peer)
    {
        if (Debugging) Debug.Print("WorldSpaceClient: Packet received.");
        WorldCellRequest request;
        try {
            request = WorldCellRequest.Deserialize(packet);
        }
        catch (Exception e){
            Debug.Print("WorldSpaceClient: Error in deserializing packet: " + e.Message);
            return;
        }
        if (Debugging) Debug.Print("WorldSpaceClient: Received request: " + request);
        CellPosition coords = request.coords;
        (EventWaitHandle, WorldCellRequest) entry;
        if (requestHandles.TryGetValue(coords, out entry)){
            if (requestHandles.TryUpdate(coords, (entry.Item1, request), entry)){
                entry.Item1.Set();
            }
        }
    }

    public void StoreBytesToCell(byte[] cellToStore, CellPosition cellCoords)
    {
        return;
    }
}
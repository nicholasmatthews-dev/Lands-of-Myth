using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using LOM.Levels;
using LOM.Multiplayer;

namespace LOM.Spaces;

public class WorldSpaceClient : Space, ENetPacketListener {
    private string spaceName = "Overworld";
    private ENetClient netClient;
    private ConcurrentDictionary<Vector2I, (EventWaitHandle, WorldCellRequest)> requestHandles = new();
    private int communicationChannel = (int)ENetCommon.ChannelNames.Spaces;
    public WorldSpaceClient(ENetClient eNetClient) : base(){
        netClient = eNetClient;
        netClient.AddPacketListener(communicationChannel, this);
    }

    public override Task<LevelCell> GetLevelCell(Vector2I cellCoords)
    {
        return new Task<LevelCell>(() => {
            WorldCellRequest request = new WorldCellRequest(spaceName, cellCoords);
            EventWaitHandle waitHandle = new(false, EventResetMode.AutoReset);
            requestHandles.TryAdd(cellCoords, (waitHandle, request));
            Debug.Print("WorldSpaceClient: Sending request to server.");
            netClient.SendMessage(communicationChannel, request.Serialize());
            waitHandle.WaitOne();
            Debug.Print("WorldSpaceClient: Returning from wait with results?");
            (EventWaitHandle, WorldCellRequest) updatedEntry;
            requestHandles.TryGetValue(cellCoords, out updatedEntry);
            LevelCell output = LevelCell.Deserialize(updatedEntry.Item2.payload);
            requestHandles.TryRemove(cellCoords, out updatedEntry);
            return output;
        });
    }

    public void ReceivePacket(byte[] packet, ENetPacketPeer peer)
    {
        Debug.Print("WorldSpaceClient: Packet received.");
        try {
            WorldCellRequest request = WorldCellRequest.Deserialize(packet);
            Debug.Print("WorldSpaceClient: Received request: " + request);
            Vector2I coords = request.coords;
            (EventWaitHandle, WorldCellRequest) entry;
            if (requestHandles.TryGetValue(coords, out entry)){
                if (requestHandles.TryUpdate(coords, (entry.Item1, request), entry)){
                    entry.Item1.Set();
                }
            }
        }
        catch (Exception e){
            Debug.Print("WorldSpaceClient: Error in deserializing packet: " + e.Message);
        }
    }

    public override void StoreBytesToCell(byte[] cellToStore, Vector2I cellCoords)
    {
        return;
    }
}
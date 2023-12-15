using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using LOM.Levels;
using LOM.Multiplayer;

namespace LOM.Spaces;

public class WorldSpaceServer : ENetPacketListener
{
    private ENetServer eNetServer;
    private WorldSpace activeSpace = new();
    private int communicationChannel = (int)ENetCommon.ChannelNames.Spaces;

    public WorldSpaceServer(ENetServer server){
        eNetServer = server;
        eNetServer.AddPacketListener(communicationChannel,this);
    }

    public void ReceivePacket(byte[] packet, ENetPacketPeer peer)
    {
        Debug.Print("WorldSpaceServer: Packet received.");
        try {
            WorldCellRequest request = WorldCellRequest.Deserialize(packet);
            Debug.Print("WorldSpaceServer: Request received: " + request);
            Task.Run(async () => {
                Task<LevelCell> levelCell = activeSpace.GetLevelCell(request.coords);
                await levelCell;
                request.payload = levelCell.Result.Serialize();
                request.status = WorldCellRequest.RequestStatus.Fulfilled;
                byte[] packet = request.Serialize();
                Debug.Print("WorldSpaceServer: Responding to request: " + request);
                Debug.Print("WorldSpaceServer: Packet size is " + packet.Length);
                peer.Send(communicationChannel, packet, (int)ENetPacketPeer.FlagReliable);
            });
        }
        catch (Exception e){
            Debug.Print("WorldSpaceServer: Error in deserializing packet: " + e.Message);
        }
    }
}
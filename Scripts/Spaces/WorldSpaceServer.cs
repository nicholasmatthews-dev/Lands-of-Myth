using System;
using System.Diagnostics;
using Godot;
using LOM.Multiplayer;

namespace LOM.Spaces;

public class WorldSpaceServer : ENetPacketListener
{
    private ENetServer eNetServer;
    private WorldSpace activeSpace = new();

    public WorldSpaceServer(ENetServer server){
        eNetServer = server;
        eNetServer.AddPacketListener((int)ENetCommon.ChannelNames.Spaces,this);
    }

    public void ReceivePacket(byte[] packet, ENetPacketPeer peer)
    {
        Debug.Print("WorldSpaceServer: Packet received.");
        try {
            WorldCellRequest request = WorldCellRequest.Deserialize(packet);
            
        }
        catch (Exception e){
            Debug.Print("WorldSpaceServer: Error in deserializing packet: " + e.Message);
        }
    }
}
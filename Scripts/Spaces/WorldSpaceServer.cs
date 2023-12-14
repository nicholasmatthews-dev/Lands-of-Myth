using Godot;
using LOM.Multiplayer;

namespace LOM.Spaces;

public class WorldSpaceServer : ENetPacketListener
{
    private ENetServer eNetServer;

    public WorldSpaceServer(ENetServer server){
        
    }

    public void ReceivePacket(byte[] packet, ENetPacketPeer peer)
    {
        throw new System.NotImplementedException();
    }
}
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using LOM.Levels;
using LOM.Multiplayer;

namespace LOM.Spaces;

public class WorldSpaceClient : Space, ENetPacketListener {
    private ENetClient netClient;
    private ConcurrentDictionary<Vector2I, EventWaitHandle> dataWaitHandles = new();
    public WorldSpaceClient(ENetClient eNetClient) : base(){
        netClient = eNetClient;
        netClient.AddPacketListener((int)ENetCommon.ChannelNames.Spaces, this);
    }

    public override Task<LevelCell> GetLevelCell(Vector2I cellCoords)
    {
        throw new System.NotImplementedException();
    }

    public void ReceivePacket(byte[] packet, ENetPacketPeer peer)
    {
        Debug.Print("WorldSpaceClient: Packet received.");
    }

    public override void StoreBytesToCell(byte[] cellToStore, Vector2I cellCoords)
    {
        return;
    }
}
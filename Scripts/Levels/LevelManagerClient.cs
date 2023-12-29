using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using LOM.Multiplayer;

namespace LOM.Levels;

public class LevelManagerClient : ILevelManager, IENetPacketListener
{
    private static bool Debugging = false;
    private ILevelHost levelHost;
    private ENetServer eNetServer;
    private int communicationChannel = (int)ENetCommon.ChannelNames.Spaces;
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

    private void HandlePacket(byte[] packet, ENetPacketPeer peer){
        LevelCellRequest request = LevelCellRequest.Deserialize(packet);
        if (Debugging) Debug.Print("LevelManagerClient: Received request " + request);
        RespondToRequest(request, peer);
    }

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
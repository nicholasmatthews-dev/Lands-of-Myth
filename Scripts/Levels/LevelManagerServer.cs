using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using LOM.Multiplayer;

namespace LOM.Levels;

public class LevelManagerServer : ILevelManager, ENetPacketListener
{
    private static bool Debugging = true;
    private ILevelHost levelHost;
    private ENetServer eNetServer;
    private int communicationChannel = (int)ENetCommon.ChannelNames.Spaces;

    public LevelManagerServer(ENetServer eNetServer){
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
        if (Debugging) Debug.Print("LevelManagerServer: Received request " + request);
        RespondToRequest(request, peer);
    }

    private void RespondToRequest(LevelCellRequest request, ENetPacketPeer peer){
        Task.Run(() => {
            Task<LevelCellRequest> requestTask = levelHost.GetLevelCell(request);
            requestTask.Wait();
            LevelCellRequest fulfilledRequest = requestTask.Result;
            if (Debugging) Debug.Print("LevelManagerServer: Responding to request with " + fulfilledRequest);
            byte[] packet = fulfilledRequest.Serialize();
            peer.Send(communicationChannel, packet, (int)ENetPacketPeer.FlagReliable);
        });
    }

    public void ReceivePacket(byte[] packet, ENetPacketPeer peer)
    {
        HandlePacket(packet, peer);
    }
}
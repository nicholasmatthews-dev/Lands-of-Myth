using Godot;
using LOM.Multiplayer;

namespace LOM.Game;

public class ServerManager : IENetConnectionListener
{
    public void HandleConnection(ENetPacketPeer peer)
    {
        throw new System.NotImplementedException();
    }
}
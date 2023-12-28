using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using LOM.Multiplayer;

namespace LOM.Game;

public class ClientManager : IENetConnectionListener
{
    private static bool Debugging = true;
    private GameModel gameModel;
    public GameModel GameModel {get => gameModel;}

    private ENetServer eNetServer;
    public ENetServer ENetServer {get => eNetServer;}

    public List<PlayerClient> clients = new();

    public ClientManager(ENetServer eNetServer, GameModel gameModel){
        this.eNetServer = eNetServer;
        eNetServer.AddConnectionListener(this);
        this.gameModel = gameModel;
    }

    public void HandleConnection(ENetPacketPeer peer)
    {
        if (Debugging) Debug.Print("ClientManager: Received connection from " + peer);
        PlayerClient newClient = new(eNetServer, peer, gameModel);
        clients.Add(newClient);
    }
}
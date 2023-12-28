using Godot;
using LOM.Multiplayer;

namespace LOM.Game;

public partial class GameServer : RefCounted {
    private GameModel gameModel = new();
    public GameModel GameModel {get => gameModel;}

    private ENetServer eNetServer;
    public ENetServer ENetServer {get => eNetServer;}

    private ClientManager connectionManager;

    public GameServer(int port){
        eNetServer = new(port);
        connectionManager = new(eNetServer, gameModel);
    }
}
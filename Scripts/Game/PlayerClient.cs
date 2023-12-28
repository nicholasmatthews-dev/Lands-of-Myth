using Godot;
using LOM.Levels;
using LOM.Multiplayer;

namespace LOM.Game;

public class PlayerClient {
    private ILevelManager levelManager;
    public ILevelManager LevelManager {get => levelManager;}

    private ENetPacketPeer associatedPeer;

    public PlayerClient(ENetServer eNetServer, ENetPacketPeer associatedPeer, GameModel gameModel){
        this.associatedPeer = associatedPeer;
        levelManager = new LevelManagerClient(eNetServer, associatedPeer);
        levelManager.ConnectLevelHost(gameModel.LevelHost);
    }
    
}
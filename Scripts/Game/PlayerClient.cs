using Godot;
using LOM.Levels;
using LOM.Multiplayer;

namespace LOM.Game;

/// <summary>
/// Represents the server layer representation of a connected <see cref="Player"/>. 
/// </summary>
public class PlayerClient {
    /// <summary>
    /// The <see cref="ILevelManager"/> implementation for this client. 
    /// </summary>
    private ILevelManager levelManager;
    /// <summary>
    /// The <see cref="ILevelManager"/> implementation for this client. 
    /// </summary>
    public ILevelManager LevelManager {get => levelManager;}

    /// <summary>
    /// The peer that this client is associated with.
    /// </summary>
    private ENetPacketPeer associatedPeer;

    public PlayerClient(ENetServer eNetServer, ENetPacketPeer associatedPeer, GameModel gameModel){
        this.associatedPeer = associatedPeer;
        levelManager = new LevelManagerClient(eNetServer, associatedPeer);
        levelManager.ConnectLevelHost(gameModel.LevelHost);
    }
    
}
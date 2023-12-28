using Godot;
using LOM.Levels;
using LOM.Multiplayer;
using LOM.Spaces;

namespace LOM.Game;

public partial class GameClient : RefCounted {
    private ENetClient eNetClient;

    private TileSetManager tileSetManager = new();
    public TileSetManager TileSetManager {get => tileSetManager;}

    private Player player = new();
    public Player Player {get => player;}

    public GameClient(string address, int port){
        WorldSpaceToken worldSpaceToken = new("Overworld");
        eNetClient = new(address, port);
        LevelHostServer levelHostServer = new(eNetClient);
        player.LevelManager.ConnectLevelHost(levelHostServer);
        player.LevelManager.ChangeActiveSpace(worldSpaceToken, new CellPosition(0,0));
    }

}
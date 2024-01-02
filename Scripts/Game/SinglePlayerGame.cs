using LOM.Control;
using LOM.Levels;
using LOM.Spaces;

namespace LOM.Game;

public class SinglePlayerGame {
    private Player player = new();
    private GameModel gameModel = new();
    public LevelManager LevelManager {get => player.LevelManager;}
    public TileSetManager TileSetManager {get => gameModel.TileSetManager;}
    public SinglePlayerGame(){
        player.LevelManager.ConnectLevelHost(gameModel.LevelHost);
        player.LevelManager.ChangeActiveSpace(new WorldSpaceToken("Overworld"), new CellPosition(0,0));
    }

    public void RegisterPostionUpdateSource(IPositionUpdateSource source){
        player.LevelManager.RegisterPostionUpdateSource(source);
    }
}
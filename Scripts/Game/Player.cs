using LOM.Levels;

namespace LOM.Game;

public class Player {
    private LevelManager levelManager = new();
    public LevelManager LevelManager {get => levelManager;}

    public Player(){
        
    }
}
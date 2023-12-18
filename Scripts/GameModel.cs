using Godot;
using LOM.Levels;
using LOM.Spaces;
using System;

public class GameModel {
    private TileSetManager tileSetManager = new();
    private LevelManager levelManager = new();

    public LevelManager LevelManager { get => levelManager; }
    public TileSetManager TileSetManager { get => tileSetManager; }

    public void ChangeActiveSpace(Space newSpace, Vector2I newCoords){
        LevelManager.ChangeActiveSpace(newSpace, newCoords);
    }
}
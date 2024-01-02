using Godot;
using LOM.Levels;
using LOM.Spaces;
using System;

public class GameModel {
    private TileSetManager tileSetManager = new();
    public TileSetManager TileSetManager { get => tileSetManager; }

    private LevelHost levelHost;
    public ILevelHost LevelHost { get => levelHost; }

    public GameModel(){
        levelHost = new(tileSetManager);
    }

}
using Godot;
using LOM.Levels;
using LOM.Spaces;
using System;

public class GameModel {
    public static TileSetManager tileSetManager = new();
    public static LevelManager levelManager = new();

    public static void ChangeActiveSpace(Space newSpace, Vector2I newCoords){
        levelManager.ChangeActiveSpace(newSpace, newCoords);
    }
}
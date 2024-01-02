using Godot;
using LOM.Levels;

namespace LOM.Game;

public partial class SinglePlayerNode : Node2D {
    private SinglePlayerGame game = new();
    private LevelManagerNode levelManagerNode;
    private GameScene gameScene;
    public override void _Ready()
    {
        gameScene = (GameScene)ResourceLoader
        .Load<PackedScene>("res://Scenes/game_scene.tscn")
        .Instantiate();
        levelManagerNode = new(game.LevelManager, game.TileSetManager){
            Name = "LevelManager"
        };
        gameScene.Movement.ActiveLevel = game.LevelManager;
        game.RegisterPostionUpdateSource(gameScene.Movement);
        AddChild(levelManagerNode);
        AddChild(gameScene);
    }
}
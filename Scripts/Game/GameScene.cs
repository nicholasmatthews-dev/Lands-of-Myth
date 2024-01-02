using Godot;
using LOM.Control;

namespace LOM.Game;

public partial class GameScene : Node2D {
    [Export]
    public Movement Movement;

    [Export]
    public CameraFollow CameraFollow;
}
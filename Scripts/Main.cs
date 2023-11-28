using LOM.Control;
using Godot;
using System;

public partial class Main : Node2D
{
	private CameraFollow camera;
	public static Movement movement;
	private Node2D character;
	private LevelManager levelManager;
	public static TileSetManager tileSetManager = new TileSetManager();
	public static World world = new World("New World");
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//tileSetManager = new TileSetManager();
		camera = (CameraFollow)ResourceLoader
		.Load<PackedScene>("res://Scenes/Characters/camera_follow.tscn")
		.Instantiate();
		movement = (Movement)ResourceLoader
		.Load<PackedScene>("res://Scenes/Characters/movement.tscn")
		.Instantiate();
		character = (Node2D)ResourceLoader
		.Load<PackedScene>("res://Scenes/Characters/00dummy.tscn")
		.Instantiate();
		levelManager = new LevelManager();

		camera.Target = character;
		movement.Target = character;
		movement.ActiveLevel = levelManager;
		
		AddChild(tileSetManager);
		AddChild(levelManager);
		AddChild(character);
		AddChild(movement);
		AddChild(camera);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

using Control;
using Godot;
using System;

public partial class Main : Node2D
{
	private CameraFollow camera;
	private Movement movement;
	private Node2D character;
	private LevelManager levelManager;
	private TileSetManager tileSetManager;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		tileSetManager = new TileSetManager();
		camera = (CameraFollow)ResourceLoader
		.Load<PackedScene>("res://Scenes/Characters/camera_follow.tscn")
		.Instantiate();
		movement = (Movement)ResourceLoader
		.Load<PackedScene>("res://Scenes/Characters/movement.tscn")
		.Instantiate();
		character = (Node2D)ResourceLoader
		.Load<PackedScene>("res://Scenes/Characters/00dummy.tscn")
		.Instantiate();
		levelManager = new LevelManager(tileSetManager);

		camera.Target = character;
		movement.Target = character;
		movement.ActiveLevel = levelManager;
		
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

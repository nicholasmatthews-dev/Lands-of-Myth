using LOM.Control;
using LOM.Levels;
using LOM.Spaces;
using Godot;
using System;
using LOM.Multiplayer;
using System.Threading;
using System.Diagnostics;
using LOM.Game;

public partial class Main : Node2D
{
	private CameraFollow camera;
	public static Movement movement;
	private Node2D character;
	private LevelManagerNode levelManager;
	public GameModel gameModel = new();
	public Player player = new();
	public static World world = new World("New World");
	public LevelManagerServer managerServer;
	ENetServer server;
	ENetClient client;
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
        levelManager = new LevelManagerNode(player.LevelManager, gameModel.TileSetManager)
        {
            Name = "LevelManager"
        };

		CreateClientServer();
		LevelHostClient hostClient = new(client);
		managerServer = new(server);
		managerServer.ConnectLevelHost(gameModel.LevelHost);

		WorldSpaceToken tokenA = new("Overworld");


        camera.Target = character;
		player.LevelManager.ConnectLevelHost(hostClient);
		player.LevelManager.RegisterPostionUpdateSource(movement);
		player.LevelManager.ChangeActiveSpace(tokenA, new CellPosition(0,0));
		movement.Target = character;
		movement.ActiveLevel = player.LevelManager;
		
		//AddChild(gameModel.TileSetManager);
		AddChild(levelManager);
		AddChild(character);
		AddChild(movement);
		AddChild(camera);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void CreateClientServer(){
		server = new(65535);
		client = new("192.168.1.3", 65535);
	}
}

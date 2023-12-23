using LOM.Control;
using LOM.Levels;
using LOM.Spaces;
using Godot;
using System;
using LOM.Multiplayer;
using System.Threading;
using System.Diagnostics;

public partial class Main : Node2D
{
	private CameraFollow camera;
	public static Movement movement;
	private Node2D character;
	private LevelManagerNode levelManager;
	public GameModel gameModel = new();
	public static World world = new World("New World");
	public WorldSpaceClient worldSpaceClient;
	public WorldSpaceServer worldSpaceServer;
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
        levelManager = new LevelManagerNode(gameModel.LevelManager, gameModel.TileSetManager)
        {
            Name = "LevelManager"
        };

		/*CreateClientServer();
		worldSpaceClient = new(client, gameModel.TileSetManager);
		worldSpaceServer = new(server, gameModel.TileSetManager);
		gameModel.ChangeActiveSpace(worldSpaceClient, new Vector2I(0,0));*/

		WorldSpaceToken tokenA = new("Overworld");
		WorldSpaceToken tokenB = new("Overworld");
		Debug.Print("Main: World space tokens are equal? " + (tokenA == tokenB));
		WorldCellRequest requestA = new(tokenA, new CellPosition(0, 0));
		WorldCellRequest requestB  = new(tokenB, new CellPosition(0,0));
		Debug.Print("Main: Requests are equal? " + (requestA == requestB));
		byte[] serializedRequest = requestA.Serialize();
		WorldCellRequest deserializedRequest = (WorldCellRequest)LevelCellRequest.Deserialize(serializedRequest);
		Debug.Print("Main: Deserialized request is equal to original? " + (requestA == deserializedRequest));

        camera.Target = character;
		gameModel.LevelManager.RegisterPostionUpdateSource(movement);
		movement.Target = character;
		movement.ActiveLevel = gameModel.LevelManager;
		
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

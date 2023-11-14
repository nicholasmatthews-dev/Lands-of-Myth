using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public partial class LevelCell : Node2D
{
	[Export]
	public int TileHeight = 16;
	[Export]
	public int TileWidth = 16;
	[Export]
	public int Width = 64;
	[Export]
	public int Height = 64;

	public TileMap Tiles = new TileMap();
	private List<int> tileSets = new List<int>();
	private Dictionary<int, int> localCodes = new Dictionary<int, int>();
	private LevelManager levelManager;
	private bool[,] Solid;

	public LevelCell(LevelManager manager) : base(){
		levelManager = manager;
		TileHeight = manager.TileHeight;
		TileWidth = manager.TileWidth;
		Width = manager.CellWidth;
		Height  = manager.CellHeight;
		Tiles.TileSet = manager.GetTileSetManager().GetDefaultTileSet();
		Tiles.AddLayer(-1);
		Tiles.AddLayer(-1);
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		AddChild(Tiles);
		Solid = new bool[Width,Height];
		for (int i = 0; i < Width; i++){
			for (int j = 0; j < Height; j++){
				Solid[i,j] = false;
			}
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void AddTileSetSource(int tileSetId){
		if (tileSets.Contains(tileSetId)){
			return;
		}
		TileSetManager tileManager = levelManager.GetTileSetManager();
		try {
			TileSetSource toAdd = tileManager.GetTileSetSource(tileSetId);
			tileSets.Add(tileSetId);
			localCodes.Add(tileSetId, tileSets.Count - 1);
			Tiles.TileSet.AddSource(toAdd, tileSets.Count - 1);
		}
		catch (Exception e) {
			Debug.Print(e.Message);
		}
	}

	public void CheckSolid(){
		for (int i=0; i < Width; i++){
			for (int j=0; j < Height; j++){
				for (int k=0; k < Tiles.GetLayersCount(); k++){
					TileData cellData = Tiles.GetCellTileData(k, new Vector2I(i,j));
					if (cellData is null){
						break;
					}
					Variant solidData = cellData.GetCustomData("Solid");
					if (solidData.VariantType == Variant.Type.Bool){
						bool solidPresent = solidData.AsBool();
						Solid[i,j] = solidPresent || Solid[i,j];
					}
				}
			}
		}
	}

	public bool PositionValid(ICollection<Vector2I> occupied){
		bool valid = true;
		foreach (Vector2I position in occupied){
			if (position.X >= 0 && position.X < Width){
				if (position.Y >= 0 && position.Y < Height){
					valid = valid && !Solid[position.X,position.Y];
				}
			}
		}
		return valid;
	}

	public int GetLocalCode(int tileSetRef){
		if (!localCodes.ContainsKey(tileSetRef)){
			throw new KeyNotFoundException("Tileset reference " + tileSetRef 
			+ " not found in tileset sources.");
		}
		return localCodes[tileSetRef];
	}

	/*public byte[] Serialize(){
		
	}*/
}

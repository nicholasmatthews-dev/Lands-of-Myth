using Godot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

public partial class LevelManager : Node2D
{
	/// <summary>
	/// The height of a single tile in the tileset (in pixels).
	/// </summary>
	public int TileHeight = 16;

	/// <summary>
	/// The width of a signle tile in the tileset (in pixels).
	/// </summary>
	public int TileWidth = 16;

	/// <summary>
	/// The height of a single map cell (in tiles).
	/// </summary>
	public int CellHeight = 64;

	/// <summary>
	/// The width of a single map cell (in tiles).
	/// </summary>
	public int CellWidth = 64;

	private TileSetManager tilesetManager;

	/// <summary>
	/// The collection of active map cells, 
	/// indexed by their position (counted by number of cells from the origin).
	/// </summary>
	private Dictionary<Vector2I,LevelCell> activeCells = new Dictionary<Vector2I, LevelCell>();

	public LevelManager(TileSetManager tileManager) : base(){
		tilesetManager = tileManager;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		int forestRefId = tilesetManager.GetTileSetCode("Forest");
		for (int i = -1; i < 2; i++){
			for (int j = -1; j < 2; j++){
				LevelCell currentLevel = new LevelCell(this);
				AddChild(currentLevel);
				activeCells.Add(new Vector2I(i,j), currentLevel);
				currentLevel.Position = new Vector2(TileWidth * CellWidth * i, TileHeight * CellHeight * j);
				if ((i + j) % 2 == 0){
					PopulateTiles(currentLevel, new Vector2I(0,0), forestRefId);
				}
				else{
					PopulateTiles(currentLevel, new Vector2I(2,1), forestRefId);
				}
				if (i == 0 && j == 0){
					AddStructure(currentLevel);
					currentLevel.CheckSolid();
				}
			}
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public TileSetManager GetTileSetManager(){
		return tilesetManager;
	}

	public bool PositionValid(ICollection<Vector2I> occupied){
		bool valid = true;
		foreach (Vector2I position in occupied){
			List<Vector2I> cellCoords = TranslateCoords(position);
			List<Vector2I> toCheck = new List<Vector2I>
            {
                cellCoords[1]
            };
			valid = valid && activeCells[cellCoords[0]].PositionValid(toCheck);
		}
		return valid;
	}

	/// <summary>
	/// Translates from a global coordinates system to the cell coordinates, and the coordinates
	/// within that cell. The first item in the returned list is the cell, and the second item
	/// is the coordinates locally within that cell.
	/// </summary>
	/// <param name="coords"></param>
	/// <returns></returns>
	private List<Vector2I> TranslateCoords(Vector2I coords){
		List<Vector2I> output = new List<Vector2I>();
		
		//Calculates the appropriate cell that coords lies in
		int cellX = FloorDivision(coords.X, CellWidth);
		int cellY = FloorDivision(coords.Y, CellHeight);
		Vector2I cellPosition = new Vector2I(cellX, cellY);
		
		//Calculates the coordinates relative to the origin of the cell coords lies in
		int localX = coords.X % CellWidth;
		if (localX < 0){
			localX += CellWidth;
		}
		int localY = coords.Y % CellHeight;
		if (localY < 0){
			localY += CellHeight;
		}
		Vector2I localPosition = new Vector2I(localX, localY);
		
		output.Add(cellPosition);
		output.Add(localPosition);
		return output;
	}

	private static int FloorDivision(int a, int b){
		if (((a < 0) || (b < 0)) && (a % b != 0)){
			return (a / b - 1);
		}
		else {
			return (a / b);
		}
	}

	private void PopulateTiles(LevelCell input, Vector2I fill, int tileSetRef){
		for (int i = 0; i < CellWidth; i++){
			for (int j = 0; j < CellHeight; j++){
				input.Place(0, tileSetRef, new Vector2I(i, j), fill);
			}
		}
	}

	private void AddStructure(LevelCell input){
		int buildingsRefId = tilesetManager.GetTileSetCode("Elf_Buildings");
		TileMap houses = (TileMap)ResourceLoader
		.Load<PackedScene>("res://Scenes/Maps/elf_buildings_test.tscn")
		.Instantiate();
		for (int i = 0; i < CellWidth; i++){
			for (int j = 0; j < CellWidth; j++){
				for (int k = 0; k < 2; k++){
					Vector2I atlasCoords = houses.GetCellAtlasCoords(k, new Vector2I(i, j));
					input.Place(k + 1, buildingsRefId, new Vector2I(i, j), atlasCoords);
				}
			}
		}
		houses.Free();
	}

}

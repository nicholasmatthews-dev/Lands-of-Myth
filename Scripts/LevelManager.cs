using Control;
using Godot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

public partial class LevelManager : Node2D, PositionUpdateListener
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
	public static int CellHeight = 64;

	/// <summary>
	/// The width of a single map cell (in tiles).
	/// </summary>
	public static int CellWidth = 64;

	private Space activeSpace;

	private Vector2I lastPosition = new(0,0);

	/// <summary>
	/// The collection of active map cells, 
	/// indexed by their position (counted by number of cells from the origin).
	/// </summary>
	private Dictionary<Vector2I,LevelCell> activeCells = new Dictionary<Vector2I, LevelCell>();

	public LevelManager() : base(){
		activeSpace = new WorldSpace();
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		ChangeLoadedCells(lastPosition);
		Main.movement.AddPositionUpdateListener(this);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void OnPositionUpdate(Vector2I coords){
		List<Vector2I> translatedCoords = TranslateCoords(coords);
		if (translatedCoords[0] != lastPosition){
			lastPosition = translatedCoords[0];
			ChangeLoadedCells(lastPosition);
		}
	}

	/// <summary>
	/// Updates the currently loaded cells so they are centered on the given cooridinates in cell space.
	/// <para>
	/// That is, all cells in a 3x3 square centered around the given cooridinates will be loaded, and any
	/// cells which were previously outside of the space will be unloaded.
	/// </para>
	/// </summary>
	/// <param name="coords"></param>
	private void ChangeLoadedCells(Vector2I coords){
		List<Vector2I> cellsToRemove = new List<Vector2I>(9);
		foreach (KeyValuePair<Vector2I, LevelCell> entry in activeCells){
			if (Math.Abs(coords.X - entry.Key.X) > 1){
				cellsToRemove.Add(entry.Key);
			}
		}
		foreach (Vector2I entry in cellsToRemove){
			DisposeOfCell(entry);
		}
		for (int i = -1; i < 2; i++){
			for (int j = -1; j < 2; j++){
				Vector2I currentCoords = new Vector2I(i + coords.X, j + coords.Y);
				LoadLevelCellFromSpace(currentCoords);
			}
		}
	}

	/// <summary>
	/// Loads in a LevelCell at the given coords from the currently active Space.
	/// </summary>
	/// <param name="coords">The coordinates of the LevelCell to be loaded.</param>
	private void LoadLevelCellFromSpace(Vector2I coords){
		if (!activeCells.ContainsKey(coords)){
			LevelCell loaded = activeSpace.GetLevelCell(coords);
			AddActiveCell(coords, loaded);
		}
	}

	/// <summary>
	/// Adds a LevelCell into the active cells dictionary with the specified coordinates, and positions
	/// it into the correct place.
	/// </summary>
	/// <param name="coords">The coordinates (given in the cell grid space).</param>
	/// <param name="levelCell">The LevelCell to be loaded.</param>
	private void AddActiveCell(Vector2I coords, LevelCell levelCell){
		AddChild(levelCell);
		activeCells.Add(coords, levelCell);
		levelCell.Position = new Vector2
		(
			TileWidth * CellWidth * coords.X,
			TileHeight * CellHeight * coords.Y
		);
	}

	/// <summary>
	/// Unloads the cell at the given coordinates.
	/// </summary>
	/// <param name="coords">The coordinates of the cell to unload.</param>
	private void DisposeOfCell(Vector2I coords){
		activeSpace.StoreBytesToCell(activeCells[coords].Serialize(), coords);
		activeCells[coords].Free();
		activeCells.Remove(coords);
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

}

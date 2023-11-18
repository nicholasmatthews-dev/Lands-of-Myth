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

	private static int numLayers = 3;

	private TileMap tiles = new TileMap();
	private Dictionary<int, TileSetTicket> tickets = new Dictionary<int, TileSetTicket>();
	private Dictionary<int, int> atlasCodesToRef = new Dictionary<int, int>();
	private TileSetManager tileSetManager;
	private bool[,] Solid;

	public LevelCell(LevelManager manager) : base(){
		tileSetManager = manager.GetTileSetManager();
		TileHeight = manager.TileHeight;
		TileWidth = manager.TileWidth;
		Width = manager.CellWidth;
		Height  = manager.CellHeight;
		tiles.TileSet = tileSetManager.GetDefaultTileSet();
		tiles.AddLayer(-1);
		tiles.AddLayer(-1);
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		AddChild(tiles);
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

	/// <summary>
	/// Updates the solid array by reading the data of the given cells.
	/// </summary>
	public void CheckSolid(){
		for (int i=0; i < Width; i++){
			for (int j=0; j < Height; j++){
				for (int k=0; k < tiles.GetLayersCount(); k++){
					TileData cellData = tiles.GetCellTileData(k, new Vector2I(i,j));
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

	/// <summary>
	/// Checks if a collection of tiles is a valid placement (doesn't collide with any
	/// tiles marked solid).
	/// </summary>
	/// <param name="occupied">The collection of tiles to check collision for.</param>
	/// <returns><c>True</c> if none of the tiles are solid, <c>False</c> otherwise.</returns>
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

	/// <summary>
	/// Places a tile on this cell's tile map.
	/// </summary>
	/// <param name="layer">The layer to place the tile on.</param>
	/// <param name="tileSetRef">The reference id for the tileset to be used.</param>
	/// <param name="coords">The position to place the tile on.</param>
	/// <param name="atlasCoords">The atlas coordinates of the tile to draw.</param>
	public void Place(int layer, int tileSetRef, Vector2I coords, Vector2I atlasCoords){
		if (!tickets.ContainsKey(tileSetRef)){
			AddTicket(tileSetRef);
		}
		tiles.SetCell
		(
			layer, 
			coords, 
			tickets[tileSetRef].GetAtlasId(),
			atlasCoords
		);
	}

	/// <summary>
	/// Requests a ticket for a given <c> tileSetRef </c> and then adds the ticket
	/// to the list of active tickets. Additionally stores a dictionary entry to associate
	/// the atlas id of the ticket with the given <c> tileSetRef </c> mainly used for
	/// <see cref="GetTileIdentifier"/> 
	/// </summary>
	/// <param name="tileSetRef">The reference code for the desired tileset.</param>
	private void AddTicket(int tileSetRef){
		TileSetTicket tileSetTicket = tileSetManager.GetTileSetTicket(tileSetRef);
		tickets.Add(tileSetRef, tileSetTicket);
		atlasCodesToRef.Add(tileSetTicket.GetAtlasId(), tileSetRef);
	}

	/// <summary>
	/// TODO: Implement full functionality.
	/// <para>
	/// Returns a representation of this LevelCell as an ordered array of bytes.
	/// </para>
	/// </summary>
	/// <returns>An ordered byte array representing this object.</returns>
	public byte[] Serialize(){
		Dictionary<(int, Vector2I), int> uniqueTiles = CountUniqueTiles();
		int tileBits = (int)Math.Ceiling(Math.Log2(uniqueTiles.Count));
		int totalTileBytes = (int)Math.Ceiling(tileBits * Width * Height * numLayers / 8.0);
		int sourceLibraryBytes = uniqueTiles.Count * (4 + 4 + 4) + 4;
		uint bitMask = (uint)((1 << tileBits) - 1);
		Debug.Print("There are " + uniqueTiles.Count + " unique tiles.");
		Debug.Print("It will take " + tileBits + " bits to encode each tile.");
		Debug.Print("It will take " + totalTileBytes + " Bytes to encode all tiles.");
		Debug.Print("It will take " + sourceLibraryBytes + " Bytes to encode the sources.");
		Debug.Print("The estimate for the total uncompressed size of this tile is " 
		+ (totalTileBytes + sourceLibraryBytes) + " Bytes.");
		Debug.Print("The bit mask is " + Convert.ToString(bitMask, toBase: 2));
		List<int> tileCodes = GetTilesAsCodeArray(uniqueTiles);
		List<int> test = new List<int>{
			0b_0110_1110_1001,
			0b_0010_1111_0000
		};
		List<int> testOutput = SerializationHelper.ConvertBetweenCodes(test, 11, 8);
		foreach (int result in testOutput){
			Debug.Print("Output byte is " + Convert.ToString(result, toBase : 2));
		}
		testOutput = SerializationHelper.ConvertBetweenCodes(testOutput, 8, 11, true);
		foreach (int result in testOutput){
			Debug.Print("Output word is " + Convert.ToString(result, toBase : 2));
		}
		return new byte[1];
	}

	/// <summary>
	/// Returns all tiles as a linear list of tile identifier codes 
	/// (see <see cref="CountUniqueTiles"/>).
	/// <para>
	/// Follows the ordering in terms of blocks Layer->Row->Column->Tile.
	/// </para>
	/// </summary>
	/// <param name="uniqueTiles">A dictionary of unique tiles and their tile identifier codes.</param>
	/// <returns>A list of tile identifier codes as described above.</returns>
	private List<int> GetTilesAsCodeArray(Dictionary<(int, Vector2I), int> uniqueTiles){
		List<int> tileCodes = new List<int>();
		for (int i = 0; i < numLayers; i++){
			for (int j = 0; j < Width; j++){
				for (int k = 0; k < Height; k++){
					(int, Vector2I) tileIdentifier = GetTileIdentifier(i, new Vector2I(j, k));
					int currentCode = uniqueTiles[tileIdentifier];
					tileCodes.Add(currentCode);
				}
			}
		}
		return tileCodes;
	}

	/// <summary>
	/// Returns the tile identifier for a specific tile. Represented by a tuple with the
	/// following format <c> (int tileSetRef, Vector2I atlasCoords) </c>
	/// </summary>
	/// <param name="layer">The layer of the specified tile.</param>
	/// <param name="coords">The position of the specified tile.</param>
	/// <returns>A tuple with the format specified above.</returns>
	private (int, Vector2I) GetTileIdentifier(int layer, Vector2I coords){
		int sourceId = tiles.GetCellSourceId(layer, coords);
		if (sourceId == -1){
			return (-1, new Vector2I(-1, -1));
		}
		int sourceRef = atlasCodesToRef[sourceId];
		Vector2I atlasCoords = tiles.GetCellAtlasCoords(layer, coords);
		return (sourceRef, atlasCoords);
	}

	/// <summary>
	/// Returns a dictionary which associates tile types (represented as a tuple with the 
	/// following format: <c> (int tileSetRef, Vector2I atlasCoords) </c>) with a corresponding
	/// integer which uniquely identifies that tile type.
	/// <para>
	/// Note that the unique tile (-1, (-1, -1)) is always assigned with the id 0.
	/// </para>
	/// </summary>
	/// <returns>A dictionary with the above referenced structure.</returns>
	private Dictionary<(int, Vector2I), int> CountUniqueTiles(){
		int tileTypes = 0;
		// All tiles and their corresponding library index, in the format (tileSetRef, atlasx, atlasy)
		Dictionary<(int, Vector2I), int> uniqueTiles = new Dictionary<(int, Vector2I), int>
        {
            { (-1, new Vector2I(-1, -1)), 0 }
        };
		for (int i = 0; i < Width; i++){
			for (int j = 0; j < Height; j++){
				for (int k = 0; k < numLayers; k++){
					(int, Vector2I) tileType = GetTileIdentifier(k, new Vector2I(i, j));
					if (!uniqueTiles.ContainsKey(tileType)){
						tileTypes++;
						uniqueTiles.Add(tileType, tileTypes);
						Debug.Print("Found new tile type:" + tileType);
					}
				}
			}
		}
		return uniqueTiles;
	}
}

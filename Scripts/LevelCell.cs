using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
		List<(int, List<Vector2I>)> tileSetSources = GetUniqueTileList();
		Dictionary<(int, Vector2I), int> uniqueTiles = GetTileSetCodes(tileSetSources);
		int tileBits = SerializationHelper.RepresentativeBits(uniqueTiles.Count);
		/*int totalTileBytes = (int)Math.Ceiling(tileBits * Width * Height * numLayers / 8.0);
		int sourceLibraryBytes = 4 + 8 * tileSetSources.Count + uniqueTiles.Count * 2;
		Debug.Print("There are " + uniqueTiles.Count + " unique tiles.");
		Debug.Print("It will take " + tileBits + " bits to encode each tile.");
		Debug.Print("It will take " + totalTileBytes + " Bytes to encode all tiles.");
		Debug.Print("It will take " + sourceLibraryBytes + " Bytes to encode the sources.");
		Debug.Print("The estimate for the total uncompressed size of this tile is " 
		+ (totalTileBytes + sourceLibraryBytes) + " Bytes.");*/
		List<int> tileCodes = GetTilesAsCodeArray(uniqueTiles);
		List<int> tileCodes8Bit = SerializationHelper.ConvertBetweenCodes(tileCodes, tileBits, 8);
		List<byte> tileCodesBytes = new List<byte>(tileCodes8Bit.Count);
		foreach (int entry in tileCodes8Bit){
			tileCodesBytes.Add((byte)entry);
		}
		List<byte> headerEncoded = EncodeTileSourceHeader(tileSetSources);
		/*Debug.Print("Sources header is " + headerEncoded.Count + " Bytes long.");
		Debug.Print("Payload is " + tileCodesBytes.Count + " Bytes long.");*/
		List<byte> outputList = new List<byte>(tileCodesBytes.Count + headerEncoded.Count);
		outputList.AddRange(headerEncoded);
		outputList.AddRange(tileCodesBytes);
		return outputList.ToArray();
	}

	public static LevelCell Deserialize(byte[] input, LevelManager manager){
		LevelCell output = new LevelCell(manager);
		List<byte> bytes = new List<byte>(input);
		List<(int, List<Vector2I>)> tileSetSources = DecodeTileSourceHeader(bytes);
		int totalTiles = 1;
		foreach ((int, List<Vector2I>) entry in tileSetSources){
			foreach (Vector2I uniqueTile in entry.Item2){
				totalTiles++;
			}
		}
		int tileBits = SerializationHelper.RepresentativeBits(totalTiles);
		List<byte> payload = bytes.GetRange(tileSetSources.Count, bytes.Count - tileSetSources.Count);
		List<int> payloadInts = new List<int>(payload.Count);
		foreach (byte entry in payload){
			payloadInts.Add(entry);
		}
		Dictionary<(int, Vector2I), int> tileCodes = GetTileSetCodes(tileSetSources);
		Dictionary<int, (int, Vector2I)> codesToTiles = tileCodes.ToDictionary((i) => i.Value, (i) => i.Key);
		List<int> codeList = SerializationHelper.ConvertBetweenCodes(payloadInts, 8, tileBits, true);
		PlaceTileCodes(output, codesToTiles, codeList);
		return output;
	}

	private List<byte> EncodeTileSourceHeader(List<(int, List<Vector2I>)> tileSources){
		int outputSize = 4  + 8 * tileSources.Count;
		foreach ((int, List<Vector2I>) tileSource in tileSources){
			outputSize += 2 * tileSource.Item2.Count;
		}
		List<byte> output = new List<byte>(outputSize);
		output.AddRange(BitConverter.GetBytes(tileSources.Count));
		for (int i = 0; i < tileSources.Count; i++){
			output.AddRange(BitConverter.GetBytes(tileSources[i].Item1));
			output.AddRange(BitConverter.GetBytes(tileSources[i].Item2.Count));
			for (int j = 0; j < tileSources[i].Item2.Count; j++){
				output.Add((byte)tileSources[i].Item2[j].X);
				output.Add((byte)tileSources[i].Item2[j].Y);
			}
		}
		return output;
	}

	private static List<(int, List<Vector2I>)> DecodeTileSourceHeader(List<byte> bytes){
		int readHead = 0;
		int sourcesCount = BitConverter.ToInt32(bytes.GetRange(readHead, 4).ToArray());
		readHead += 4;
		List<(int, List<Vector2I>)> output = new List<(int, List<Vector2I>)>(sourcesCount);
		for (int i = 0; i < sourcesCount; i++){
			int tileId = BitConverter.ToInt32(bytes.GetRange(readHead, 4).ToArray());
			readHead += 4;
			int uniqueTiles = BitConverter.ToInt32(bytes.GetRange(readHead, 4).ToArray());
			readHead += 4;
			List<Vector2I> tileList = new List<Vector2I>();
			for (int j = 0; j < uniqueTiles; j++){
				int X = bytes[readHead];
				readHead++;
				int Y = bytes[readHead];
				readHead++;
				tileList.Add(new Vector2I(X,Y));
			}
			output.Add((tileId, tileList));
		}
		return output;
	}

	/// <summary>
	/// Returns all tiles as a linear list of tile identifier codes 
	/// (see <see cref="GetUniqueTileList"/>).
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

	private static void PlaceTileCodes
	(
		LevelCell toModify, 
		Dictionary<int, (int, Vector2I)> codesToTiles,
		List<int> codeList
	){
		for (int i = 0; i < codeList.Count; i++){
			int layer = i / (toModify.Width * toModify.Height);
			int X = i / toModify.Height % toModify.Width;
			int Y = i % toModify.Height;
			(int, Vector2I) currentTile = codesToTiles[codeList[i]];
			toModify.Place(layer, currentTile.Item1, new Vector2I(X, Y), currentTile.Item2);
		}
	}

	/// <summary>
	/// Returns a list of tuples in which each tileSet is associated with a list of all
	/// the atlas coords of all the unique tiles which use that tileSet.
	/// <para>
	/// Note that the unique tile for a null value is not included in this list.
	/// </para>
	/// </summary>
	/// <returns>A dictionary with the above referenced structure.</returns>
	private List<(int, List<Vector2I>)> GetUniqueTileList(){
		Dictionary<int, HashSet<Vector2I>> tilesByTileSet = new Dictionary<int, HashSet<Vector2I>>();
		// Count all the unique tiles, grouped into sets by tileSet
		for (int i = 0; i < Width; i++){
			for (int j = 0; j < Height; j++){
				for (int k = 0; k < numLayers; k++){
					(int, Vector2I) tileType = GetTileIdentifier(k, new Vector2I(i, j));
					if (tileType.Item1 == -1){
						break;
					}
					// Add in a new tileSet for the given tileSetRef and initializes an empty hashset
					if (!tilesByTileSet.ContainsKey(tileType.Item1)){
						HashSet<Vector2I> tilesForCurrentTileSet = new HashSet<Vector2I>();
						tilesByTileSet.Add(tileType.Item1, tilesForCurrentTileSet);
					}
					if (!tilesByTileSet[tileType.Item1].Contains(tileType.Item2)){
						tilesByTileSet[tileType.Item1].Add(tileType.Item2);
					}
				}
			}
		}
		List<(int, List<Vector2I>)> tileSetSourceList = new List<(int, List<Vector2I>)>(tilesByTileSet.Count);
		int currentSource = 0;
		foreach (KeyValuePair<int, HashSet<Vector2I>> tileSet in tilesByTileSet){
			tileSetSourceList.Add((tileSet.Key, new List<Vector2I>(tileSet.Value.Count)));
			foreach (Vector2I uniqueTile in tileSet.Value){
				tileSetSourceList[currentSource].Item2.Add(uniqueTile);
			}
			currentSource++;
		}
		return tileSetSourceList;
	}

	private static Dictionary<(int, Vector2I), int> GetTileSetCodes(List<(int, List<Vector2I>)> sourcesList){
		Dictionary<(int, Vector2I), int> tileCodes = GetDefaultTileCodeDictionary();
		int currentCode = tileCodes.Count;
		for (int i = 0; i < sourcesList.Count; i++){
			for (int j = 0; j < sourcesList[i].Item2.Count; j++){
				tileCodes.Add((sourcesList[i].Item1, sourcesList[i].Item2[j]), currentCode);
				currentCode++;
			}
		}
		return tileCodes;
	}

	private static Dictionary<(int, Vector2I), int> GetDefaultTileCodeDictionary(){
		Dictionary<(int, Vector2I), int> tileCodes = new Dictionary<(int, Vector2I), int>{
			{(-1, new Vector2I(-1, -1)), 0}
		};
		return tileCodes;
	}
}

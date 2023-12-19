using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace LOM.Levels;

public partial class LevelCell
{
	public int TileHeight = 16;
	public int TileWidth = 16;
	public static int Width = LevelManager.CellWidth;
	public static int Height = LevelManager.CellHeight;

	/// <summary>
	/// The number of layers in the <c>LevelCell</c>'s <c>TileMap</c>.
	/// </summary>
	public static int NumLayers = 3;

	/// <summary>
	/// A <c>TileMap</c> representing the tiles contained in this cell.
	/// </summary>
	//private TileMap tiles = new TileMap();
	/// <summary>
	/// A dictionary mapping between tileSetRefs and the corresponding <c>TileSetTicket</c>s which
	/// this <c>LevelCell</c> has checked out.
	/// </summary>
	private Dictionary<int, ITileSetTicket> tickets = new Dictionary<int, ITileSetTicket>();
	/// <summary>
	/// A dictionary mapping between the atlas source id in the master <c>TileSet</c> and the
	/// associated tileSetRef.
	/// </summary>
	private Dictionary<int, int> atlasCodesToRef = new Dictionary<int, int>();
	/// <summary>
	/// A 2D array representing which coordinates are solid, <c>True</c> for solid and false
	/// otherwise.
	/// </summary>
	private bool[,] Solid = new bool[Width, Height];
	/// <summary>
	/// Represents the tiles in this <c>LevelCell</c> ordered as [X, Y, Layer]. Tiles are 
	/// represented as codes given by the mapping in codesToTiles.
	/// </summary>
	private int[,,] tiles = new int[Width, Height, NumLayers];
	/// <summary>
	/// Represents a mapping between different <c>Tile</c> types and their internal code representation.
	/// </summary>
	private Dictionary<Tile, int> tileToCodes = GetDefaultTileCodeDictionary();
	/// <summary>
	/// Represents a mapping between the internal codes and a given <c>Tile</c> type.
	/// </summary>
	private Dictionary<int, Tile> codesToTiles = new(){
		{0, Tile.EmptyTile}
	};

	/// <summary>
	/// Holds all the updates to the <c>Tiles</c> in this <c>LevelCell</c> given as a tuple with the format 
	/// ((X, Y, Layer), PlacedTile). Meant to be consumed by a listener such as <c>LevelCellNode</c>.
	/// </summary>
	public ConcurrentQueue<((Position, int), Tile)> tileUpdates = new();

	private TileSetManager tileSetManager;

	public LevelCell(TileSetManager tileSetManager){
		this.tileSetManager = tileSetManager;
		for (int i = 0; i < Width; i++){
			for (int j = 0; j < Height; j++){
				Solid[i,j] = false;
				for (int k = 0; k < NumLayers; k++){
					tiles[i,j,k] = 0;
				}
			}
		}
	}

	/// <summary>
	/// Gets the <c>Tile</c> at the given coordinates, this function will decode from
	/// the tile code in the Tiles array to the appropriate Tile.
	/// </summary>
	/// <param name="coords"></param>
	/// <param name="layer"></param>
	/// <returns></returns>
	private Tile GetTile(Position coords, int layer){
		return codesToTiles[tiles[coords.X, coords.Y, layer]];
	}

	/// <summary>
	/// Updates the solid array by reading the data of the given cells.
	/// </summary>
	public void CheckSolid(){
		for (int i=0; i < Width; i++){
			for (int j=0; j < Height; j++){
				for (int k=0; k < NumLayers; k++){
					Tile cellData = GetTile(new Position(i, j), k);
					if (cellData is null){
						break;
					}
					Solid[i,j] = cellData.isSolid || Solid[i,j];
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
	public bool PositionValid(ICollection<Position> occupied){
		bool valid = true;
		foreach (Position position in occupied){
			if (position.X >= 0 && position.X < Width){
				if (position.Y >= 0 && position.Y < Height){
					valid = valid && !Solid[position.X, position.Y];
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
	public void Place(int layer, Position coords, Tile toPlace){
		int code = GetTileAsCode(toPlace);
		Tile permTile = codesToTiles[code];
		tiles[coords.X, coords.Y, layer] = code;
		Solid[coords.X,coords.Y] = Solid[coords.X, coords.Y] || permTile.isSolid;
		tileUpdates.Enqueue(((coords, layer), permTile));
	}

	public void PlaceAll(int startLayer, Position startCoords, Tile[,,] toPlace){
		for (int i = 0; i < toPlace.GetLength(0); i++){
			for (int j = 0; j < toPlace.GetLength(1); j++){
				for (int k = 0; k < toPlace.GetLength(2); k++){
					Place(k + startLayer, new Position(i, j) + startCoords, toPlace[i,j,k]);
				}
			}
		}
	}

	/// <summary>
	/// Returns the code that corresponds to this tile in the <c>Tile</c> array.
	/// If no code currently exists, the a new code will be created and associated with
	/// this tile.
	/// </summary>
	/// <param name="input">The tile to input</param>
	/// <returns>The code associated with this tile.</returns>
	public int GetTileAsCode(Tile input){
		if (!tickets.ContainsKey(input.tileSetRef)){
			AddTicket(input.tileSetRef);
		}
		if (!tileToCodes.ContainsKey(input)){
			int code = tileToCodes.Count();
			input.PopulateTileData(tileSetManager);
			tileToCodes.Add(input, code);
			codesToTiles.Add(code, input);
		}
		return tileToCodes[input];
	}

	/// <summary>
	/// Requests a ticket for a given <c> tileSetRef </c> and then adds the ticket
	/// to the list of active tickets. Additionally stores a dictionary entry to associate
	/// the atlas id of the ticket with the given <c> tileSetRef </c> mainly used for
	/// <see cref="GetTileIdentifier"/> 
	/// </summary>
	/// <param name="tileSetRef">The reference code for the desired tileset.</param>
	private void AddTicket(int tileSetRef){
		ITileSetTicket tileSetTicket = tileSetManager.GetTileSetTicket(tileSetRef);
		tickets.Add(tileSetRef, tileSetTicket);
		atlasCodesToRef.Add(tileSetTicket.GetAtlasId(), tileSetRef);
	}

	/// <summary>
	/// Returns a representation of this <c>LevelCell</c> as an ordered array of bytes. Consists of a
	/// header "palette" and then a body of codes which correspond to entries in the palette.
	/// <para>
	/// The header consists of a list of TileSets (by tileSetRef) and their associated tiles as atlas
	/// coordinates see <see cref="EncodeTileSourceHeader"/>
	/// </para>
	/// <para>
	/// The body consists of a linear list of codes which correspond to tiles via the palette. Codes are
	/// represented by Log_2(UniqueTiles)+1 bits. See <see cref="EncodeTilesBody"/>
	/// </para>
	/// </summary>
	/// <returns>An ordered byte array representing this object.</returns>
	public byte[] Serialize(){
		List<(int, List<Tile>)> tileSetSources = GetUniqueTileList();
		List<byte> headerEncoded = EncodeTileSourceHeader(tileSetSources);
		List<byte> tileCodesBytes = EncodeTilesBody(tileSetSources);
		List<byte> outputList = new List<byte>(tileCodesBytes.Count + headerEncoded.Count);
		outputList.AddRange(headerEncoded);
		outputList.AddRange(tileCodesBytes);
		byte[] outputArray = outputList.ToArray();
		return outputArray;
	}

	/// <summary>
	/// Returns a new instance of an object based on an array of bytes previously created by an
	/// invocation of <see cref="Serialize"/>. 
	/// </summary>
	/// <param name="input">The bytes which represent the <c>LevelCell</c></param>
	/// <returns>A new instance of <c>LevelCell</c> which is a copy of the object 
	/// previously serialized.</returns>
	public static LevelCell Deserialize(byte[] input, TileSetManager tileSetManager){
		LevelCell output = new LevelCell(tileSetManager);
		List<byte> bytes = new List<byte>(input);

		(int, List<(int, List<Tile>)>) tileSetHeader = DecodeTileSourceHeader(bytes);
		List<(int, List<Tile>)> tileSetSources = tileSetHeader.Item2;
		bytes = bytes.GetRange(tileSetHeader.Item1, bytes.Count - tileSetHeader.Item1);

		(int, List<int>) codesBody = DecodeTilesBody(bytes, tileSetSources);
		List<int> codeList = codesBody.Item2;

		Dictionary<Tile, int> tileCodes = GetTileSetCodes(tileSetSources);
		Dictionary<int, Tile> codesToTiles = tileCodes.ToDictionary((i) => i.Value, (i) => i.Key);
		PlaceTileCodes(output, codesToTiles, codeList);
		return output;
	}

	/// <summary>
	/// Encodes the list of tileSources as a list of bytes which represents the given list.
	/// The header starts with 4 bytes which represent the number of TileSets used. 
	/// <para>
	/// Then for each TileSet 4 bytes are written for TileSetRef, then 4 bytes for the 
	/// count of unique tiles for this TileSet, then 1 byte is written for AtlasX and 1 byte 
	/// for AtlasY for each unique tile. 
	/// </para>
	/// <para>
	/// Then the next TileSet follows.
	/// </para>
	/// </summary>
	/// <param name="tileSources">A list representing used tile sets, the first item in the tuple is
	/// the tileSetRef and the second is a list of tile atlas coordinates.</param>
	/// <returns></returns>
	private List<byte> EncodeTileSourceHeader(List<(int, List<Tile>)> tileSources){
		int outputSize = 4  + 8 * tileSources.Count;
		foreach ((int, List<Tile>) tileSource in tileSources){
			outputSize += 2 * tileSource.Item2.Count;
		}
		List<byte> output = new List<byte>(outputSize);
		output.AddRange(BitConverter.GetBytes(tileSources.Count));
		for (int i = 0; i < tileSources.Count; i++){
			output.AddRange(BitConverter.GetBytes(tileSources[i].Item1));
			output.AddRange(BitConverter.GetBytes(tileSources[i].Item2.Count));
			for (int j = 0; j < tileSources[i].Item2.Count; j++){
				output.Add((byte)tileSources[i].Item2[j].atlasX);
				output.Add((byte)tileSources[i].Item2[j].atlasY);
			}
		}
		return output;
	}

	/// <summary>
	/// Decodes a TileSource header previously generated by <see cref="EncodeTileSourceHeader"/>.
	/// <para>
	/// NOTE: This function requires that the header start at byte 0. However, the list of bytes may extend
	/// past the length of the header.
	/// </para>
	/// </summary>
	/// <param name="bytes">A list of bytes to be decoded.</param>
	/// <returns>A tuple consisting of an integer representing the next byte to read, and a list 
	/// representing the TileSourceHeader.</returns>
	private static (int, List<(int, List<Tile>)>) DecodeTileSourceHeader(List<byte> bytes){
		int readHead = 0;
		int sourcesCount = BitConverter.ToInt32(bytes.GetRange(readHead, 4).ToArray());
		readHead += 4;
		List<(int, List<Tile>)> output = new List<(int, List<Tile>)>(sourcesCount);
		for (int i = 0; i < sourcesCount; i++){
			int tileId = BitConverter.ToInt32(bytes.GetRange(readHead, 4).ToArray());
			readHead += 4;
			int uniqueTiles = BitConverter.ToInt32(bytes.GetRange(readHead, 4).ToArray());
			readHead += 4;
			List<Tile> tileList = new List<Tile>();
			for (int j = 0; j < uniqueTiles; j++){
				int X = bytes[readHead];
				readHead++;
				int Y = bytes[readHead];
				readHead++;
				tileList.Add(new Tile(tileId, X, Y));
			}
			output.Add((tileId, tileList));
		}
		return (readHead, output);
	}

	/// <summary>
	/// Encodes the tiles represented in this LevelCell as a list of bytes. First the tiles are converted
	/// to codes by <see cref="GetTilesAsCodeArray"/> and then formatted into a list of bytes. 
	/// </summary>
	/// <param name="tileSources">The list of sources, see <see cref="GetUniqueTileList"/> </param>
	/// <returns>A list of bytes representing the tiles in this <c>LevelCell</c></returns>
	private List<byte> EncodeTilesBody(List<(int, List<Tile>)> tileSources){
		Dictionary<Tile, int> uniqueTiles = GetTileSetCodes(tileSources);
		int tileBits = SerializationHelper.RepresentativeBits(uniqueTiles.Count);
		List<int> tileCodes = GetTilesAsCodeArray(uniqueTiles);
		List<int> tileCodes8Bit = SerializationHelper.ConvertBetweenCodes(tileCodes, tileBits, 8);
		List<byte> tileCodesBytes = new List<byte>(tileCodes8Bit.Count);
		foreach (int entry in tileCodes8Bit){
			tileCodesBytes.Add((byte)entry);
		}
		return tileCodesBytes;
	}

	/// <summary>
	/// Decodes the tiles previously encoded by <see cref="EncodeTilesBody"/>. 
	/// </summary>
	/// <param name="input">The bytes to be decoded.</param>
	/// <param name="tileSources">The list of sources, see <see cref="GetUniqueTileList"/></param>
	/// <returns>A list of tile codes, see <see cref="GetTilesAsCodeArray"/> </returns>
	private static (int, List<int>) DecodeTilesBody(List<byte> input, List<(int, List<Tile>)> tileSources){
		int totalTiles = 1;
		foreach ((int, List<Tile>) entry in tileSources){
			foreach (Tile uniqueTile in entry.Item2){
				totalTiles++;
			}
		}
		int tileBits = SerializationHelper.RepresentativeBits(totalTiles);
		int bytesToRead;
		if (tileBits * Width * Height * NumLayers % 8 == 0){
			bytesToRead = tileBits * Width * Height * NumLayers / 8;
		}
		else {
			bytesToRead = tileBits * Width * Height * NumLayers / 8 + 1;
		}
		List<byte> payload = input.GetRange(0,bytesToRead);
		List<int> payloadInts = new List<int>(payload.Count);
		foreach (byte entry in payload){
			payloadInts.Add(entry);
		}
		List<int> tileCodes = SerializationHelper.ConvertBetweenCodes(payloadInts, 8, tileBits, true);
		return (bytesToRead, tileCodes);
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
	private List<int> GetTilesAsCodeArray(Dictionary<Tile, int> uniqueTiles){
		List<int> tileCodes = new List<int>();
		for (int i = 0; i < NumLayers; i++){
			for (int j = 0; j < Width; j++){
				for (int k = 0; k < Height; k++){
					Tile tileIdentifier = GetTile(new Position(j, k), i);
					int currentCode = uniqueTiles[tileIdentifier];
					tileCodes.Add(currentCode);
				}
			}
		}
		return tileCodes;
	}

	/// <summary>
	/// Places all the tiles in a linear list of tiles, most likely given by <see cref="DecodeTilesBody"/>,
	/// onto a given <c>LevelCell</c>.
	/// </summary>
	/// <param name="toModify">The <c>LevelCell</c> to be modified.</param>
	/// <param name="codesToTiles">A dictionary associating given codes to their associated tiles.</param>
	/// <param name="codeList">The linear list of tiles as codes to be placed.</param>
	private static void PlaceTileCodes
	(
		LevelCell toModify, 
		Dictionary<int, Tile> codesToTiles,
		List<int> codeList
	){
		for (int i = 0; i < codeList.Count; i++){
			int layer = i / (Width * Height);
			int X = i / Height % Width;
			int Y = i % Height;
			Tile currentTile = codesToTiles[codeList[i]];
			if (currentTile.tileSetRef >= 0){
				toModify.Place
				(
					layer, 
					new Position(X, Y), 
					currentTile
				);
			}
		}
	}

	/// <summary>
	/// Returns a list of tuples in which each tileSet is associated with a list of all
	/// the unique tiles which use that tileSet.
	/// <para>
	/// Note that the unique tile for a null value is not included in this list.
	/// </para>
	/// </summary>
	/// <returns>A dictionary with the above referenced structure.</returns>
	private List<(int, List<Tile>)> GetUniqueTileList(){
		Dictionary<int, HashSet<Tile>> tilesByTileSet = new Dictionary<int, HashSet<Tile>>();
		// Count all the unique tiles, grouped into sets by tileSet
		for (int i = 0; i < Width; i++){
			for (int j = 0; j < Height; j++){
				for (int k = 0; k < NumLayers; k++){
					Tile tileType = GetTile(new Position(i, j), k);
					if (tileType.tileSetRef == -1){
						break;
					}
					// Add in a new tileSet for the given tileSetRef and initializes an empty hashset
					if (!tilesByTileSet.ContainsKey(tileType.tileSetRef)){
						HashSet<Tile> tilesForCurrentTileSet = new HashSet<Tile>();
						tilesByTileSet.Add(tileType.tileSetRef, tilesForCurrentTileSet);
					}
					if (!tilesByTileSet[tileType.tileSetRef].Contains(tileType)){
						tilesByTileSet[tileType.tileSetRef].Add(tileType);
					}
				}
			}
		}
		List<(int, List<Tile>)> tileSetSourceList = new List<(int, List<Tile>)>(tilesByTileSet.Count);
		int currentSource = 0;
		foreach (KeyValuePair<int, HashSet<Tile>> tileSet in tilesByTileSet){
			tileSetSourceList.Add((tileSet.Key, new List<Tile>(tileSet.Value.Count)));
			foreach (Tile uniqueTile in tileSet.Value){
				tileSetSourceList[currentSource].Item2.Add(uniqueTile);
			}
			currentSource++;
		}
		return tileSetSourceList;
	}

	/// <summary>
	/// Returns a dictionary associating unique tiles to an integer code.
	/// <para>
	/// Note: The tiles are sorted by tileSet, so the identifier codes will be consecutive for 
	/// tiles in the same tileset.
	/// </para>
	/// </summary>
	/// <param name="sourcesList">The sources list, see <see cref="GetUniqueTileList"/> </param>
	/// <returns>A dictionary associating tiles with a unique identifier.</returns>
	private static Dictionary<Tile, int> GetTileSetCodes(List<(int, List<Tile>)> sourcesList){
		Dictionary<Tile, int> tileCodes = GetDefaultTileCodeDictionary();
		int currentCode = tileCodes.Count;
		for (int i = 0; i < sourcesList.Count; i++){
			for (int j = 0; j < sourcesList[i].Item2.Count; j++){
				tileCodes.Add(sourcesList[i].Item2[j], currentCode);
				currentCode++;
			}
		}
		return tileCodes;
	}

	/// <summary>
	/// Returns a default dictionary for <see cref="GetTileSetCodes"/>, this dictionary just includes the
	/// special tile (-1, (-1, -1)) associated with id 0.
	/// </summary>
	/// <returns>A dictionary as described above.</returns>
	private static Dictionary<Tile, int> GetDefaultTileCodeDictionary(){
		Dictionary<Tile, int> tileCodes = new Dictionary<Tile, int>{
			{Tile.EmptyTile, 0}
		};
		return tileCodes;
	}

	public int GetAtlasId(int tileSetRef){
		return tickets[tileSetRef].GetAtlasId();
	}
}

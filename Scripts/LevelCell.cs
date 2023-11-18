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

	private void AddTicket(int tileSetRef){
		TileSetTicket tileSetTicket = tileSetManager.GetTileSetTicket(tileSetRef);
		tickets.Add(tileSetRef, tileSetTicket);
		atlasCodesToRef.Add(tileSetTicket.GetAtlasId(), tileSetRef);
	}

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
		List<uint> tileCodes = GetTilesAsCodeArray(uniqueTiles);
		return new byte[1];
	}

	private List<uint> GetTilesAsCodeArray(Dictionary<(int, Vector2I), int> uniqueTiles){
		List<uint> tileCodes = new List<uint>();
		for (int i = 0; i < numLayers; i++){
			for (int j = 0; j < Width; j++){
				for (int k = 0; k < Height; k++){
					(int, Vector2I) tileIdentifier = GetTileIdentifier(k, new Vector2I(j, k));
					uint currentCode = (uint)uniqueTiles[tileIdentifier];
					tileCodes.Add(currentCode);
				}
			}
		}
		return tileCodes;
	}

	private byte[] ConvertCodesToBytes(List<uint> input, int codeSize){
		if (codeSize < 1){
			throw new ArgumentException("Code size " + codeSize + " is less than 1.");
		}
		if (input.Count <= 0){
			throw new ArgumentException("Invalid input list size.");
		}
		int numBytes;
		if (codeSize * input.Count % 8 == 0){
			numBytes = input.Count * codeSize / 8;
		}
		else {
			numBytes = input.Count * codeSize / 8 + 1;
		}
		byte[] output = new byte[numBytes];
		byte currentByte = 0;
		uint currentCode = input[0];
		int byteCount = 0;
		int codeCount = 0;
		int byteOffSet = 7;
		int codeOffSet = codeSize - 1;
		while (codeCount < input.Count){
			// There are more bits in the code than a single byte.
			if (byteOffSet < codeOffSet){
				// Shift the current code to align with the current byte head, deleting all lower order bits.
				uint tempCode = currentCode;
				tempCode >>= codeOffSet - byteOffSet;
				
				// Add the bits into the current byte and pass it into the array
				currentByte += (byte)tempCode;
				output[byteCount] = currentByte;
				byteCount++;
				currentByte = 0;
				
				// Shift the bits in tempCode back into place, then remove them
				tempCode <<= codeOffSet - byteOffSet;
				currentCode -= tempCode;

				// Move the heads of the current Byte and current code back into place
				codeOffSet -= byteOffSet + 1;
				byteOffSet = 7;
			}
			// The current code contains exactly the remaining bits for the current byte (how nice!).
			else if (byteOffSet == codeOffSet){
				// Add the bits into the current byte and pass it into the array
				currentByte += (byte)currentCode;
				output[byteCount] = currentByte;
				byteCount++;
				currentByte = 0;

				// No need to pull out a new value if we are at the end of the list
				if (codeCount == input.Count - 1){
					break;
				}

				// Get the next code and reset the offsets.
				codeCount++;
				currentCode = input[codeCount];
				codeOffSet = codeSize - 1;
				byteOffSet = 7;
			}
			// There aren't enough bits available to fill a whole byte
			else if (byteOffSet > codeOffSet){
				// Shift the current code to align with the byte offset
				currentCode <<= byteOffSet - codeOffSet;

				// Adds the available bits to the current byte and shifts the byte offset
				currentByte += (byte)currentCode;
				byteOffSet = byteOffSet - codeOffSet - 1;

				// If this is the last code, add the byte as is and break
				if (codeCount == input.Count - 1){
					output[byteCount] = currentByte;
					break;
				}

				// Retrieve the next code in the list and update the code offset
				codeCount++;
				currentCode = input[codeCount];
				codeOffSet = codeSize - 1;
			}
		}
		return output;
	}

	private (int, Vector2I) GetTileIdentifier(int layer, Vector2I coords){
		int sourceId = tiles.GetCellSourceId(layer, coords);
		if (sourceId == -1){
			return (-1, new Vector2I(-1, -1));
		}
		int sourceRef = atlasCodesToRef[sourceId];
		Vector2I atlasCoords = tiles.GetCellAtlasCoords(layer, coords);
		return (sourceRef, atlasCoords);
	}

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

using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LOM.Levels;

public partial class TileSetManager : Node
{
	class Ticket : TileSetTicket {
		private TileSetManager tileSetManager;
		private int tileSetRef;

		public Ticket(TileSetManager manager, int reference){
			tileSetManager = manager;
			tileSetRef = reference;
			tileSetManager.AddClient(tileSetRef);
		}

		public int GetAtlasId(){
			if (tileSetManager is null){
				throw new NullReferenceException
				(
					"This ticket's TileSetManager is null. Likely the ticket has already been released."
				);
			}
			if (!tileSetManager.atlasSources.ContainsKey(tileSetRef)){
				throw new KeyNotFoundException
				(
					"Ticket's tileSetRef (" + tileSetRef + ") does not have an assigned atlas source."
				);
			}
			return tileSetManager.atlasSources[tileSetRef];
		}

		public void Release(){
			if (tileSetManager is null){
				throw new NullReferenceException
				(
					"This ticket's TileSetManager is null. Likely the ticket has already been released."
				);
			}
			tileSetManager.RemoveClient(tileSetRef);
			tileSetManager = null;
		}
	}

	public int TileWidth = 16;
	public int TileHeight = 16;
	private int atlasSourceHead = 0;
	private static readonly string basePath = "res://Tilesets/";
	private static readonly string fileExtension = ".tres";
	private static readonly string defaultTileSetName = "Default";
	//private TileSet defaultTileSet;
	private Dictionary<string, int> tileSetCodes = new Dictionary<string, int>(){
		{"Forest", 1},
		{"Elf_Buildings", 2}
	};

	private Dictionary<int, string> codesToTileSets;

	/// <summary>
	/// Describes the relationship between <c>tileSetCodes</c> (the permanent link to a tileset)
	/// and the corresponding atlas position in the current <c>TileSet</c>.
	/// </summary>
	private Dictionary<int, int> atlasSources = new Dictionary<int, int>();

	/// <summary>
	/// Counts the number of clients for a given <c>TileSet</c> as indicated by <c>tileSetCode</c>.
	/// </summary>
	private Dictionary<int, int> tileSetClients = new Dictionary<int, int>();
	private TileSet masterTileSet;

	public TileSetManager() : base() {
		//defaultTileSet = ResourceLoader.Load<TileSet>(basePath + "Default" + fileExtension);
		masterTileSet = ResourceLoader.Load<TileSet>
		(
			basePath + defaultTileSetName + fileExtension,
			null,
			ResourceLoader.CacheMode.Ignore
		);
		codesToTileSets = new Dictionary<int, string>();
		foreach (KeyValuePair<string, int> entry in tileSetCodes) {
			codesToTileSets.Add(entry.Value, entry.Key);
		}
	}

	public TileSet GetDefaultTileSet(){
		return masterTileSet;
	}

	/// <summary>
	/// Returns the reference code for a tileset with the given name.
	/// </summary>
	/// <param name="refName"></param>
	/// <returns></returns>
	public int GetTileSetCode(string refName){
		int code = -1;
		try {
			code = tileSetCodes[refName];
		}
		catch (Exception){
		}
		return code;
	}

	public TileData GetTileData(Tile tile){
		if (!atlasSources.ContainsKey(tile.tileSetRef)){
			throw new ArgumentException("TilesetRef " + tile.tileSetRef + " is not associated with an atlas source.");
		}
		int atlasSource = atlasSources[tile.tileSetRef];
		TileSetAtlasSource tileSetAtlasSource = (TileSetAtlasSource)masterTileSet.GetSource(atlasSource);
		return tileSetAtlasSource.GetTileData(new Vector2I(tile.atlasX, tile.atlasY), 0);
	}

	public TileSetTicket GetTileSetTicket(int tileSetRef){
		return new Ticket(this, tileSetRef);
	}

	private void AddClient(int tileSetRef){
		if (!codesToTileSets.ContainsKey(tileSetRef)){
			throw new ArgumentException
			(
				"TileSetRef " + tileSetRef + " does not correspond to a valid TileSet."
			);
		}
		if (!atlasSources.ContainsKey(tileSetRef)){
			atlasSources.Add(tileSetRef, atlasSourceHead);
			atlasSourceHead++;
			MergeTileSet(tileSetRef);
		}
		if (!tileSetClients.ContainsKey(tileSetRef)){
			tileSetClients.Add(tileSetRef, 1);
		}
		else {
			if (tileSetClients[tileSetRef] == 0){
				MergeTileSet(tileSetRef);
			}
			tileSetClients[tileSetRef]++;
		}
	}

	private void RemoveClient(int tileSetRef){
		if (!atlasSources.ContainsKey(tileSetRef)){
			throw new ArgumentException
			(
				"Attempted to remove " + tileSetRef + ", but it has not been assigned an atlas source."
			);
		}
		if (!tileSetClients.ContainsKey(tileSetRef)){
			throw new ArgumentException
			(
				"Attempted to remove " + tileSetRef + ", but it is not a counted TileSet client."
			);
		}
		tileSetClients[tileSetRef]--;
		if (tileSetClients[tileSetRef] == 0){
			masterTileSet.RemoveSource(atlasSources[tileSetRef]);
			atlasSources.Remove(tileSetRef);
		}
	}

	private void MergeTileSet(int tileSetRef){
		if (!codesToTileSets.ContainsKey(tileSetRef)){
			throw new ArgumentException
			(
				"TileSetRef " + tileSetRef + " does not correspond to a managed TileSet."
			);
		}
		if (!atlasSources.ContainsKey(tileSetRef)){
			throw new ArgumentException
			(
				"TileSetRef " + tileSetRef + " has not been assigned an atlas source id."
			);
		}
		TileSet toAdd = ResourceLoader.Load<TileSet>
		(
			basePath + codesToTileSets[tileSetRef] + fileExtension,
			null,
			ResourceLoader.CacheMode.Ignore
		);
		masterTileSet.AddSource(toAdd.GetSource(0), atlasSources[tileSetRef]);
	}


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

}

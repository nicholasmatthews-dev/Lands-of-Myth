using Godot;
using System;
using System.Collections.Generic;

public partial class TileSetManager : Node
{
	public int TileWidth = 16;
	public int TileHeight = 16;
	private static readonly string basePath = "res://Tilesets/";
	private static readonly string fileExtension = ".tres";
	private TileSet defaultTileSet;
	private Dictionary<string, int> tileSetCodes = new Dictionary<string, int>(){
		{"Forest", 1},
		{"Elf_Buildings", 2}
	};

	private Dictionary<int, string> codesToTileSets;
	private Dictionary<int, TileSet> tileSetReferences = new Dictionary<int, TileSet>();

	public TileSetManager() : base() {
		defaultTileSet = ResourceLoader.Load<TileSet>(basePath + "Default" + fileExtension);
		codesToTileSets = new Dictionary<int, string>();
		foreach (KeyValuePair<string, int> entry in tileSetCodes) {
			codesToTileSets.Add(entry.Value, entry.Key);
		}
	}

	public TileSet GetDefaultTileSet(){
		return (TileSet)defaultTileSet.Duplicate();
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

	/// <summary>
	/// Attempts to get a reference to the tileset source specified by code.
	/// The source will always be taken from the first atlas (source id 0) of the
	/// referenced tileset.
	/// <para>A tileset will be loaded from disk if no existing reference is found.</para>
	/// </summary>
	/// <param name="code"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentException"></exception>
	public TileSetSource GetTileSetSource(int code){
		if (!codesToTileSets.ContainsKey(code)){
			throw new ArgumentException(code + " is not a valid tileset code.");
		}
		if (tileSetReferences.ContainsKey(code)){
			return (TileSetSource)tileSetReferences[code].GetSource(0).Duplicate();
		}
		string pathName = basePath + codesToTileSets[code] + fileExtension;
		TileSet output = ResourceLoader.Load<TileSet>(pathName);
		tileSetReferences.Add(code, output);
		return (TileSetSource)output.GetSource(0).Duplicate(true);
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

using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LOM.Levels;

public partial class LevelCellNode : Node2D {
    private static bool Debugging = false;
    /// <summary>
    /// The <c>LevelCell</c> that this Node references and represents.
    /// </summary>
    public LevelCell referenceCell;
    /// <summary>
    /// The number of Tiles that can be loaded in a single frame, used to throttle loading
    /// for performance reasons.
    /// </summary>
    private static int tilesPerFrame = 1000;
    /// <summary>
    /// The TileMap which contains all the Tiles the LevelCell represents.
    /// </summary>
    private TileMap tileMap = new();

    private Dictionary<int, ITileSetTicket> tileSetTickets = new();
    private Dictionary<int, int> atlasCodes = new();

    private Queue<(Position, int)> invalidTileQueue = new();

    private TileSetManager tileSetManager;

    public LevelCellNode(LevelCell referenceCell, TileSetManager tileSetManager) : base(){
        this.tileSetManager = tileSetManager;
        this.referenceCell = referenceCell;
    }

    public override void _Ready(){
        tileMap.TileSet = tileSetManager.GetDefaultTileSet();
        tileMap.AddLayer(-1);
        tileMap.AddLayer(-1);
        InvalidateAllTiles();
        AddChild(tileMap);
    }

    public override void _Process(double delta)
    {
        //PlaceQueuedTiles(); 
        GetInvalidTiles();       
    }

    /// <summary>
    /// Invalidates all the tiles in this <see cref="LevelCell"/> and adds them
    /// to the invalid tile queue for retrieval.
    /// </summary>
    private void InvalidateAllTiles(){
        for (int i = 0; i < LevelManager.CellWidth; i++){
            for (int j = 0; j < LevelManager.CellHeight; j++){
                for (int k = 0; k < LevelCell.NumLayers; k++){
                    invalidTileQueue.Enqueue((new Position(i,j), k));
                }
            }
        }
    }

    /// <summary>
    /// Attempts to load in all tiles that have been marked invalid and are in the 
    /// invalid tile queue. This is limited by the number of tiles that can be loaded
    /// per frame.
    /// </summary>
    private void GetInvalidTiles() {
        int tilesLoaded = 0;
        while (tilesLoaded <= tilesPerFrame){
            if (invalidTileQueue.Count <= 0){
                break;
            }
            (Position, int) coords = invalidTileQueue.Dequeue();
            Tile toPlace = referenceCell.GetTile(coords.Item1, coords.Item2);
            Place(coords.Item2, coords.Item1, toPlace);
            tilesLoaded++;
        }
    }

    /// <summary>
    /// Attempts to place all of the tiles in the Queue of tile updates contained in the
    /// referenced LevelCell object.
    /// </summary>
    public void PlaceQueuedTiles(){
        if (referenceCell.tileUpdates.Count <= 0){
            return;
        }
        int tilesLoaded = 0;
        while (tilesLoaded <= tilesPerFrame){
            if (referenceCell.tileUpdates.TryDequeue(out ((Position, int), Tile) tileUpdate))
            {
                int layer = tileUpdate.Item1.Item2;
                Position coords = tileUpdate.Item1.Item1;
                Tile tile = tileUpdate.Item2;
                Place(layer, coords, tile);
                tilesLoaded++;
            }
            else {
                break;
            }
        }
    }

    /// <summary>
    /// Places a Tile on the TileMap at the given coordinates.
    /// </summary>
    /// <param name="layer">The layer to place on.</param>
    /// <param name="coords">The coordinates of the tile to place.</param>
    /// <param name="tile">The tile to be placed.</param>
    private void Place(int layer, Position coords, Tile tile){
        if (tile == Tile.EmptyTile){
            return;
        }
        int sourceId = GetAtlasId(tile);
        tileMap.SetCell
        (
            layer, 
            new Vector2I(coords.X, coords.Y), 
            sourceId, 
            new Vector2I(tile.atlasX, tile.atlasY)
        );
    }

    private int GetAtlasId(Tile tile){
        if (!tileSetTickets.ContainsKey(tile.tileSetRef)){
            AddTileSetTicket(tile.tileSetRef);
        }
        return atlasCodes[tile.tileSetRef];
    }

    private void AddTileSetTicket(int tileSetRef){
        ITileSetTicket ticket = tileSetManager.GetTileSetTicket(tileSetRef);
        tileSetTickets.Add(tileSetRef, ticket);
        atlasCodes.Add(tileSetRef, ticket.GetAtlasId());
    }

}
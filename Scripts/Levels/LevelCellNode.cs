using Godot;
using System;
using System.Diagnostics;

namespace LOM.Levels;

public partial class LevelCellNode : Node2D {
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

    public LevelCellNode(LevelCell referenceCell) : base(){
        this.referenceCell = referenceCell;
    }

    public override void _Ready(){
        tileMap.TileSet = GameModel.tileSetManager.GetDefaultTileSet();
        tileMap.AddLayer(-1);
        tileMap.AddLayer(-1);
        AddChild(tileMap);
    }

    public override void _Process(double delta)
    {
        PlaceQueuedTiles();        
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
            if (referenceCell.tileUpdates.TryDequeue(out ((int, int, int), Tile) tileUpdate))
            {
                int layer = tileUpdate.Item1.Item3;
                (int, int) coords = (tileUpdate.Item1.Item1, tileUpdate.Item1.Item2);
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
    private void Place(int layer, (int, int) coords, Tile tile){
        int sourceId = referenceCell.GetAtlasId(tile.tileSetRef);
        tileMap.SetCell
        (
            layer, 
            new Vector2I(coords.Item1, coords.Item2), 
            sourceId, 
            new Vector2I(tile.atlasX, tile.atlasY)
        );
    }

}
using Godot;
using System;
using System.Diagnostics;

namespace LOM.Levels;

public partial class LevelCellNode : Node2D {

    public LevelCell referenceCell;
    private static int tilesPerFrame = 1000;
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
using System;
using Godot;

namespace LOM.Levels;

public class Tile{
    public static readonly Tile EmptyTile = new(-1, -1, -1);
    public readonly int tileSetRef;
    public readonly int atlasX;
    public readonly int atlasY;
    public bool isSolid = false;
    private TileData tileData;

    public Tile(int tileSet, int X, int Y){
        tileSetRef = tileSet;
        atlasX = X;
        atlasY = Y;
    }

    public void PopulateTileData(TileSetManager tileSetManager){
        tileData = tileSetManager.GetTileData(this);
        isSolid = tileData.GetCustomData("Solid").AsBool();
    }

    public override bool Equals(object obj)
    {
        if (obj is not Tile){
            return false;
        }
        Tile other = (Tile)obj;
        return (other.tileSetRef, other.atlasX, other.atlasY).Equals((tileSetRef, atlasX, atlasY));
    }

    public override int GetHashCode()
    {
        return (tileSetRef, atlasX, atlasY).GetHashCode();
    }

    public override string ToString()
    {
        return "(Tile: (RefId: " + tileSetRef + ", Coords: (" + atlasX + ", " + atlasY + "))"; 
    }

}
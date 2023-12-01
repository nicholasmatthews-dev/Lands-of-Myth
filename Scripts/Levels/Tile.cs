using System;

namespace LOM.Levels;

public class Tile{
    public static readonly Tile EmptyTile = new(-1, -1, -1);
    public readonly int tileSetRef;
    public readonly int atlasX;
    public readonly int atlasY;
    public bool isSolid = false;

    public Tile(int tileSet, int X, int Y){
        tileSetRef = tileSet;
        atlasX = X;
        atlasY = Y;
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

}
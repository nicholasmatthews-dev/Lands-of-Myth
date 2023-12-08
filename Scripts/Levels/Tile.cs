using System;
using Godot;

namespace LOM.Levels;

public class Tile{
    /// <summary>
    /// A special Tile representing the empty tile, given by Godot's TileMap definition of the
    /// empty tile.
    /// </summary>
    public static readonly Tile EmptyTile = new(-1, -1, -1);
    /// <summary>
    /// The permanent reference code to the TileSet that this Tile uses.
    /// </summary>
    public readonly int tileSetRef;
    /// <summary>
    /// The X coordinate of this Tile's atlas coordinates.
    /// </summary>
    public readonly int atlasX;
    /// <summary>
    /// The Y coordinate of this Tile's atlas coordinates.
    /// </summary>
    public readonly int atlasY;
    /// <summary>
    /// Whether this tile is considered solid. Use <see cref="PopulateTileData"/> to ensure
    /// that this value is correctly synced with the TileSet resource.
    /// </summary>
    public bool isSolid = false;
    /// <summary>
    /// The data associated with this Tile, taken from its TileSet. This may be null unless
    /// <see cref="PopulateTileData"/> is called.
    /// </summary>
    private TileData tileData;

    public Tile(int tileSet, int X, int Y){
        tileSetRef = tileSet;
        atlasX = X;
        atlasY = Y;
    }

    /// <summary>
    /// Populates the custom data associated with this Tile from the TileSet resource. This may be slow,
    /// so this function is not included in the constructor.
    /// </summary>
    /// <param name="tileSetManager">The TileSetManager to be used for retrieving the data.</param>
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
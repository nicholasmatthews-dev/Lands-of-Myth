using Godot;
using LOM.Levels;
using System;
using System.Threading.Tasks;

namespace LOM.Spaces;

public abstract class Space{
    protected const FileAccess.CompressionMode compressionMode = FileAccess.CompressionMode.GZip;

    /// <summary>
    /// Returns a <c>LevelCell</c> for the given cell coordinates. Can either be generated 
    /// or returned from a file.
    /// </summary>
    /// <param name="cellCoords">The coordinates of the request in cell space.</param>
    /// <returns>The <c>LevelCell</c> at the given coordinates.</returns>
    public abstract Task<LevelCell> GetLevelCell(Vector2I cellCoords);

    /// <summary>
    /// Stores the given <c>LevelCell</c> (given as a byte array via <c>LevelCell.Serialize()</c>)
    /// to the given cell coordinates. Used to persist cells.
    /// <para>
    /// NOTE: The implementation of this function is left to the implementing class, therefore
    /// no given save structure can be garuanteed across different <c>Space</c> implementations.
    /// </para>
    /// </summary>
    /// <param name="cellToStore"></param>
    /// <param name="cellCoords"></param>
    public abstract void StoreBytesToCell(byte[] cellToStore, Vector2I cellCoords);
}
using Godot;
using System;

public abstract class Space{
    public abstract LevelCell GetLevelCell(Vector2I cellCoords);
    public abstract void StoreBytesToCell(byte[] cellToStore, Vector2I cellCoords);
}
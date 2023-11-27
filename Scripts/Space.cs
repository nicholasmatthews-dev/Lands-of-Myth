using Godot;
using System;

public abstract class Space{
    protected static string rootFolder = "user://Save/world/";
    public abstract LevelCell GetLevelCell(Vector2I cellCoords);
    public abstract void StoreBytesToCell(byte[] cellToStore, Vector2I cellCoords);
}
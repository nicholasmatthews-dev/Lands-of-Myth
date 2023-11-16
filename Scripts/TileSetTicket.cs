using Godot;
using System;

public interface TileSetTicket {
    public int GetAtlasId();
    public void Release();
}
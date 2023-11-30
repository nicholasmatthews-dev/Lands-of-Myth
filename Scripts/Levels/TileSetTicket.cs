using Godot;
using System;

namespace LOM.Levels;
public interface TileSetTicket {
    public int GetAtlasId();
    public void Release();
}
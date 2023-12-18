using Godot;
using System;

namespace LOM.Levels;
public interface ITileSetTicket {
    public int GetAtlasId();
    public void Release();
}
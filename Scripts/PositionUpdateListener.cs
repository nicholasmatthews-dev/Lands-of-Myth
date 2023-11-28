using Godot;
using System;

namespace Control;

public interface PositionUpdateListener{
    public void OnPositionUpdate(Vector2I coords);
}
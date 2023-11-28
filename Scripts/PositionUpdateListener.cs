using Godot;
using System;

namespace Control;

public interface PositionUpdateListener{
    /// <summary>
    /// Called when the event source updates the position of some object on the tile grid.
    /// Passes the new coordinates to the listener.
    /// </summary>
    /// <param name="coords">The new coordinates of some reference object.</param>
    public void OnPositionUpdate(Vector2I coords);
}
using System;
using LOM.Levels;

namespace LOM.Control;

public interface IPositionUpdateListener{
    /// <summary>
    /// Called when the event source updates the position of some object on the tile grid.
    /// Passes the new coordinates to the listener.
    /// </summary>
    /// <param name="coords">The new coordinates of some reference object.</param>
    public void OnPositionUpdate(WorldPosition coords);
}
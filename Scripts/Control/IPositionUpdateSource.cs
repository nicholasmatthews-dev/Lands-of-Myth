using Godot;

namespace LOM.Control;

public interface IPositionUpdateSource {
    public void AddPositionUpdateListener(PositionUpdateListener listener);
}
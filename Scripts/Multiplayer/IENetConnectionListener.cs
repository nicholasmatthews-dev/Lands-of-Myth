using Godot;

namespace LOM.Multiplayer;

/// <summary>
/// Represents an interface for an object that handles connection events on an ENetService.
/// </summary>
public interface IENetConnectionListener {
    /// <summary>
    /// Handles a connection event from the given peer.
    /// </summary>
    /// <param name="peer">The peer which triggered the connection event.</param>
    public void HandleConnection(ENetPacketPeer peer);
}
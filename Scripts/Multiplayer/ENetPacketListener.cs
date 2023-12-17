using Godot;

namespace LOM.Multiplayer;

/// <summary>
/// Represents a listener which can retrieve ENetPackets.
/// </summary>
public interface ENetPacketListener {
    /// <summary>
    /// Represents the action that this ENetPacketListener will take when a packet
    /// is received. The packet and the associated peer are passed as parameters.
    /// </summary>
    /// <param name="packet">A byte array representing the received packet.</param>
    /// <param name="peer">The ENetPacketPeer which sent the received packet.</param>
    public void ReceivePacket(byte[] packet, ENetPacketPeer peer);
}
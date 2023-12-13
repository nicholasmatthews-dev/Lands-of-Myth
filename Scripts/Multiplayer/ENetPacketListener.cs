using Godot;

namespace LOM.Multiplayer;

public interface ENetPacketListener {
    public void ReceivePacket(byte[] packet, ENetPacketPeer peer);
}
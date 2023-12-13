using Godot;

namespace LOM.Multiplayer;

public abstract class ENetPacketListener {
    private int channel = 0;

    public abstract void ReceivePacket(byte[] packet, ENetPacketPeer peer);
}
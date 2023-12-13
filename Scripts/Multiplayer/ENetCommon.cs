using Godot;

namespace LOM.Multiplayer;

public static class ENetCommon {
    public static ENetConnection.CompressionMode compressionMode = ENetConnection.CompressionMode.Zlib;
    public static int channels = 16;
    public enum ChannelNames {
        Spaces = 1
    }
}
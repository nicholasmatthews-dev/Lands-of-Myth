using Godot;

namespace LOM.Multiplayer;

/// <summary>
/// Represents common data for ENet related classes.
/// </summary>
public static class ENetCommon {
    /// <summary>
    /// Represents the compression mode that <see cref="ENetService"/>s will use to communicate. 
    /// </summary>
    public static ENetConnection.CompressionMode compressionMode = ENetConnection.CompressionMode.Zlib;
    /// <summary>
    /// Represents the total number of channels to be used on the <see cref="ENetConnection"/>. 
    /// </summary>
    public static int channels = 16;
    /// <summary>
    /// Represents the name and associated channel number for each communication channel.
    /// </summary>
    public enum ChannelNames {
        /// <summary>
        /// The communication channel for exchanging <see cref="Spaces.Space"/>s.
        /// </summary>
        Spaces = 1
    }
}
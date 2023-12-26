namespace LOM.Levels;

public interface ILevelManager {
    /// <summary>
    /// Connects this <see cref="ILevelManager"/> to an <see cref="ILevelHost"/> which will provide
    /// it with the requiste <see cref="LevelCell"/>s. 
    /// </summary>
    /// <param name="levelHost">The host to connect to.</param>
    public void ConnectLevelHost(ILevelHost levelHost);

    /// <summary>
    /// Disconnects this <see cref="ILevelManager"/> from a given <see cref="ILevelHost"/>.  
    /// </summary>
    /// <param name="levelHost">The host to disconnect from.</param>
    public void DisconnectLevelHost(ILevelHost levelHost);
}
using System.Threading.Tasks;

namespace LOM.Levels;

/// <summary>
/// Represents a level host interface, implementing classes provide a way of retrieving 
/// <see cref="LevelCell">s given <see cref="LevelCellRequest"/>s. 
/// </summary>
public interface ILevelHost {
    /// <summary>
    /// Attempts to get a level cell using the specified request. Will return a task which
    /// can be awaited to get the completed <see cref="LevelCellRequest"/>. 
    /// </summary>
    /// <param name="request">The request for a given level cell.</param>
    /// <returns>A task which will return the completed request.</returns>
    public Task<LevelCellRequest> GetLevelCell(LevelCellRequest request);

    /// <summary>
    /// Signals that a <see cref="ILevelManager"/> is no longer is using a given 
    /// <see cref="LevelCell"/> specified in the <see cref="LevelCellRequest"/>.
    /// </summary>
    /// <param name="levelManager">The <see cref="ILevelManager"/> which is calling this method.</param>
    /// <param name="request">The <see cref="LevelCellRequest"/> that specifies the cell to be disposed
    /// of.</param>
    public void SignalDispose(ILevelManager levelManager, LevelCellRequest request);

    /// <summary>
    /// Connects an <see cref="ILevelManager"/> which will use this object. 
    /// </summary>
    /// <param name="levelManager">The <see cref="ILevelManager"/> to connect.</param>
    public void ConnectManager(ILevelManager levelManager);

    /// <summary>
    /// Disconnects an <see cref="ILevelManager"/> from this object. 
    /// </summary>
    /// <param name="levelManager">The <see cref="ILevelManager"/> which wishes to disconnect.</param>
    public void DisconnectManager(ILevelManager levelManager);
}
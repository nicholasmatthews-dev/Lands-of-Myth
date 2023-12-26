using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using LOM.Spaces;
using System.Diagnostics;

namespace LOM.Levels;

public class LevelHost : ILevelHost
{
    private static bool Debugging = false;
    private static readonly int MinQueueSize = 10;
    private static readonly int MaxQueueSize = 1000;
    private static readonly int CellsPerClient = 10;
    private int currentMaxQueueSize = MinQueueSize;
    private HashSet<ILevelManager> levelManagers = new();
    private object tokenToSpacesLock = new();
    private Dictionary<SpaceToken, Space> tokensToSpaces = new();
    private object loadedCellsLock = new();
    private Dictionary<(Space, CellPosition), LevelCell> loadedCells = new();
    private object loadedSpaceQueueLock = new();
    private Queue<(Space, CellPosition)> loadedSpaceQueue = new();
    private TileSetManager tileSetManager;

    public LevelHost(TileSetManager tileSetManager){
        this.tileSetManager = tileSetManager;
    }

    public Task<LevelCellRequest> GetLevelCell(LevelCellRequest request)
    {
        return Task.Run(() => {
            SpaceToken token = request.requestSpace;
            CellPosition coords = request.coords;
            lock (tokenToSpacesLock){
                if (!tokensToSpaces.ContainsKey(token)){
                    AddSpaceFromToken(token);
                }
            }
            LevelCell cell = null;
            if (!loadedCells.ContainsKey((tokensToSpaces[token], coords))){
                cell = LoadCell(request);
            }
            else {
                cell = GetLoadedCell(request);
            }
            request.payload = cell;
            request.requestStatus = LevelCellRequest.RequestStatus.Fulfilled;
            return request;
        });
    }

    /// <summary>
    /// Gets a cell that has previously been loaded and currently exists in memory.
    /// </summary>
    /// <param name="request">The <see cref="LevelCellRequest"/> to fulfill.</param>
    /// <returns>A <see cref="LevelCell"/> fulfilling the request.</returns>
    private LevelCell GetLoadedCell(LevelCellRequest request){
        Space requestSpace = tokensToSpaces[request.requestSpace];
        CellPosition coords = request.coords;
        LevelCell output = null;
        lock(loadedCellsLock){
            output = loadedCells[(requestSpace, coords)];
        }
        return output;
    }

    /// <summary>
    /// Loads a cell in from the appropriate <see cref="Space"/>. 
    /// </summary>
    /// <param name="request">The request to fulfill.</param>
    /// <returns>A <see cref="LevelCell"/> satisfying the request.</returns>
    public LevelCell LoadCell(LevelCellRequest request){
        if (Debugging) Debug.Print("LevelHost: Loading cell from request " + request);
        Space requestSpace = tokensToSpaces[request.requestSpace];
        Task<LevelCell> retrieveTask = requestSpace.GetLevelCell(request.coords);
        retrieveTask.Wait();
        LevelCell levelCell = retrieveTask.Result;
        lock(loadedCellsLock){
            loadedCells.Add((requestSpace, request.coords), levelCell);
        }
        EnqueueCellLoad((requestSpace, request.coords));
        return levelCell;
    }

    /// <summary>
    /// Enqueues a cell into the loaded cells queue, and removes the oldest cell if
    /// the queue is over capacity.
    /// </summary>
    /// <param name="update">The identifier for the load to queue.</param>
    private void EnqueueCellLoad((Space, CellPosition) update){
        int loadedCount;
        lock(loadedSpaceQueueLock){
            loadedCount = loadedSpaceQueue.Count;
            loadedSpaceQueue.Enqueue(update);
        }
        if (loadedCount > currentMaxQueueSize){
            UnloadCell();
        }
    }

    /// <summary>
    /// Unloads the oldest cell from the loadedSpaceQueue.
    /// </summary>
    private void UnloadCell(){
        (Space, CellPosition) key;
        lock (loadedSpaceQueueLock){
            key = loadedSpaceQueue.Dequeue();
        }
        if (Debugging) Debug.Print("LevelHost: Unloading cell with key: "+ key);
        lock(loadedCellsLock){
            loadedCells.Remove(key);
        }
    }

    /// <summary>
    /// Adds in a space from a given <see cref="SpaceToken"/> will throw an error if the token cannot
    /// be resolved to a known implementing type of <see cref="SpaceToken"/>. 
    /// </summary>
    /// <param name="token">The <see cref="SpaceToken"/> to resolve.</param>
    /// <exception cref="Exception">If the token cannot be resolved to a known type.</exception>
    public void AddSpaceFromToken(SpaceToken token){
        if (token is WorldSpaceToken) {
            WorldSpaceToken worldSpaceToken = (WorldSpaceToken)token;
            WorldSpace worldSpace = new(worldSpaceToken.spaceName, tileSetManager);
            tokensToSpaces.Add(token, worldSpace);
        }
        else {
            throw new Exception("LevelHost: Space token " + token + " does not correspond to a valid type.");
        }
    }

    private void ResizeQueue(){
        currentMaxQueueSize = Math.Min(
            MaxQueueSize, 
            Math.Max(MinQueueSize, levelManagers.Count * CellsPerClient)
        );
        while (loadedSpaceQueue.Count > currentMaxQueueSize){
            UnloadCell();
        }
    }

    public void SignalDispose(ILevelManager levelManager, LevelCellRequest request)
    {
        throw new NotImplementedException();
    }

    public void ConnectManager(ILevelManager levelManager)
    {
        levelManagers.Add(levelManager);
        ResizeQueue();
    }

    public void DisconnectManager(ILevelManager levelManager)
    {
        levelManagers.Remove(levelManager);
        ResizeQueue();
    }
}
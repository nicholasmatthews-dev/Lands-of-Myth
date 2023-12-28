using LOM.Control;
using LOM.Spaces;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace LOM.Levels;

/// <summary>
/// Represents a manager which is responsible for retrieving the appropriate
/// <c>LevelCells</c> from its active <c>Space</c>.
/// </summary>
public partial class LevelManager : IPositionUpdateListener, ILevelManager
{
	private static bool Debugging = true;
	/// <summary>
	/// The height of a single tile in the tileset (in pixels).
	/// </summary>
	public static int TileHeight = 16;

	/// <summary>
	/// The width of a signle tile in the tileset (in pixels).
	/// </summary>
	public static int TileWidth = 16;

	/// <summary>
	/// The height of a single map cell (in tiles).
	/// </summary>
	public static int CellHeight = 64;

	/// <summary>
	/// The width of a single map cell (in tiles).
	/// </summary>
	public static int CellWidth = 64;

	/// <summary>
	/// The currently active space that this LevelManager is representing.
	/// </summary>
	//private Space activeSpace;

	/// <summary>
	/// The <see cref="SpaceToken"/> for the currently active space.
	/// </summary>
	private SpaceToken spaceToken;

	/// <summary>
	/// The <see cref="ILevelHost"/> that this <see cref="LevelManager"/> will draw its
	/// <see cref="LevelCell"/>s from. 
	/// </summary>
	private ILevelHost levelHost;

	/// <summary>
	/// The last center (in cell coordinates) for the LevelManager's loaded cells.
	/// </summary>
	private CellPosition lastPosition = new(0,0);

	/// <summary>
	/// The collection of active map cells, 
	/// indexed by their position (counted by number of cells from the origin).
	/// </summary>
	private ConcurrentDictionary<CellPosition,LevelCell> activeCells = new();

	/// <summary>
	/// A queue that holds all the updates to levelCells that have not been processed by the node
	/// which listens to this object. The form is (bool removed, Vector2I coords, LevelCell newCell).
	/// </summary>
	public ConcurrentQueue<(bool, CellPosition, LevelCell)> levelCellUpdates = new();

	/// <summary>
	/// The thread which handles all the processing for this LevelManager, this is to keep the
	/// execution time of loading and saving cells from impacting frame rate.
	/// </summary>
	private Thread processThread;
	/// <summary>
	/// Whether the process thread should be kept alive or finish its work after its next cycle.
	/// </summary>
	private bool keepAlive = true;
	/// <summary>
	/// A lock used to acquire read/write access to new position.
	/// </summary>
	private object newPositionLock = new();
	/// <summary>
	/// The newest position from OnPositionUpdate (in cell coordinates).
	/// </summary>
	private CellPosition newPosition = new(0,0);
	/// <summary>
	/// The handle for registering whether the thread should be resumed to respond to a position update.
	/// </summary>
	private EventWaitHandle positionUpdateHandle = new(false, EventResetMode.AutoReset);

	public LevelManager(){
        processThread = new Thread(Process)
        {
            IsBackground = true,
			Name = "LevelManagerProcess"
        };
        processThread.Start();
	}

	public void RegisterPostionUpdateSource(IPositionUpdateSource source){
		source.AddPositionUpdateListener(this);
	}

	/// <summary>
	/// The core functionality of the thread for this LevelManager, waits until a position update
	/// is signaled and then handles the position update.
	/// </summary>
	private void Process(){
		while (true){
			positionUpdateHandle.WaitOne();
			HandlePositionUpdate();
			positionUpdateHandle.Reset();
			if (!keepAlive){
				return;
			}
		}
	}

	/// <summary>
	/// Handles the last position update to occur, will change the loaded cells to center on the
	/// new position if it differs from the previous center.
	/// </summary>
	private void HandlePositionUpdate(){
		lock(newPositionLock){
			if (newPosition != lastPosition){
				if (Debugging) Debug.Print("LevelManager: New position " + newPosition + " differs from " + lastPosition);
				lastPosition = newPosition;
				ChangeLoadedCells(lastPosition);
			}
		}
	}

	/// <summary>
	/// Changes the currently active <c>Space</c> that this <c>LevelManager</c> is handling.
	/// </summary>
	/// <param name="newSpace">The <c>Space</c> to switch to.</param>
	/// <param name="newPosition">The center (in cell coordinates) for this <c>LevelManager</c>.</param>
	public void ChangeActiveSpace(SpaceToken newSpace, CellPosition newPosition){
		if (Debugging) Debug.Print("LevelManager: Change active space called from " 
		+ newSpace + " around " + newPosition);
		spaceToken = newSpace;
		lastPosition = newPosition;
		ChangeLoadedCells(lastPosition);
	}

	public void OnPositionUpdate(WorldPosition coords){
		lock(newPositionLock){
			newPosition = coords.GetCellCoords(CellWidth, CellHeight).Item1;
		}
		positionUpdateHandle.Set();
	}

	/// <summary>
	/// Updates the currently loaded cells so they are centered on the given cooridinates in cell space.
	/// <para>
	/// That is, all cells in a 3x3 square centered around the given cooridinates will be loaded, and any
	/// cells which were previously outside of the space will be unloaded.
	/// </para>
	/// </summary>
	/// <param name="coords">The new coordinates of the center of the loaded cells.</param>
	private void ChangeLoadedCells(CellPosition coords){
		if (Debugging) Debug.Print("LevelManager: ChangeLoadedCells called around " + coords);
		List<CellPosition> cellsToRemove = new List<CellPosition>(9);
		foreach (KeyValuePair<CellPosition, LevelCell> entry in activeCells){
			if (Math.Abs(coords.X - entry.Key.X) > 1 
			|| Math.Abs(coords.Y - entry.Key.Y) > 1){
				cellsToRemove.Add(entry.Key);
			}
		}
		foreach (CellPosition entry in cellsToRemove){
			DisposeOfCell(entry);
		}
		for (int i = -1; i < 2; i++){
			for (int j = -1; j < 2; j++){
				CellPosition currentCoords = new CellPosition(i + coords.X, j + coords.Y);
				LoadLevelCellFromSpace(currentCoords);
			}
		}
	}

	/// <summary>
	/// Loads in a LevelCell at the given coords from the currently active Space.
	/// </summary>
	/// <param name="coords">The coordinates of the LevelCell to be loaded.</param>
	private void LoadLevelCellFromSpace(CellPosition coords){
		if (!activeCells.ContainsKey(coords)){
			Task.Run(() => {
				if (Debugging) Debug.Print("LevelManager: Attemtping to get cell at " + coords);
				LevelCellRequest request = null;
				if (spaceToken is WorldSpaceToken){
					request = new WorldCellRequest((WorldSpaceToken)spaceToken, coords);
				}
				Task<LevelCellRequest> loadTask = levelHost.GetLevelCell(request);
				loadTask.Wait();
				request = loadTask.Result;
				//Debug.Print("LevelManager: Returned result is " + request);
				AddActiveCell(coords, request.payload);
				loadTask.Dispose();
			});
		}
	}

	/// <summary>
	/// Adds a LevelCell into the active cells dictionary with the specified coordinates, and positions
	/// it into the correct place.
	/// </summary>
	/// <param name="coords">The coordinates (given in the cell grid space).</param>
	/// <param name="levelCell">The LevelCell to be loaded.</param>
	private void AddActiveCell(CellPosition coords, LevelCell levelCell){
		if (Debugging) Debug.Print("LevelManager: Adding cell at " + coords);
		bool success = activeCells.TryAdd(coords, levelCell);
		if (!success){
			if (Debugging) Debug.Print("LevelManager: Cell was not successfully added.");
		}
		levelCellUpdates.Enqueue((false, coords, levelCell));
	}

	/// <summary>
	/// Unloads the cell at the given coordinates.
	/// </summary>
	/// <param name="coords">The coordinates of the cell to unload.</param>
	private void DisposeOfCell(CellPosition coords){
		if (Debugging) Debug.Print("LevelManager: Removing cell at " + coords);
        activeCells.Remove(coords, out _);
		levelCellUpdates.Enqueue((true, coords, null));
	}

    public void ConnectLevelHost(ILevelHost levelHost)
    {
        this.levelHost = levelHost;
		levelHost.ConnectManager(this);
    }

    public void DisconnectLevelHost(ILevelHost levelHost)
    {
        this.levelHost = null;
		levelHost.DisconnectManager(this);
    }
}

using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LOM.Levels;

public partial class LevelManagerNode : Node2D {
    /// <summary>
    /// The <c>LevelManager</c> that this Node references and represents.
    /// </summary>
    public LevelManager referenceManager;
    /// <summary>
    /// A Dictionary mapping from coordinates in cell space to the <c>LevelCellNodes</c> which
    /// represent those cells.
    /// </summary>
    private Dictionary<Vector2I, LevelCellNode> activeNodes = new();

    public LevelManagerNode(LevelManager referenceManager) : base(){
        this.referenceManager = referenceManager;
    }

    public override void _Ready()
    {
        
    }

    public override void _Process(double delta)
    {
        while(true){
            if (referenceManager.levelCellUpdates.TryDequeue(out (bool, Vector2I, LevelCell) update)){
                ProcessUpdate(update);
            }
            else{
                break;
            }
        }
    }

    /// <summary>
    /// Processes an update to the actively loaded cells, and will either remove or add a cell as
    /// appropriate.
    /// </summary>
    /// <param name="update">The update in the form of (Removed, CellCoords, LevelCell).</param>
    private void ProcessUpdate((bool, Vector2I, LevelCell) update){
        if (update.Item1){
            RemoveCell(update.Item2);
        }
        else {
            AddCell(update.Item2, update.Item3);
        }
    }

    /// <summary>
    /// Removes a LevelCell at the given coordinates. Removes the entry from the activeCells dictionary
    /// and frees the corresponding node.
    /// </summary>
    /// <param name="coords">The coordinates in cell space of the LevelCell to remove.</param>
    private void RemoveCell(Vector2I coords){
        if (!activeNodes.ContainsKey(coords)){
            return;
        }
        LevelCellNode toRemove = activeNodes[coords];
        activeNodes.Remove(coords);
        RemoveChild(toRemove);
        toRemove.Free();
    }

    /// <summary>
    /// Adds a LevelCell at the given coordinates. An entry is created for it in the activeCells dictionary
    /// and a new LevelCellNode is created to represent the cell.
    /// </summary>
    /// <param name="coords">The coordinates of the cell to add in cell space.</param>
    /// <param name="toAdd">The LevelCell to be added.</param>
    private void AddCell(Vector2I coords, LevelCell toAdd){
        LevelCellNode newNode = new(toAdd)
        {
            Position = new Vector2
        (
            coords.X * LevelManager.CellWidth * LevelManager.TileWidth,
            coords.Y * LevelManager.CellHeight * LevelManager.TileHeight
        ),
            Name = "LevelCell(" + coords.X + "," + coords.Y + ")"
        };
        AddChild(newNode);
        activeNodes.Add(coords, newNode);
    }

}
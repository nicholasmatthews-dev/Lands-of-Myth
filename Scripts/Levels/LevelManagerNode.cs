using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LOM.Levels;

public partial class LevelManagerNode : Node2D {
    private static bool Debugging = false;
    /// <summary>
    /// The <c>LevelManager</c> that this Node references and represents.
    /// </summary>
    public LevelManager referenceManager;
    /// <summary>
    /// A Dictionary mapping from coordinates in cell space to the <c>LevelCellNodes</c> which
    /// represent those cells.
    /// </summary>
    private Dictionary<CellPosition, LevelCellNode> activeNodes = new();
    private TileSetManager tileSetManager;

    public LevelManagerNode(LevelManager referenceManager, TileSetManager tileSetManager) : base(){
        this.tileSetManager = tileSetManager;
        this.referenceManager = referenceManager;
    }

    public override void _Ready()
    {
        
    }

    public override void _Process(double delta)
    {
        while(true){
            if (referenceManager.levelCellUpdates.TryDequeue(out (bool, CellPosition, LevelCell) update)){
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
    private void ProcessUpdate((bool, CellPosition, LevelCell) update){
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
    private void RemoveCell(CellPosition coords){
        if (Debugging) Debug.Print("LMNode: Removing cell at position " + coords);
        if (!activeNodes.ContainsKey(coords)){
            if (Debugging) Debug.Print("LMNode: Cell not contained in active nodes.");
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
    private void AddCell(CellPosition coords, LevelCell toAdd){
        if (Debugging) Debug.Print("LMNode: Adding cell " + toAdd + " at position " + coords);
        LevelCellNode newNode = new(toAdd, tileSetManager)
        {
            Position = new Vector2
        (
            coords.X * LevelManager.CellWidth * LevelManager.TileWidth,
            coords.Y * LevelManager.CellHeight * LevelManager.TileHeight
        ),
            Name = "LevelCell(" + coords.X + "," + coords.Y + ")"
        };
        if (Debugging){
            Node findChild = FindChild(newNode.Name);
            if (findChild is not null){
                Debug.Print("LMNode: Child with name " + newNode.Name + " already exists."); 
            }
        }
        AddChild(newNode);
        activeNodes.Add(coords, newNode);
    }

}
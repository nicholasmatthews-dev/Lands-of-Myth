using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LOM.Levels;

public partial class LevelManagerNode : Node2D {

    public LevelManager referenceManager;
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

    private void ProcessUpdate((bool, Vector2I, LevelCell) update){
        if (update.Item1){
            RemoveCell(update.Item2);
        }
        else {
            AddCell(update.Item2, update.Item3);
        }
    }

    private void RemoveCell(Vector2I coords){
        Debug.Print("LevelManagerNode: Removing LevelCellNode at " + coords);
        if (!activeNodes.ContainsKey(coords)){
            return;
        }
        LevelCellNode toRemove = activeNodes[coords];
        activeNodes.Remove(coords);
        RemoveChild(toRemove);
        toRemove.Free();
    }

    private void AddCell(Vector2I coords, LevelCell toAdd){
        Debug.Print("LevelManagerNode: Adding LevelCellNode at " + coords);
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
using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using LOM.Levels;

namespace LOM.Control;

[GlobalClass]
public partial class Movement : Node, IPositionUpdateSource
{
	/// <summary>
	/// The target Node2D for this movement node to move.
	/// </summary>
	[Export]
	public Node2D Target;
	/// <summary>
	/// The speed in pixels that this node will move at.
	/// </summary>
	[Export]
	public int Speed {get; set;} = 64;
	/// <summary>
	/// The width (in tiles) of this node.
	/// </summary>
	[Export]
	public int Width = 1;
	/// <summary>
	/// The height (in tiles) of this node.
	/// </summary>
	[Export]
	public int Height = 1;
	/// <summary>
	/// The width (in pixels) of a single tile.
	/// </summary>
	[Export]
	public int TileWidth = 16;
	/// <summary>
	/// The height (in pixels) of a single tile.
	/// </summary>
	[Export]
	public int TileHeight = 16;

	public LevelManager ActiveLevel;

	private bool destinationReached = true;

	private HashSet<WeakReference<IPositionUpdateListener>> positionUpdateListeners = new();

	private Queue<Vector2I> currentPath = new();
	private int maxPathSize = 2;
	private float inputRadius = 5;

	private Vector2I PositionCoord = new(0,0);
	private Vector2I DestinationCoord = new(0,0);
	private Vector2 TopLeft;
	private Vector2 Destination;
	private byte[] cellData;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		TopLeft = new Vector2(-0.5f * Width * TileWidth, -0.5f * Height * TileHeight);
		Target.Position = new Vector2(PositionCoord.X * TileWidth, PositionCoord.Y * TileHeight) - TopLeft;
		Destination = Target.Position;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if ((Target.Position - Destination).Length() <= inputRadius){
			HandleInput();
		}
		MoveToDestination(delta);
	}

	/// <summary>
	/// Adds a position update listener as a subscriber to this object.
	/// <para>
	/// NOTE: All references to listeners are stored as <c>WeakReference</c>s so this
	/// object shouldn't be relied on to keep its listeners alive.
	/// </para>
	/// </summary>
	/// <param name="listener">The listener which wishes to subscribe to this object.</param>
	public void AddPositionUpdateListener(IPositionUpdateListener listener){
		WeakReference<IPositionUpdateListener> reference 
		= new WeakReference<IPositionUpdateListener>(listener);
		positionUpdateListeners.Add(reference);
	}

	/// <summary>
	/// Sends a position update including the current <c>PositionCoord</c> to all the 
	/// <c>PositionUpdateListener</c>s subscribed to this object.
	/// <para>
	/// NOTE: This function also cleans up (removes) any references to dead listeners.
	/// </para>
	/// </summary>
	private void SignalPositionUpdate(){
		List<WeakReference<IPositionUpdateListener>> deadReferences = new(positionUpdateListeners.Count);
		foreach (WeakReference<IPositionUpdateListener> reference in positionUpdateListeners){
            if (reference.TryGetTarget(out IPositionUpdateListener listener))
            {
                listener.OnPositionUpdate(new WorldPosition(PositionCoord.X, PositionCoord.Y));
            }
			else {
				deadReferences.Add(reference);
			}
        }
		foreach (WeakReference<IPositionUpdateListener> reference in deadReferences){
			positionUpdateListeners.Remove(reference);
		}
	}

	/// <summary>
	/// Handles user input, sets a destination point based on which direction is pressed.
	/// </summary>
	private void HandleInput(){
		Vector2I newDestinationCoord = new Vector2I(DestinationCoord.X, DestinationCoord.Y);
		if (Input.IsActionPressed("left")){
			newDestinationCoord.X -= 1;
		}
		else if (Input.IsActionPressed("right")){
			newDestinationCoord.X += 1;
		}
		else if (Input.IsActionPressed("up")){
			newDestinationCoord.Y -= 1;
		}
		else if (Input.IsActionPressed("down")){
			newDestinationCoord.Y += 1;
		}
		if (DestinationCoord != newDestinationCoord && currentPath.Count <= maxPathSize){
			DestinationCoord = newDestinationCoord;
			currentPath.Enqueue(newDestinationCoord);
			Destination = GetWorldSpaceFromGrid(newDestinationCoord);
			destinationReached = false;
		}
	}

	/*
	private bool CheckCollision(Vector2I destinationPoint){
		List<WorldPosition> occupied = new List<WorldPosition>();
		for (int i = 0; i < Width; i++){
			for (int j = 0; j < Height; j++){
				occupied.Add(new WorldPosition(destinationPoint.X + i, destinationPoint.Y + j));
			}
		}
		return ActiveLevel.PositionValid(occupied);
	}
	*/

	/// <summary>
	/// Handles the interpolation of movement between grid spaces.
	/// Attempts to move based on <c>Speed</c> and <c>delta</c>, or otherwise moves exactly
	/// to the destination.
	/// </summary>
	/// <param name="delta">The time in seconds since the last frame.</param>
	private void MoveToDestination(double delta){
		float remainingLength = Speed * (float)delta;
		while (remainingLength > 0){
			if (currentPath.Count <= 0){
				break;
			}
			Vector2I nextDestinationCoord = currentPath.Peek();
			Vector2 nextDestination = GetWorldSpaceFromGrid(nextDestinationCoord);
			Vector2 toDestination = nextDestination - Target.Position;
			if (toDestination.Length() <= remainingLength){
				Target.Position = nextDestination;
				PositionCoord = nextDestinationCoord;
				currentPath.Dequeue();
				SignalPositionUpdate();
				if (!destinationReached && currentPath.Count <= 0){
					destinationReached = true;
				}
				remainingLength -= toDestination.Length();
			}
			else {
				Target.Position += toDestination.Normalized() * remainingLength;
				remainingLength -= remainingLength;
			}
		}
	}

	private Vector2 GetWorldSpaceFromGrid(Vector2I gridCoords){
		return new Vector2(gridCoords.X * TileWidth, gridCoords.Y * TileHeight) - TopLeft;
	}
}

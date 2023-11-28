using Godot;
using System;
using System.Diagnostics;

namespace LOM.Control;
public partial class CameraFollow : Node
{
	/// <summary>
	/// The target of the camera follow script, the camera will follow this Node2D.
	/// </summary>
	[Export]
	public Node2D Target;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		CenterCamera();
	}

	/// <summary>
	/// Centers the parent viewport on the position of this node.
	/// </summary>
	private void CenterCamera(){
		Viewport view = Target.GetViewport();
		Rect2 viewArea = view.GetVisibleRect();	
		//Transforms the position into canvas coordinates
		Vector2 newPosition = Target.GetGlobalTransform().Origin;
		view.CanvasTransform = new Transform2D(Vector2.Right, Vector2.Down, 
		(newPosition * -1) + (viewArea.Size * 0.5f));
	}
}

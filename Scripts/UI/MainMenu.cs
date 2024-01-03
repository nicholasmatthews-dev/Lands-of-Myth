using Godot;
using System;
using System.Diagnostics;

namespace LOM.UI;

public partial class MainMenu : Node2D
{
	private bool _Disposed = false;
	[Export]
	public Button SinglePlayerButton;
	public Action SinglePlayerAction;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void AssignSinglePlayerAction(Action action){
		if (SinglePlayerAction is null){
			SinglePlayerAction = action;
			if (SinglePlayerButton is not null){
				SinglePlayerButton.Pressed += SinglePlayerAction;
			}
			else {
				Debug.Print(GetType() + ": SinglePlayerButton not found.");
			}
		}
		else {
			throw new ArgumentException(GetType() + ": Single player action already assigned.");
		}
	}

    protected override void Dispose(bool disposing)
    {
		if (_Disposed){
			return;
		}
		if (disposing){
			Debug.Print(GetType() + ": Disposing of this instance.");
			try {
				SinglePlayerButton.Pressed -= SinglePlayerAction;
				Debug.Print(GetType() + ": Action unassigned.");
			}
			catch (Exception e){
				Debug.Print(GetType() + ": Error unassigning action. \"" + e.Message + "\"");
			}
			SinglePlayerAction = null;
			base.Dispose(disposing);
		}
    }
}

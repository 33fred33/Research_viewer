using Godot;
using System;

public class Individual_view : HBoxContainer
{
	[Signal]
	public delegate void view_individual(int individual_index);
	[Signal]
	public delegate void exit_individual();

	public override void _Ready()
	{
		
	}

	public void on_mouse_hover()
	{
		Label individual_index = (Label)GetNode<Label>("Individual_index");
		EmitSignal("view_individual", Convert.ToInt32(individual_index.Text));
	}
	public void on_mouse_exit()
	{
		EmitSignal("exit_individual");
	}

}

using Godot;
using System;

public class Node_data : HBoxContainer
{
	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";
	[Signal]
	public delegate void pressed_view_child(int child_node_index);
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
	}

   public void on_view_child_node(int child_node_index)
   {
	   Label child_index = (Label)GetNode<Label>("Child_index");
	   GD.Print(child_index.Text);
	   EmitSignal("pressed_view_child", Convert.ToInt32(child_index.Text));
   }
}

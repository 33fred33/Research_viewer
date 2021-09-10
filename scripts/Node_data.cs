using Godot;
using System;

public class Node_data : HBoxContainer
{
	
	[Signal]
	public delegate void pressed_view_child(int child_node_index);
	[Signal]
	public delegate void hovered_view_child(int child_node_index);
	[Signal]
	public delegate void exit_hover();
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
	}

   public void on_view_child_node(int child_node_index)
   {
	   Label child_index = (Label)GetNode<Label>("Child_index");
	   //GD.Print(child_index.Text);
	   EmitSignal("pressed_view_child", Convert.ToInt32(child_index.Text));
   }
   public void on_hover_child_node(int child_node_index)
   {
	   Label child_index = (Label)GetNode<Label>("Child_index");
	   //GD.Print(child_index.Text);
	   EmitSignal("hovered_view_child", Convert.ToInt32(child_index.Text));
   }
   public void on_exit_hover()
   {
	   EmitSignal("exit_hover");
   }

}

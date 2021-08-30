using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;


public class Main_control : Control
{

	//mnk game variables
	[Export]
	public Godot.TileSet mnk_tileset;
	Godot.PackedScene mnk_game_viewer;
	public List<mnk_state> mnk_game_states = new List<mnk_state>();


	public Random rand = new Random();

	public override void _Ready()
	{
		mnk_game_viewer = (PackedScene)GD.Load("res://mnk_game_view.tscn");
		var base_state = new mnk_state();
		base_state.set_initial_state(13,13,5);
		mnk_game_states.Add(base_state);
		mnk_game_states[0].set_initial_state(13,13,5);
	}

	public override void _Process(float delta)
	{
		if (Input.IsKeyPressed((int)KeyList.N))
		{
			GD.Print("Pressed N");
			var final_state = mnk_game_states[0].random_game(rand);
			view_mnk_state(final_state);
			//mnk_game.full_random_games();
			//this.EmitSignal("Test_game");
			//Godot.Control mnk_game = this.GetNode<Godot.Control>("mnk_game");

		}
	}

	public void view_mnk_state(mnk_state state)
	{
		state.view_state();
		PanelContainer mnk_game_view = (PanelContainer)mnk_game_viewer.Instance();
		mnk_game_view.SetPosition(this.RectPosition);
		this.AddChild(mnk_game_view);
	}

	

}





// ------------------------MCTS---------------------------------
public class MCTS_node
{
	public MCTS_node parent;
	public List<MCTS_node> children = new List<MCTS_node>();
	public int visits;
	public float reward;
	public int state_index;

}

public class MCTS
{


	
}






// -----------------------MNK GAME-----------------------------------
public class mnk_state
{
	public int m, n, k, player_turn, ply, winner;
	public int [][] board;
	public List<mnk_action> available_actions = new List<mnk_action>();
	public bool terminal;
	private int count = 0;
	private List<int> diag = new List<int>();
	private List<int> inv_diag = new List<int>();
	private int offset;

	//public mnk_state(int m, int n, int k, int player_turn, int ply, int winner, int[][] board, List<mnk_action>, bool terminal)
	//{
		
	//}

	public void set_initial_state(int new_m, int new_n, int new_k)
	{
		//GD.Print("{0},{1},{2}",new_m,new_n,new_k);
		//Assign variables
		m = new_m; n = new_n; k = new_k;
		terminal = false;
		player_turn = 1;
		ply = 1;
		winner = 0;
		//if (available_actions != null) {
		available_actions.Clear();// }
	
		//Initialize the board and available actions
		board = new int[m][];
		for (int y = 0; y < m; y++)
		{
			board[y] = new int[n];
			for (int x = 0; x< n; x++)
			{
				mnk_action action = new mnk_action {x=x, y=y};
				available_actions.Add(action);
			}
		}

	
	}

	public void make_action(int action_index)
	{
		var action = available_actions[action_index];
		board[action.y][action.x] = player_turn;


		//Horizontal
		if (k_line(board[action.y])) terminal = true;
		//Vertical
		if (!terminal){
			var column = board
				//.Where(o => (o != null && o.Count() > action.y))
				.Select(o => o[action.x])
				.ToArray();
			if (k_line(column)) terminal = true;
		}
		//Diagonals
		if (!terminal){
			diag.Clear();
			inv_diag.Clear();
			offset = action.y - action.x;
			for (int y=0; y<m; y++)
			{
				for (int x=0; x<n; x++)
				{
					if (y-x==offset) diag.Add(board[y][x]);
					else
					{
						if (x + y == action.x + action.y) inv_diag.Add(board[y][x]);
					}
				}
			}
			if (k_line(diag.ToArray())) terminal = true;
			if (!terminal) { if (k_line(inv_diag.ToArray())) terminal = true;}
		}

		if (available_actions.Count == 0) terminal = true;
		if (terminal) winner = player_turn;
		else
		{
			player_turn = 3 - player_turn;
			available_actions.RemoveAt(action_index);
			ply++;
		}
	}


	public bool k_line(int[] sequence)
	{
		if (sequence.Length < k) return false;
		count = 0;
		foreach(int element in sequence)
		{
			if (element == player_turn) 
			{
				count ++;
				if (count >= k) return true;
			}
			else count = 0;
		}
		return false;
	}

	public void print_array(int[] sequence)
	{
		string string_row = "";
		foreach (int content in sequence)
		{
			string_row += " " + Convert.ToString(content);
		}
		GD.Print(Convert.ToString(string_row));
	}

	public void view_state()
	{
		string string_row = "";
		GD.Print("Printing mnk state");
		foreach(int[] row in board)
		{
			string_row = "";
			foreach (int content in row)
			{
				string_row += Convert.ToString(content);
			}
			GD.Print(Convert.ToString(string_row));
		}
	}

	public mnk_state random_game(Random rand)
	{
		var ds = this.duplicate();
		while (!ds.terminal)
			{
				ds.make_action(rand.Next(0,ds.available_actions.Count-1));
			}
		//GD.Print("Winner", ds.winner, " in", ds.ply, " plies");
		//this.EmitSignal("mnk_game_finished", ds.board);
		//PanelContainer mnk_game_view = (PanelContainer)mnk_game_viewer.Instance();
		//mnk_game_view.SetPosition(game_controller.RectPosition);
		//game_controller.AddChild(mnk_game_view);
		return ds;
	}
		

	public mnk_state duplicate()
	{
		int[][] duplicate_board = new int[board.Length][];
		for (int i = 0; i < board.Length; i++)
		{
			duplicate_board[i] = (int[]) board[i].Clone();
		}
		List<mnk_action> duplicate_actions = new List<mnk_action>();
		foreach (mnk_action action in available_actions)
		{
			duplicate_actions.Add(action.duplicate());
		}

		mnk_state the_duplicate = new mnk_state
		{
			m = m,
			n = n, 
			k = k, 
			player_turn = player_turn, 
			ply = ply, 
			winner = winner,
			board = duplicate_board,
			available_actions = duplicate_actions,
			terminal = terminal
		};

		return the_duplicate;
	}

}

public class mnk_action
{
	public int x, y;

	public mnk_action duplicate()
	{
		mnk_action the_duplicate = new mnk_action {x=x, y=y};
		return the_duplicate;
	}
}

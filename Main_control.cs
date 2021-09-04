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
	Godot.PackedScene node_table;
	Godot.VBoxContainer instance_game_viewer;
	Godot.Label node_data;
	public List<mnk_state> mnk_game_states = new List<mnk_state>();
	public Godot.ScrollContainer tree_inspector;
	public Godot.GridContainer tree_data;

	//MCTS variables
	public MCTS mcts;
	public MCTS_node selected_node;
	public MCTS_node expanded_node;
	public MCTS_node showing_node;
	public double temporal_reward = 0;
	public Godot.Label reward_label;
	public GridContainer MCTS_menu;
	public Godot.Button selection_button, simulation_button, expansion_button, backpropagation_button, iterate_button, see_suggested_button;
	public Godot.HSlider N_iterations;
	public List<HBoxContainer> node_table_list = new List<HBoxContainer>();

	//Godot items

	//Class variables
	public bool M_lock = false;
	public bool N_lock = false;
	public bool S_lock = false;
	public Random rand = new Random();

	public override void _Ready()
	{
		mnk_game_viewer = (PackedScene)GD.Load("res://mnk_game_view.tscn");
		node_table = (PackedScene)GD.Load("res://Node_data.tscn");
		instance_game_viewer = (VBoxContainer)mnk_game_viewer.Instance();
		instance_game_viewer.SetPosition(this.RectPosition);
		this.AddChild(instance_game_viewer);
		node_data = instance_game_viewer.GetNode<Label>("Node_data");
		var base_state = new mnk_state();
		//base_state.set_initial_state(13, 13, 5);
		base_state.set_initial_state(3, 3, 3);
		mnk_game_states.Add(base_state);
		mcts = new MCTS(base_state);
		MCTS_menu = GetNode<GridContainer>("MCTS_menu");
		reward_label = MCTS_menu.GetNode<Label>("Reward");
		selection_button = MCTS_menu.GetNode<Button>("Selection");
		expansion_button = MCTS_menu.GetNode<Button>("Expansion");
		simulation_button = MCTS_menu.GetNode<Button>("Simulation");
		selection_button = MCTS_menu.GetNode<Button>("Backpropagation");
		selection_button = MCTS_menu.GetNode<Button>("Iterate");
		N_iterations = MCTS_menu.GetNode<HSlider>("N_iterations");
		see_suggested_button = MCTS_menu.GetNode<Button>("See_suggested");
		tree_inspector = (ScrollContainer)GetNode<ScrollContainer>("Tree_inspector");
		tree_data = tree_inspector.GetNode<GridContainer>("Tree_data");
	}

	public override void _Process(float delta)
	{
		free_keys();
		if (Input.IsKeyPressed((int)KeyList.N))
		{
			if (!N_lock)
			{
				N_lock = true;
				GD.Print("Pressed N");
				var final_state = mnk_game_states[0].random_game(rand);
				view_mnk_state(final_state);
				node_data.Text = "winner: " + Convert.ToString(final_state.winner);
			}

		}
		if (Input.IsKeyPressed((int)KeyList.S))
		{
			if (!S_lock)
			{
				S_lock = true;
				GD.Print("Pressed S");
				double s_temp_reward = 0;
				for (int i = 0; i < 1000; i++)
				{
					var final_state = showing_node.state.random_game(rand);
					s_temp_reward += mcts.result_to_reward(final_state);
					//view_mnk_state(final_state);
				}
				node_data.Text = "reward after 1000 rand games from this state: " + Convert.ToString(s_temp_reward);
			}

		}
		if (Input.IsKeyPressed((int)KeyList.M))
		{
			if (!M_lock)
			{
				M_lock = true;
				GD.Print("Pressed M");
				mcts.iteration();
				GD.Print(mcts.root_node.children.Count);
			}

		}
	}

	public void free_keys()
	{
		M_lock = false;
		N_lock=false;
		S_lock = false;
	}

	public void view_mnk_state(mnk_state state)
	{
		//state.view_state();
		//instance_game_viewer.QueueFree();
		TileMap mnk_tilemap = (TileMap)instance_game_viewer.GetNode<TileMap>("Board");

		var temp_vec = new Vector2(1, 1);

		int content;
		for (int x = 0; x < state.n; x++)
		{
			for (int y = 0; y < state.m; y++)
			{
				temp_vec = new Vector2(x, y);
				content = (int)state.board[x][y];
				if (content != 0)
				{
					content = content + 2;
					mnk_tilemap.SetCellv(temp_vec, content);
				}
				else mnk_tilemap.SetCellv(temp_vec, 1);
			}
		}
	}

	public void view_node(MCTS_node node)
	{
		clear_node_table();
		view_mnk_state(node.state);
		node_data.Text = (node._str());
		showing_node = node;
		view_in_node_table(node);
	}

	public void view_child_from_showing(int child_index)
	{
		GD.Print("In view_child_from_showing" + Convert.ToString(child_index));
		view_node(showing_node.children[child_index]);
	}

	public void clear_node_table()
	{
		foreach (Godot.HBoxContainer node_data_container in node_table_list)
		{
			node_data_container.QueueFree();
		}
		node_table_list.Clear();
	}

	public void sort_node_table_ucb()
	{
		var tree_inspector = GetNode<ScrollContainer>("Tree_inspector");
		var tree_data = tree_inspector.GetNode<GridContainer>("Tree_data");


	}

	public void view_in_node_table(MCTS_node node)
	{
		double min_ucb = 0;
		double max_ucb = 0;
		double max_reward = 0;
		double min_reward = 0;
		double max_visits = 0;
		double min_visits = 1;
		bool first_time = true;

		foreach (var child_node in node.children)
		{
			if (first_time)
			{
				max_ucb = child_node.Value.UCB(mcts.c);
				min_ucb = child_node.Value.UCB(mcts.c);
				max_reward = child_node.Value.reward;
				min_reward = child_node.Value.reward;
				max_visits = child_node.Value.visits;
				//min_visits = child_node.Value.visits;

			}
			else
			{
				double child_ucb = child_node.Value.UCB(mcts.c);
				if (child_ucb < min_ucb) min_ucb = child_ucb;
				else if (child_ucb > max_ucb) max_ucb = child_ucb;
				if (child_node.Value.reward < min_reward) min_reward = child_node.Value.reward;
				else if (child_node.Value.reward > max_reward) max_reward = child_node.Value.reward;
				//if (child_node.Value.visits < min_visits) min_visits = child_node.Value.visits;
				if (child_node.Value.visits > max_visits) max_visits = child_node.Value.visits;

			}
			Godot.HBoxContainer instance_node_inspector = (HBoxContainer)node_table.Instance();
			instance_node_inspector.Connect("pressed_view_child", this, "view_child_from_showing");
			Label child_index = (Label)instance_node_inspector.GetNode<Label>("Child_index");
			child_index.Text = Convert.ToString(child_node.Key);
			//botonsitodecandela.Node_data.child_node_index = child_node.Key;
			tree_data.AddChild(instance_node_inspector);
			node_table_list.Add(instance_node_inspector);
			first_time = false;
			
		}
		GD.Print("Children in list:", node_table_list.Count, " Children in node:", node.children.Count);
		foreach(var child_node in node.children)
		//foreach (var node_row in node_table_list)
		{
			GD.Print(child_node.Key);
			bool found = false;
			HBoxContainer node_row = new HBoxContainer();
			foreach(var row in node_table_list)
			{
				Label child_index = (Label)row.GetNode<Label>("Child_index");
				if (child_index.Text == Convert.ToString(child_node.Key))
				{
					node_row = row;
					found = true;
				}
			}
			if (!found) GD.Print("Node not found");
			else
			{
				//var node_row = node_table_list[child_node.Key];
				Godot.ProgressBar ucb_progress = node_row.GetNode<ProgressBar>("UCB_relative");
				ucb_progress.MinValue = min_ucb;
				ucb_progress.MaxValue = max_ucb;
				ucb_progress.Value = child_node.Value.UCB(mcts.c);
				Godot.ProgressBar reward_progress = node_row.GetNode<ProgressBar>("Rew_relative");
				reward_progress.MinValue = min_reward;
				reward_progress.MaxValue = max_reward;
				reward_progress.Value = child_node.Value.reward;
				Godot.ProgressBar visits_progress = node_row.GetNode<ProgressBar>("Visits_relative");
				visits_progress.MinValue = min_visits;
				visits_progress.MaxValue = max_visits;
				visits_progress.Value = child_node.Value.visits;
			}
		}

	}

	public void mcts_view_root_node()
	{
		view_node(mcts.root_node);
	}

	public void mcts_selection()
	{
		selected_node = mcts.UCT_policy(mcts.root_node);
	}

	public void mcts_expansion()
	{
		if (selected_node.is_leaf()) expanded_node = mcts.random_expansion(selected_node);
		else GD.Print("mcts_expansion didnt receive a leaf node");
	}

	public void view_selected_node()
	{
		view_node(selected_node);
	}

	public void view_expanded_node()
	{
		view_node(expanded_node);
	}

	public void mcts_simulation()
	{
		temporal_reward = mcts.simulation(expanded_node);
		reward_label.Text = "Reward: " + Convert.ToString(temporal_reward);
	}

	public void mcts_backpropagation()
	{
		mcts.backpropagate(expanded_node, temporal_reward);
	}

	public void mcts_iterate()
	{
		for (int i = 0; i < N_iterations.Value; i++)
		{
		mcts_selection();
		mcts_expansion();
		mcts_simulation();
		mcts_backpropagation();
		view_expanded_node();
		}
	}

	public void see_suggested_move()
	{
		//int index = mcts.suggested_action_index();
		int index = mcts.robust_action_index();
		view_node(mcts.root_node.children[index]);
	}

	public void see_parent()
	{
		if (!showing_node.is_root) view_node(showing_node.parent);
	}

}





// ------------------------MCTS---------------------------------
public class MCTS_node
{
	public int action_index;
	public mnk_state state;
	public MCTS_node parent;
	public Dictionary<int, MCTS_node> children = new Dictionary<int, MCTS_node>();
	public int visits;
	public double reward;
	public List<int> pattern_indexes = new List<int>();
	public bool is_root;

	public MCTS_node(mnk_state t_state, MCTS_node t_parent, bool t_is_root=false, int t_action_index = -1)
	{
		state = t_state;
		parent = t_parent;
		is_root = t_is_root;
		action_index = t_action_index;
	}
	public bool is_leaf()
	{
		return (children.Count == 0 || state.available_actions.Count != children.Count);
	}

	public double UCB(double c)
	{
		if (visits > 0) return reward / visits + c * Math.Sqrt(Math.Log(parent.visits) / visits);
		else return double.PositiveInfinity;
	}

	public MCTS_node add_child(mnk_state child_state, int action_index)
	{
		MCTS_node child_node = new MCTS_node (child_state, this, false, action_index);
		children.Add(action_index, child_node);
		return child_node;
	}

	public void update_reward(double new_reward)
	{
		visits = visits + 1;
		reward += new_reward;
	}

	public int depth()
	{
		int depth = 0;
		var node = this;
		while (!node.is_root)
		{
			node = node.parent;
			depth++;
		}
		return depth;
	}

	public string _str()
	{
		string s = "Node. Depth " + Convert.ToString(depth()) 
				+ ", Visits " + Convert.ToString(visits) 
				+ ", AvgReward " + Convert.ToString(reward/visits) 
				+ ", AvActions " + Convert.ToString(state.available_actions.Count)
				+ ", Children " + Convert.ToString(children.Count)
				+ ", Action_index " + Convert.ToString(action_index); 
		return s;
	}



}

public class MCTS
{
	public MCTS_node root_node;
	public double c;
	public int rollouts;
	public double win_value;
	public double draw_value;
	public double lose_value;
	public Random rand = new Random();

	public MCTS(mnk_state root_state, double c = 2, int rollouts = 200, double win_value = 1, double draw_value = 0, double lose_value = -1)
	{
		root_node = new MCTS_node(root_state, null, true);
		this.c = c;
		this.rollouts = rollouts;
		this.win_value = win_value;
		this.draw_value = draw_value;
		this.lose_value = lose_value;

	}
	public void iteration(int max_iterations = 1)
	{
		int iteration_count = 0;

		while(iteration_count < max_iterations)
		{
			MCTS_node node = UCT_policy(root_node);
			GD.Print("Node after UCT_policy: ", node._str());

			node = random_expansion(node);
			GD.Print("Node after expansion: ", node._str());


			double reward = simulation(node);
			GD.Print("Reward after simulation: ", Convert.ToString(reward));

			backpropagate(node, reward);

			iteration_count++;
		}
		

	}

	public int suggested_action_index()
	{
		
		int action_index = -1;
		double max_visits = 0;
		int visits;

		foreach (var child_node in root_node.children)
		{
			visits = child_node.Value.visits;
			if (visits > max_visits)
			{
				max_visits = visits;
				action_index = child_node.Key;
			}
		}
		return action_index;
		

	}

	public int robust_action_index()
	{
		int action_index = -1;
		double max_reward = 0;
		double reward;

		foreach (var child_node in root_node.children)
		{
			reward = child_node.Value.reward;
			if (reward > max_reward)
			{
				max_reward = reward;
				action_index = child_node.Key;
			}
		}
		return action_index;
	}



	public void backpropagate(MCTS_node node, double new_reward)
	{
		while (!node.is_root)
		{
			if (node.state.player_turn == root_node.state.player_turn) node.update_reward(-new_reward);
			else node.update_reward(new_reward);
			node = node.parent;
			//GD.Print("Node after backpropagation: ", node._str());
		}
		node.update_reward(0);
	}

	public double simulation(MCTS_node node)
	{
		if(node.state.terminal) return result_to_reward(node.state);
		double reward =0;
		for (int i =0; i<rollouts; i++)
		{
			reward += result_to_reward(node.state.random_game(rand));
		}
		return reward/rollouts;
	}

	public double result_to_reward(mnk_state final_state)
	{

		if (final_state.winner == root_node.state.player_turn) return win_value;
		if (final_state.winner != root_node.state.player_turn) return lose_value;
		return draw_value;
	}

	public MCTS_node random_expansion(MCTS_node node)
	{
		if (node.state.terminal) return node;
		var duplicate_state = node.state.duplicate();
		List<int> available_action_indexes = new List<int>();
		for (int i = 0; i < node.state.available_actions.Count; i++) available_action_indexes.Add(i);
		foreach (int i in node.children.Keys) available_action_indexes.Remove(i);
		int selection_index = rand.Next(available_action_indexes.Count);
		int action_index = available_action_indexes[selection_index];
		duplicate_state.make_action(action_index);
		return node.add_child(duplicate_state, action_index);
	}

	public MCTS_node select_UCB(MCTS_node node)
	{

		int max_key = 0;
		double max_value = 0;
		double max_UCB_value;

		foreach (var child_node in node.children)
		{
			max_UCB_value = child_node.Value.UCB(c);
			if (max_UCB_value > max_value)
			{
				max_value = max_UCB_value;
				max_key = child_node.Key;
			}
		}
		return node.children[max_key];
	}

	public MCTS_node UCT_policy(MCTS_node node)
	{
		while (!node.is_leaf())
		{
			node = select_UCB(node);
			//if (node.depth() > 1) GD.Print(Convert.ToString(node.depth()));
		}
		return node;
	}


}

/*
public class MCTS_EA : MCTS
{

	public MCTS_EA(mnk_state root_state, double c = 2, int rollouts = 100, double win_value = 1, double draw_value = 0, double lose_value = -1)
	{
		root_node = new MCTS_node(root_state, null, true);
		this.c = c;
		this.rollouts = rollouts;
		this.win_value = win_value;
		this.draw_value = draw_value;
		this.lose_value = lose_value;

	}
}
*/



// -----------------------MNK GAME-----------------------------------
public class mnk_state
{
	public int m, n, k, player_turn, ply, winner;
	public int[][] board;
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
			for (int x = 0; x < n; x++)
			{
				mnk_action action = new mnk_action { x = x, y = y };
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
		if (!terminal)
		{
			var column = board
				//.Where(o => (o != null && o.Count() > action.y))
				.Select(o => o[action.x])
				.ToArray();
			if (k_line(column)) terminal = true;
		}
		//Diagonals
		if (!terminal)
		{
			diag.Clear();
			inv_diag.Clear(); //test speed versus declaration
			offset = action.y - action.x;
			for (int y = 0; y < m; y++)
			{
				for (int x = 0; x < n; x++)
				{
					if (y - x == offset) diag.Add(board[y][x]);
					else
					{
						if (x + y == action.x + action.y) inv_diag.Add(board[y][x]);
					}
				}
			}
			if (k_line(diag.ToArray())) terminal = true;
			if (!terminal) { if (k_line(inv_diag.ToArray())) terminal = true; }
		}

		available_actions.RemoveAt(action_index);
		if (terminal) winner = player_turn;
		else
		{
			if (available_actions.Count == 0) terminal = true;
			else
			{
				player_turn = 3 - player_turn;
				ply++;
			}
		}
	}

	public mnk_state imagine_action(int action_index)
	{
		mnk_state imagined_state = duplicate();
		imagined_state.make_action(action_index);
		return imagined_state;
	}


	public bool k_line(int[] sequence)
	{
		if (sequence.Length < k) return false;
		count = 0;
		foreach (int element in sequence)
		{
			if (element == player_turn)
			{
				count++;
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
		foreach (int[] row in board)
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
		//rand = new Random();
		var ds = this.duplicate();
		while (!ds.terminal)
		{
			ds.make_action(rand.Next(0,ds.available_actions.Count));
		}
		return ds;
	}


	public mnk_state duplicate()
	{
		int[][] duplicate_board = new int[board.Length][];
		for (int i = 0; i < board.Length; i++)
		{
			duplicate_board[i] = (int[])board[i].Clone();
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
		mnk_action the_duplicate = new mnk_action { x = x, y = y };
		return the_duplicate;
	}
}


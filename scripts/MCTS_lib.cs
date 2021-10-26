using Godot;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class MCTS_lib : Control
{
	
	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		/*
		IGameState test_state = (IGameState)new MNKGameState{m=3,n=3,k=3};
		test_state.set_initial_state();
		test_state.make_action(0);
		IGameState dup_state = test_state.duplicate();
		IGameState final_state = dup_state.random_game();
		GD.Print("test_moves", test_state.available_actions.Count);
		GD.Print("dup_moves", dup_state.available_actions.Count);
		GD.Print("winner", dup_state.winner);
		IAgent test_agent = (IAgent)new AgentMCTS();
		*/
		//int test_action_index = test_agent.best_action(test_state);
		//test_state.make_action(test_action_index);
	}

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }

	public void test_method()
	{
		GD.Print("Test method reached");
	}
}



//----------------------------------------------------------------------
//----------------------------------------------------------------------
//----------------------------------------------------------------------
//-------------------------------AGENTS---------------------------------
//----------------------------------------------------------------------
//----------------------------------------------------------------------
//----------------------------------------------------------------------
public interface IAgent
{
	Random rand {get; set;}
	IGameState root_state {get; set;}
	int best_action(IGameState root_state);
}

public interface ITreeAgent : IAgent
{
	MCTSNode selection(MCTSNode node);
	MCTSNode expansion(MCTSNode node);
	double rollout(MCTSNode node, int sim_rollouts);
	void backpropagate(MCTSNode node, double total_reward, int sim_rollouts);
}

public class AgentMCTS : IAgent
{
public Random rand { get; set;}
public IGameState root_state {get; set;}
	public int best_action(IGameState root_state)
	{
		if (root_state.available_actions.Count > 0) return rand.Next(root_state.available_actions.Count);
		else return 0;
	}
}

public class AgentEAMCTS : IAgent, ITreeAgent
{
	public MCTSNode root_node, expanded_node, selected_node;
	public IGameState root_state {get; set;}
	public int rollouts, active_pop_size, postulant_pop_size, individuals_count, mutation_growth, mutation_shrink, tournament_size, max_complexity, max_initial_complexity, random_shrink, random_growth, simulation_count, simulation_time;
	public double stop_condition_value, c, win_value, draw_value, lose_value, selection_c, mutation_rate, elitism, cumulative_reward;
	public string stop_condition;
	public Dictionary<int,Pattern> active_population = new Dictionary<int, Pattern>();
	public Dictionary<int,Pattern> postulant_population = new Dictionary<int, Pattern>();
	public Random rand { get; set;}
	public AgentEAMCTS(IGameState root_state
				,Random fixed_rand = null
				,double c = 2
				,int rollouts = 100
				,double win_value = 1
				,double draw_value = 0
				,double lose_value = -1
				,string stop_condition = "iterations" //time, iterations
				,double stop_condition_value = 1000 //time in milliseconds
				//ea params
				,double selection_c = 0.1
				,int active_pop_size = 100
				,int postulant_pop_size = 100
				,int max_initial_complexity = 3
				,int max_complexity = 5
				,double mutation_rate = 0.5
				,double elitism = 0.5
				,int mutation_growth = 2
				,int mutation_shrink = 2
				,int tournament_size = 5)
		{
			root_node = new MCTSNode(root_state, null, true);
			if (fixed_rand == null) rand = new Random();
			else rand = fixed_rand;
			this.root_state = root_state;
			this.c = c;
			this.rollouts = rollouts;
			this.win_value = win_value;
			this.draw_value = draw_value;
			this.lose_value = lose_value;
			this.selection_c = selection_c;
			this.active_pop_size = active_pop_size;
			this.postulant_pop_size = postulant_pop_size;
			this.max_initial_complexity = max_initial_complexity;
			this.max_complexity = max_complexity;
			this.mutation_rate = mutation_rate;
			this.elitism = elitism;
			this.mutation_growth = mutation_growth;
			this.mutation_shrink = mutation_shrink;
			this.tournament_size = tournament_size;
			this.stop_condition = stop_condition;
			this.stop_condition_value = stop_condition_value;
		}

	public int best_action(IGameState root_state)
	{
		root_node = new MCTSNode(root_state,null,true);
		simulation_count = 0;
		simulation_time = 0;
		DateTime start_time = DateTime.UtcNow;           	

		while ((stop_condition == "iterations" && simulation_count < stop_condition_value)||(stop_condition == "time" && simulation_time < stop_condition_value))
		{
			simulation(rollouts);
			simulation_count++;
			simulation_time = Convert.ToInt32((DateTime.UtcNow - start_time).TotalMilliseconds);
		}
		return 0;
	}
	public void simulation(int sim_rollouts)
	{
		MCTSNode selected_node = selection(root_node);
		expanded_node = expansion(selected_node);
		cumulative_reward = rollout(expanded_node, sim_rollouts);
		backpropagate(expanded_node, cumulative_reward, sim_rollouts);
	}
	public MCTSNode selection(MCTSNode node)
	{
		while (!node.is_leaf())
		{
			node = select_UCB(node);
		}
		return node;
	}
	public MCTSNode select_UCB(MCTSNode node)
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
	public MCTSNode expansion(MCTSNode node)
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
	public double rollout(MCTSNode node, int sim_rollouts)
	{
		if(node.state.terminal) return result_to_reward(node.state);
		double reward =0;
		for (int i =0; i<sim_rollouts; i++)
		{
			reward += result_to_reward(node.state.random_game());
		}
		return reward;
	
	}
	public void backpropagate(MCTSNode node, double total_reward, int sim_rollouts)
	{
		double average_reward = total_reward/sim_rollouts;
		while (!node.is_root)
		{
			if (node.state.player_turn == root_node.state.player_turn) node.update_reward(-average_reward);
			else node.update_reward(average_reward);
			node = node.parent;
		}
		node.update_reward(average_reward);
	}
	//Utilities
	public double result_to_reward(IGameState final_state)
		{
			if (final_state.winner == root_node.state.player_turn) return win_value;
			if (final_state.winner == root_node.state.swap_player(root_node.state.player_turn)) return lose_value;
			return draw_value;
		}

}

public class MCTSNode
{
	//variables
		public int action_index;
		public int visits;
		public int max_tested_index = 0;
		public IGameState state;
		public MCTSNode parent;
		public Dictionary<int, MCTSNode> children = new Dictionary<int, MCTSNode>();
		public double reward; //cumulative reward
		public bool is_root;
		public List<int> matching_indexes = new List<int>();

	public MCTSNode(IGameState state, MCTSNode parent, bool is_root=false, int action_index = -1)
		{
			this.state = state;
			this.parent = parent;
			this.is_root = is_root;
			this.action_index = action_index;
		}
	public bool is_leaf()
		{
			return (children.Count == 0 || state.available_actions.Count > children.Count);
		}
	public double UCB(double c)
		{
			if (visits > 0) return reward / visits + c * Math.Sqrt(Math.Log(parent.visits) / visits);
			else return double.PositiveInfinity;
		}
	public MCTSNode add_child(IGameState child_state, int action_index)
		{
			MCTSNode child_node = new MCTSNode (child_state, this, false, action_index);
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
	public double average_reward()
		{
			return reward/visits;
		}
	public void update_matching_indexes(List<int> indexes)
	{
		if (indexes.Count > 0)
		{
			matching_indexes.AddRange(indexes);
		}
	}
	public void update_max_tested_index(int max_index)
	{
		if (max_index > max_tested_index)
		{
			max_tested_index = max_index;
		}
	}
}

public class Pattern
{
	public int index, visits, age, updates_age;
	public double cumulative_reward;
	public IGameState matching_state;
	public Dictionary<int, int> pattern = new Dictionary<int, int>();

	public Pattern(Dictionary<int, int> pattern, int index, IGameState state)
		{
			this.pattern = pattern;
			this.index = index;
			this.matching_state = state;
		}

}
//----------------------------------------------------------------------
//----------------------------------------------------------------------
//----------------------------------------------------------------------
//-------------------------------GAMES----------------------------------
//----------------------------------------------------------------------
//----------------------------------------------------------------------
//----------------------------------------------------------------------
public interface IGameState
{
	bool terminal {get; set;}
	int player_turn { get; set;}
	int ply { get; set;}
	int winner { get; set;}
	Random rand {get; set;}
	int[][] board {get; set;}
	void set_initial_state();
	void make_action(int action_index);
	void make_random_action();
	List<IAction> available_actions {get;set;}
	IGameState duplicate();
	(IGameState random_state, IGameState final_state) get_random_future_state();
	IGameState random_game();
	int swap_player(int player_turn);
	IGameState imagine_action(int action_index);
	Dictionary<int, int> feature_vector {get;set;}
}
public class MNKGameState : IGameState
{
	public bool terminal { get; set;}
	public int player_turn { get; set;}
	public int ply { get; set;}
	public int winner { get; set;}
	public Random rand { get; set;} = new Random();
	public int m, n, k;
	private int count = 0;
	private List<int> diag = new List<int>();
	private List<int> inv_diag = new List<int>();
	private int offset;
	public int[][] board {get; set;}
	public Dictionary<int, int> feature_vector {get;set;}= new Dictionary<int, int>();
	public Dictionary<int, int[]> feature_index_to_board_coordinates = new Dictionary<int, int[]>();
	public int[][] coordinates_to_feature_index;
	public List<IAction> available_actions {get; set; }= new List<IAction>();
	public void set_initial_state()
	{
		//Assign variables
		terminal = false;
		player_turn = 1;
		ply = 1;
		winner = 0;
		//if (available_actions != null) {
		available_actions.Clear();// }

		//Initialize the board and available actions
		int counter = 0;
		board = new int[m][];
		coordinates_to_feature_index = new int[m][];
		for (int y = 0; y < m; y++)
		{
			board[y] = new int[n];
			coordinates_to_feature_index[y] = new int[n];
			for (int x = 0; x < n; x++)
			{
				MNKAction action = new MNKAction(x,y);// { x = x , y = y };
				available_actions.Add((IAction)action);
				feature_index_to_board_coordinates[counter] = new int[] {x,y};
				coordinates_to_feature_index[y][x] = counter;
				counter ++;
			}
		}
		set_feature_vector();
	}
	public void set_feature_vector() //Dictionary<int, int>
	{
		foreach (var pair in feature_index_to_board_coordinates)
		{
			feature_vector[pair.Key] = board[pair.Value[1]][pair.Value[0]];
		}
	}
	public void make_action(int action_index)
	{
		MNKAction action = (MNKAction)available_actions[action_index];
		board[action.y][action.x] = player_turn;
		feature_vector[coordinates_to_feature_index[action.y][action.x]] = player_turn;

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
					if (x + y == action.x + action.y) inv_diag.Add(board[y][x]);
				}
			}
			if (k_line(diag.ToArray())) terminal = true;
			if (!terminal) 
			{ 
				if (k_line(inv_diag.ToArray())) terminal = true; 
			}
		}

		available_actions.RemoveAt(action_index);
		if (terminal) winner = player_turn;
		else
		{
			if (available_actions.Count == 0) terminal = true;
			else
			{
				player_turn = swap_player(player_turn);
				ply++;
			}
		}
	}
	public void make_random_action()
	{
		make_action(rand.Next(0,available_actions.Count));
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
	
	//Utilities
	public int swap_player(int player_turn)
	{
		return 3-player_turn;
	}
	public (IGameState random_state, IGameState final_state) get_random_future_state()
	{
		List <IGameState> states = new List<IGameState>();
		IGameState state = this;
		states.Add(state);
		while (!state.terminal)
		{
			state = state.imagine_action(rand.Next(0,state.available_actions.Count));
			if (!state.terminal) states.Add(state);
		}
		return ((IGameState)states[rand.Next(0,states.Count)], (IGameState)state);
	}
	public IGameState random_game()
	{
		var ds = this.duplicate();
		while (!ds.terminal)
		{
			ds.make_action(rand.Next(0,ds.available_actions.Count()));
		}
		return ds;
	}
	public IGameState imagine_action(int action_index)
	{
		IGameState imagined_state = duplicate();
		imagined_state.make_action(action_index);
		return imagined_state;
	}
	public IGameState duplicate()
	{
		int[][] duplicate_board = new int[board.Length][];
		for (int i = 0; i < board.Length; i++)
		{
			duplicate_board[i] = (int[])board[i].Clone();
		}
		List<IAction> duplicate_actions = new List<IAction>();
		foreach (IAction action in available_actions)
		{
			duplicate_actions.Add(action.duplicate());
		}
		Dictionary<int, int> duplicate_feature_vector = new Dictionary<int, int>();
		foreach (var feature in feature_vector)
		{
			duplicate_feature_vector[feature.Key] = feature.Value;
		}

		MNKGameState the_duplicate = new MNKGameState
		{
			m = m,
			n = n,
			k = k,
			player_turn = player_turn,
			ply = ply,
			winner = winner,
			board = duplicate_board,
			available_actions = duplicate_actions,
			terminal = terminal,
			feature_index_to_board_coordinates = feature_index_to_board_coordinates,
			coordinates_to_feature_index = coordinates_to_feature_index,
			feature_vector = duplicate_feature_vector,
			rand = rand
		};
		return (IGameState)the_duplicate;
	}
}

public interface IAction
{
	IAction duplicate();
}
public class MNKAction : IAction
{
	public int x, y;
	public MNKAction(int new_x, int new_y)
	{
		x = new_x;
		y = new_y;
	}
	public IAction duplicate()
	{
		IAction the_duplicate = (IAction)new MNKAction(x,y);
		return the_duplicate;
	}
}

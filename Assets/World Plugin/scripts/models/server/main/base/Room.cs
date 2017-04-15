using UnityEngine.Networking;
using System.Collections.Generic;

public class Room{
	public static Dictionary<int, Player> allPlayers = new Dictionary<int, Player> ();

	#region Values of this Room
	public List<Player> players = new List<Player> ();
	public Player host;
	public string id{ get; set; }
	public int totalPlayers{ get; set; }
	public bool saved{ get; set; }
	#endregion

	#region Room Functions
	/// <summary>
	/// Adds the player to the room.
	/// </summary>
	public void AddPlayer(Player player){
		players.Add (player);
		allPlayers.Add (player.connectionID, player);
		host = players [0];

		for (int i = 0; i < players.Count; i++)
			players [i].UpdatePlayer (i + 1, players.Count, players[i].connectionID, -1, false);
		totalPlayers = players.Count;
	}

	/// <summary>
	/// Removes the player from the room.
	/// </summary>
	public void RemovePlayer(Player player){
		
		bool wasHost = false;

		if (player == host)
			wasHost = true;
		
		int left = player.connectionID;
		players.Remove (player);
		allPlayers.Remove (player.connectionID);
		totalPlayers = players.Count;

		if (players.Count != 0) {
			
			host = players [0];

			for (int i = 0; i < players.Count; i++){
				players [i].UpdatePlayer (i + 1, players.Count, players [i].connectionID, left, wasHost);
			}

			MasterServerClient.main.UpdateServer (allPlayers.Count, "", "");

		}else {

			MasterServer.rooms.Remove (id);
			MasterServerClient.main.UpdateServer (allPlayers.Count, "", id);

		}

	}

	/// <summary>
	/// Sends the change message.
	/// </summary>
	public void SendMessage(Player player, ServerClientConstants.SendChanges msg){
		foreach(Player p in players)
			if(player != p)
				p.SendMessage(msg);
	}

	/// <summary>
	/// Gets the next save player.
	/// </summary>
	/// <returns>The save player.</returns>
	public Player NextSavePlayer(){
		
		foreach (Player p in players)
			if (!p.saved) {
				p.saved = true;
				return p;
			}
		
		ResetSaves ();

		return NextSavePlayer();
	}

	/// <summary>
	/// Resets the saves.
	/// </summary>
	void ResetSaves(){
		foreach (Player p in players)
			p.saved = false;
	}
	#endregion
}
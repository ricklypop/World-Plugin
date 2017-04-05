using UnityEngine.Networking;

public class Player{
	public int connectionID{ get; set; }//The connection id for this player
	public Room room { get; set; }//The room this player is in
	public string deviceID;
	public bool saved {get; set;}

	#region Player Functions
	/// <summary>
	/// Updates the players load balancing information.
	/// </summary>
	public void UpdatePlayer(int i, int t, int c, int l, bool wasHost){
		ServerClientConstants.UpdatePlayers update = new ServerClientConstants.UpdatePlayers ();
		update.playerNumber = i;
		update.totalPlayers = t;
		update.connNumber = c;
		update.leftConnId = l;

		if (wasHost)
			update.wasHost = 1;
		else
			update.wasHost = 0;
		NetworkServer.SendToClient (connectionID, ServerClientConstants.UpdatePlayersId, update);
	}

	/// <summary>
	/// Sends the change message to the client.
	/// </summary>
	public void SendMessage(ServerClientConstants.SendChanges msg){
		NetworkServer.SendToClient (connectionID, ServerClientConstants.SendChangesId, msg);
	}
	#endregion
}
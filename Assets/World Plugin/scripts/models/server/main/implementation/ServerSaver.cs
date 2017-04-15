using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class ServerSaver : Update {
	private float time; 

	/// <summary>
	/// Update this instance. If config is set, listen on port.
	/// </summary>
	public override void OnUpdate(){
		time += Time.deltaTime;

		if (time > ServerClientConstants.saveTime / MasterServer.rooms.Count 
			&& MasterServer.rooms.Count > 0) {

			SaveWorld ();
			time = 0;

		} 

	}

	/// <summary>
	/// Gets the next unsaved room.
	/// </summary>
	/// <returns>The next unsaved room.</returns>
	Room GetNextRoom(){

		foreach (Room r in MasterServer.rooms.Values) {
			if (!r.saved) {
				return r;
			}
		}

		ResetSaves ();

		return GetNextRoom ();

	}

	/// <summary>
	/// Resets the saves.
	/// </summary>
	void ResetSaves(){

		foreach (Room r in MasterServer.rooms.Values) {
			r.saved = false;
		}

	}

	void SaveWorld(){

		Room r = GetNextRoom ();
		Player p = r.NextSavePlayer ();

		NetworkServer.SendToClient (p.connectionID, ServerClientConstants.SaveWorldId, new ServerClientConstants.SaveWorld());

		r.saved = true;

	}
		
	public override void OnApplicationQuit (){}

}

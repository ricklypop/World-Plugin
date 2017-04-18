﻿using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class ServerStreamer {


	public void RegisterListeners(){
		NetworkServer.RegisterHandler (ServerClientConstants.RequestObjUpdateId, StreamUpdateRequest);
		NetworkServer.RegisterHandler (ServerClientConstants.SendObjUpdateId, StreamUpdate);
		NetworkServer.RegisterHandler (ServerClientConstants.SendWorldId, StreamWorld);
		NetworkServer.RegisterHandler (ServerClientConstants.SendChangesId, StreamChanges);
	}

	/// <summary>
	/// Streams the changes.
	/// </summary>
	void StreamChanges(NetworkMessage m){
		var msg = m.ReadMessage<ServerClientConstants.SendChanges> ();
		Room.allPlayers[m.conn.connectionId].room.SendMessage(Room.allPlayers[m.conn.connectionId], msg);
	}

	/// <summary>
	/// Streams the update.
	/// </summary>
	void StreamUpdate(NetworkMessage m){
		DLog.Log ("Send update requested from: " + m.conn.connectionId, Color.yellow);

		var msg = m.ReadMessage<ServerClientConstants.UpdateObject> ();
		int conn = msg.conn;
		msg.conn = conn;

		NetworkServer.SendToClient (conn, ServerClientConstants.SendObjUpdateId, msg);
	}

	/// <summary>
	/// Streams the update.
	/// </summary>
	void StreamUpdateRequest(NetworkMessage m){
		DLog.Log ("Send update requested from: " + m.conn.connectionId, Color.yellow);

		var msg = m.ReadMessage<ServerClientConstants.RequestObjUpdate> ();
		NetworkServer.SendToClient (Room.allPlayers[m.conn.connectionId].room.host.connectionID, ServerClientConstants.RequestObjUpdateId, msg);

	}

	/// <summary>
	/// Streams the world.
	/// </summary>
	void StreamWorld(NetworkMessage m) {
		
		var msg = m.ReadMessage<ServerClientConstants.SendWorld>();
		int id = msg.connId;
		int done = msg.done;

		if (done == 2) {
			DLog.Log("Got world from host: "+m.conn.connectionId, Color.green);
		}

		msg.connId = id;
		msg.done = done;

		NetworkServer.SendToClient (id, ServerClientConstants.SendWorldId, msg);
	}

}

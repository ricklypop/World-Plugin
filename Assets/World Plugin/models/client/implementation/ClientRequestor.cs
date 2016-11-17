using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class ClientRequestor {
	private NetworkClient network;
	private ClientParser clientParser;
	private Client client;

	public ClientRequestor(ClientSession clientSession){
		this.client = clientSession.client;
		this.network = clientSession.network;
		this.clientParser = clientSession.clientParser;
	}

	#region Server Client Intial Interactions
	/// <summary>
	/// Join a room, with the players on the chosen world.
	/// </summary>
	public void JoinRoom ()
	{
		
		DisableLogging.Logger.Log (network.connection.connectionId + ":Requested Join Room: " + WorldDatabase.currentWorldID, Color.cyan);
		var msg = new ServerClientConstants.JoinRoom ();

		msg.roomID = WorldDatabase.currentWorldID;
		msg.deviceID = SystemInfo.deviceUniqueIdentifier;
		network.Send (ServerClientConstants.JoinRoomId, msg);

	}

	/// <summary>
	/// Requests the world from host of room.
	/// </summary>
	public void RequestWorld ()
	{
		var msg = new ServerClientConstants.RequestWorld ();
		network.Send (ServerClientConstants.RequestWorldId, msg);
	}
	#endregion


	#region Request Using Update Methods
	/// <summary>
	/// Requests the update.
	/// Request to request needed updates, once finished notify the host
	/// </summary>
	public void RequestUpdate ()
	{
		
		if (client.gotWorld && UpdateCache.GetUpdateCount() > 0) {


			byte[] b = clientParser.ParseRequestUpdate ();

			if (b.Length != 0) {

				ServerClientConstants.RequestObjUpdate up = new  ServerClientConstants.RequestObjUpdate ();
				up.objUpdates = b;

				if (clientParser.UpdateRequestCompleted ()) {
					up.done = 1;
				}else {
					up.done = 0;
				}

				up.fromClient = LoadBalancer.connectionID;
				network.Send (ServerClientConstants.RequestObjUpdateId, up);

			}


		}

	}

	#endregion
}

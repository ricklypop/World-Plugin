using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class ClientRequestor
{
	private NetworkClient network;
	private ClientSerializer clientSerializer;
	private ClientLoadBalancer clientBalancer;
	private Client client;

	public ClientRequestor (ClientSession clientSession)
	{
		this.client = clientSession.client;
		this.network = clientSession.network;
		this.clientSerializer = clientSession.clientSerializer;
		this.clientBalancer = clientSession.clientBalancer;
	}

	#region Server Client Intial Interactions

	/// <summary>
	/// Join a room, with the players on the chosen world.
	/// </summary>
	public void JoinRoom ()
	{
		
		DLog.Log (network.connection.connectionId + ":Requested Join Room: " + WorldDatabase.currentWorldID, Color.cyan);
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

		MultiThreading.doTask (client.clientThread, () => {

			Task task = clientSerializer.ParseRequestUpdate ();

			MultiThreading.setOnCompletion (task, () => MultiThreading.doOnMainThread (() => {
				
				if (client.gotWorld && UpdateCache.GetUpdateCount () > 0) {

					byte[] b = (byte[])task.results;

					ServerClientConstants.RequestObjUpdate up = new  ServerClientConstants.RequestObjUpdate ();
					up.objUpdates = b;

					if (b.Length > 0) {
						up.done = 0;
						RequestUpdate ();
					} else {
						up.done = 1;
					}

					up.fromClient = clientBalancer.connectionID;
					network.Send (ServerClientConstants.RequestObjUpdateId, up);

				}

			}));
		});

	}

	#endregion
}

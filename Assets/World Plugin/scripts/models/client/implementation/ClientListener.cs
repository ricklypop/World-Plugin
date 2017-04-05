using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

public class ClientListener {
	private NetworkClient network;
	private ClientSerializer clientSerializer;
	private Client client;

	public ClientListener(ClientSession clientSession){
		this.client = clientSession.client;
		this.network = clientSession.network;
		this.clientSerializer = clientSession.clientSerializer;
	}

	public void RegisterOnNetwork(){
		network.RegisterHandler (ServerClientConstants.SetWorldId, OnSendWorld);
		network.RegisterHandler (ServerClientConstants.JoinedId, OnJoined);
		network.RegisterHandler (ServerClientConstants.RequestObjUpdateId, OnUpdateRequest);
	}

	#region Server and Client Chain Methods

	/// <summary>
	/// Raises the joined event.
	/// Once joined, trash the current world and request the room world
	/// </summary>
	void OnJoined (NetworkMessage m)
	{
		
		DisableLogging.Logger.Log (network.connection.connectionId + ": Joined", Color.cyan);

		WorldDatabase.world = new List<SerializableWorldObject> ();
		WorldObjectCache.ClearCache ();
		
		client.gotWorld = false;

	}
	#endregion

	#region Recieve Request
	/// <summary>
	/// Recieve the update request, and grab the JSON string from the stream.
	/// When done, create a queue of compressed objects and add the objects based on the 
	/// keys from the desrialized list. Then serialize the queue, to start sending updates.
	/// </summary>
	void OnUpdateRequest (NetworkMessage m)
	{
		
		var msg = m.ReadMessage<ServerClientConstants.RequestObjUpdate> ();
		int id = msg.fromClient;

		if (!clientSerializer.recievedUpdateRequest.ContainsKey (id)) {
			
			clientSerializer.recievedUpdateRequest.Add (id, new byte[0]);
			clientSerializer.AddRequestedIndex (id);

		}

		clientSerializer.recievedUpdateRequest[id] = clientSerializer.recievedUpdateRequest[id].Concat(msg.objUpdates).ToArray();
		if (msg.done == 1) {
			
			MultiThreading.setOnCompletion (clientSerializer.SerializeRequestedUpdates (id), 
				() => client.clientSender.SendRequestedUpdates (id));

		}

	}

	/// <summary>
	/// Raises the send world event. Save the id to send to.
	/// </summary>
	void OnSendWorld (NetworkMessage m)
	{
		
		DisableLogging.Logger.Log (network.connection.connectionId
			+ ":Reached Host Set World, Waiting...", Color.cyan);
		
		var recieved = m.ReadMessage<ServerClientConstants.RequestWorld> ();
		int id = recieved.id;

		MultiThreading.setOnCompletion (clientSerializer.SerializeWorld (id), () => {
			client.clientSender.SendWorld(id);
		});

	}

	#endregion

}

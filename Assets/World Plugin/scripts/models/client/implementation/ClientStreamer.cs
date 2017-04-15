using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ClientStreamer {
	private Client client;
	private ClientSerializer clientSerializer;
	private NetworkClient network;

	public ClientStreamer(ClientSession clientSession){
		this.client = clientSession.client;
		this.clientSerializer = clientSession.clientSerializer;
		this.network = clientSession.network;
	}

	public void RegisterOnNetwork(){
		network.RegisterHandler (ServerClientConstants.SendChangesId, GetFromChangesStream);
		network.RegisterHandler (ServerClientConstants.SendWorldId, GetFromWorldStream);
		network.RegisterHandler (ServerClientConstants.SendObjUpdateId, GetFromUpdateStream);
	}

	/// <summary>
	/// Gets from update stream. When done, updates objects from the list.
	/// </summary>
	public void GetFromUpdateStream (NetworkMessage m)
	{
		
		var msg = m.ReadMessage<ServerClientConstants.UpdateObject> ();

		clientSerializer.recievedUpdate = clientSerializer.recievedUpdate.Concat(msg.updates).ToArray();

		if (msg.done == 1) {

			MultiThreading.setOnCompletion(clientSerializer.DeserializeUpdates (), () => {
				MultiThreading.doOnMainThread(() => {
					client.TransferUpdates();
					clientSerializer.lastDeserializedUpdates = null;
				});});

		}

	}

	/// <summary>
	/// Gets from world stream. If reset, reset. If host, get from database. If set, create the world.
	/// </summary>
	public void GetFromWorldStream (NetworkMessage m)
	{

		var recieved = m.ReadMessage<ServerClientConstants.SendWorld> ();

		int r = recieved.done;
		byte[] b = recieved.world;

		if (r == 0) {
			
			clientSerializer.recievedWorld = clientSerializer.recievedWorld.Concat(b).ToArray();

		} else if (r == 1) {

			DisableLogging.Logger.Log ("Client is host... getting world from database.", Color.cyan);
			WorldDatabase.GetWorld ();

		} else if (r == 2) {
			
			WorldObjectCache.ClearCache ();

			MultiThreading.setOnCompletion(clientSerializer.DeserializeWorld (), () => {
				MultiThreading.doOnMainThread(() => {
					WorldDatabase.world = clientSerializer.lastDeserializedWorld;
					clientSerializer.lastDeserializedWorld = null;
					World.CreateWorld();
				});});

		}

	}

	/// <summary>
	/// Transfers the changes to the object communicator, so changes can be made to world objects.
	/// If still getting the world, enqueue the data lost, to request an update when world is set.
	/// </summary>
	public void GetFromChangesStream (NetworkMessage m)
	{
		
		var msg = m.ReadMessage<ServerClientConstants.SendChanges> ();
		int id = msg.id;

        if (!clientSerializer.recievedChanges.ContainsKey(id))
            clientSerializer.recievedChanges.Add(id, new byte[0]);

		clientSerializer.recievedChanges [id] = clientSerializer.recievedChanges[id].Concat (msg.changes).ToArray ();



		if (msg.done == 1) {

			MultiThreading.setOnCompletion(clientSerializer.DeserializeChanges (id), () => {
				MultiThreading.doOnMainThread(() => {
					client.TransferChanges(clientSerializer.lastDeserializedChanges);
					clientSerializer.lastDeserializedChanges = null;
				});});

		}

	}

}

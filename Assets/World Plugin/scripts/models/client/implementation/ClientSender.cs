using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;

public class ClientSender
{
	private NetworkClient network;
	private ClientLoadBalancer clientBalancer;
	private ClientSerializer clientSerializer;
	private Client client;

	public ClientSender (ClientSession clientSession)
	{
		this.client = clientSession.client;
		this.network = clientSession.network;
		this.clientSerializer = clientSession.clientSerializer;
		this.clientBalancer = clientSession.clientBalancer;
	}


	#region Send World and Updates as Requested

	/// <summary>
	/// Sends the updates, by parsed piece.
	/// </summary>
	public void SendRequestedUpdates (int id)
	{

		MultiThreading.doTask (client.clientThread, () => {


			Task task = clientSerializer.ParseUpdates (id);

			MultiThreading.setOnCompletion (task, () => MultiThreading.doOnMainThread (() => {

				byte[] b = (byte[])task.results;

				ServerClientConstants.UpdateObject upd = new ServerClientConstants.UpdateObject ();
				upd.updates = b;

				if (b.Length > 0) {

					upd.done = 0;
					SendRequestedUpdates (id);

				} else {
					
					upd.done = 1;

					clientSerializer.recievedUpdateRequest.Remove (id);
					clientSerializer.RemoveRequestedUpdate (id);

				}

				network.Send (ServerClientConstants.SendObjUpdateId, upd);

			}));
				

		});

	}

	/// <summary>
	/// Sends the world, by parsed piece.
	/// </summary>
	public void SendWorld (int index)
	{
		
		MultiThreading.doTask (client.clientThread, () => {


			if (!WorldDatabase.gettingWorld) {
				
				Task task = clientSerializer.ParseWorldSend (index);

				MultiThreading.setOnCompletion (task, () => MultiThreading.doOnMainThread (() => {

					byte[] b = (byte[])task.results;
					ServerClientConstants.SendWorld msg = new ServerClientConstants.SendWorld ();
					msg.connId = index;

					if (b.Length > 0) {

						msg.world = b;
						network.Send (ServerClientConstants.SendWorldId, msg);
						SendWorld (index);

					} else {

						msg.done = 2;
						network.Send (ServerClientConstants.SendWorldId, msg);

					}

				}));

			}


		});

	}

	/// <summary>
	/// If the list is not empty and there aren't any changes being streamed, set up a JSON string to stream
	/// </summary>
	public void SendChanges ()
	{

		MultiThreading.doTask (client.clientThread, () => {

			Task task = clientSerializer.ParseChangesSend ();

			MultiThreading.setOnCompletion (task, () => MultiThreading.doOnMainThread (() => {

				byte[] b = (byte[])task.results;
				
				ServerClientConstants.SendChanges msg = new ServerClientConstants.SendChanges ();
				msg.changes = b;

				if (b.Length > 0) {
					msg.done = 0;
					SendChanges ();
				} else {
					msg.done = 1;
					clientSerializer.ResetConvertedChanges ();
					client.CheckForChanges ();
				}

				msg.id = clientBalancer.connectionID;
				network.Send (ServerClientConstants.SendChangesId, msg);

			})); 
		});
	}

	#endregion

}

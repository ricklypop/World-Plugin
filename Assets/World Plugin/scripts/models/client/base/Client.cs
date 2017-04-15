using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using DisableLogging;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System;
using System.Threading;

public class Client
{
	public static Client main;	//This is a static method, used to reference the most recent instantiation of the Client

	#region Vars Regarding Client

	public NetworkClient network;//The network
	public bool gotWorld { get; set; }//Used to see if the world has been loaded yet
	public int connectionAttemptCount { get; set; }//How many time the client has tryed to reconnect
	public int clientThread { get; set; }

	#endregion

	public ClientSerializer clientSerializer = new ClientSerializer();
	public ClientLoadBalancer clientBalancer = new ClientLoadBalancer();

	public ClientRequestor clientRequestor{get; set;}
	public ClientDirectory clientDirectory{ get; set; }
	public ClientSender clientSender{ get; set; }
	public ClientStreamer clientStreamer{get; set;}
	public ClientListener clientListener{get; set;}

	#region System Functions

	static Client(){
		new Client();
	}

	//Start this behaviour
	public Client ()
	{

		main = this;
		clientThread = MultiThreading.startNewThread (16384);

		CheckForChanges ();
		CheckDestroyObjects ();

	}

	/// <summary>
	/// Starts the client. 
	/// Create network, create a connection config, configure network, register handlers
	/// </summary>
	public void StartClient (string ip, int port)
	{
		DisableLogging.Logger.Log ("Starting Client...", Color.cyan);

		network = new NetworkClient ();

		ClientSession clientSession = new ClientSession (network, this, clientSerializer, clientBalancer);
		clientRequestor = new ClientRequestor (clientSession);
		clientDirectory = new ClientDirectory (clientSession);
		clientSender = new ClientSender (clientSession);
		clientStreamer = new ClientStreamer (clientSession);
		clientListener = new ClientListener (clientSession);

		ConnectionConfig config = new ConnectionConfig ();
		config.AddChannel (QosType.ReliableSequenced);
		config.AddChannel (QosType.ReliableFragmented);
		config.MaxSentMessageQueueSize = YamlConfig.config.maxSendMessages;
		config.PacketSize = YamlConfig.config.packetSize;
		config.FragmentSize = (ushort)(config.PacketSize - ((ushort)129));
		config.AddChannel (QosType.Unreliable);
		config.ConnectTimeout = 5000;
		config.MinUpdateTimeout = 10;
		config.DisconnectTimeout = 2000;
		config.PingTimeout = 500;
		network.Configure (config, ServerClientConstants.maxConnections);
		network.Connect (ip, port);

		network.RegisterHandler (MsgType.Connect, OnClientConnect);
		network.RegisterHandler (MsgType.Disconnect, OnClientDisconnect);

		network.RegisterHandler (ServerClientConstants.UpdatePlayersId, UpdatePlayer);
		network.RegisterHandler (ServerClientConstants.TestPingId, TestPing);
		network.RegisterHandler (ServerClientConstants.CreateObjectId, CreateObject);
		network.RegisterHandler (ServerClientConstants.RoomFullId, RoomFullDisconnect);
		network.RegisterHandler (ServerClientConstants.DestroyObjectRequestId, DestroyObject);
		network.RegisterHandler (ServerClientConstants.SaveWorldId, SaveWorld);

		clientStreamer.RegisterOnNetwork ();
		clientListener.RegisterOnNetwork ();
		clientDirectory.RegisterOnNetwork ();

	}

	/// <summary>
	/// Stops the client.
	/// Stop the client and network. Unregister handlers and shutdown everything
	/// </summary>
	public void StopClient (bool save)
	{
		if(save)
			World.SaveWorld ();

		Debug.Log ("Stopping client: hostid=" + (connectionAttemptCount));

		network.UnregisterHandler (MsgType.Connect);
		network.UnregisterHandler (MsgType.Disconnect);

		network.UnregisterHandler (ServerClientConstants.UpdatePlayersId);
		network.UnregisterHandler (ServerClientConstants.SendChangesId);
		network.UnregisterHandler (ServerClientConstants.SendWorldId);
		network.UnregisterHandler (ServerClientConstants.SetWorldId);
		network.UnregisterHandler (ServerClientConstants.JoinedId);
		network.UnregisterHandler (ServerClientConstants.TestPingId);
		network.UnregisterHandler (ServerClientConstants.CreateObjectId);
		network.UnregisterHandler (ServerClientConstants.RoomFullId);
		network.UnregisterHandler (ServerClientConstants.DestroyObjectRequestId);
		network.UnregisterHandler (ServerClientConstants.SendObjUpdateId);
		network.UnregisterHandler (ServerClientConstants.SaveWorldId);
		network.UnregisterHandler (ServerClientConstants.RequestServerId);

		network.Shutdown ();

		network = null;
		connectionAttemptCount += 1;
		gotWorld = false;

	}

	/// <summary>
	/// Disconnect this instance of the client.
	/// </summary>
	public static void Disconnect (bool save)
	{
		main.StopClient (save);
	}

	/// <summary>
	/// Raises the client connect event.
	/// When connected, join a room.
	/// </summary>
	void OnClientConnect (NetworkMessage m)
	{
		DisableLogging.Logger.Log (network.connection.connectionId + ":Client Started", Color.cyan);
		if (clientDirectory.wentToDirectory) {
			clientRequestor.JoinRoom ();
		}else {
			clientDirectory.RequestServer ();
		}
	}

	/// <summary>
	/// Raises the client disconnect event.
	/// When disconnect, try to reconnect.
	/// </summary>
	void OnClientDisconnect (NetworkMessage m)
	{
		Reset ();
	}

	/// <summary>
	/// Raises the application quit event.
	/// Saves the world.
	/// </summary>
	void OnApplicationQuit(){
		if(gotWorld)
			World.SaveWorld ();
	}

	/// <summary>
	/// Reset this instance of the client.
	/// </summary>
	void Reset ()
	{
		
		StopClient (false);
		MultiThreading.doTask (clientThread, () => {
			
			Thread.Sleep (1000);

			MultiThreading.doOnMainThread(() => {
				clientDirectory.wentToDirectory = false;
				StartClient (Environment.GetEnvironmentVariable (YamlConfig.DIRECTORY_IP_ENV), 
					int.Parse (Environment.GetEnvironmentVariable (YamlConfig.DIRECTORY_PORT_ENV)));
			});
			
		});

	}

	#endregion

	#region Server Functions

	/// <summary>
	/// The room is full. Disconnect.
	/// </summary>
	void RoomFullDisconnect (NetworkMessage m)
	{
		DisableLogging.Logger.Log (network.connection.connectionId + ":Room is full. Disconnecting...", Color.red);
		Disconnect (false);
	}

	/// <summary>
	/// Test the pings the server.
	/// </summary>
	void TestPing (NetworkMessage m)
	{
		var msg = new ServerClientConstants.TestPinged ();
		network.Send (ServerClientConstants.TestPingedId, msg);
	}
		
	void SaveWorld(NetworkMessage m){
		if(gotWorld)
			World.SaveWorld ();
	}

	#endregion

	/// <summary>
	/// Updates the player.
	/// Update the total amount of players, the player number. Good for load balancing scripts and reseting stuff
	/// </summary>
	void UpdatePlayer (NetworkMessage m)
	{
		var msg = m.ReadMessage<ServerClientConstants.UpdatePlayers> ();
		int totalPlayers = msg.totalPlayers;
		int player = msg.playerNumber;
        int leftConn = msg.leftConnId;

		clientBalancer.totalPlayers = totalPlayers;
		clientBalancer.player = player;
		clientBalancer.connectionID = msg.connNumber;

		clientBalancer.Balance ();
		if (!gotWorld) {
			if (WorldDatabase.world != null)
				WorldDatabase.world.Clear ();
			clientRequestor.RequestWorld ();
		}

        if (leftConn != -1)
        {
            if (clientSerializer.recievedChanges.ContainsKey(leftConn))
                clientSerializer.recievedChanges.Remove(leftConn);

            WorldObjectCache.NotifyPlayerLeft(leftConn);
        }

		if (msg.wasHost == 1) {
			clientSerializer.recievedWorld = new byte[0];
			clientSerializer.recievedUpdate = new byte[0];
		}
		DisableLogging.Logger.Log ("Updated Player:" + totalPlayers + ": " + player, Color.cyan);
	}


	#region Regards Creating and Deleting World Objects

	/// <summary>
	/// Creates the object.
	/// </summary>
	void CreateObject (NetworkMessage m)
	{
		var msg = m.ReadMessage<ServerClientConstants.CreateObject> ();
		DisableLogging.Logger.Log ("Received create object: " + msg.name + ":" + msg.trans, Color.cyan);
		ObjectCommunicator.ClientCreateWorldObject (msg.name, msg.trans, msg.id, msg.own);
	}
		
	/// <summary>
	/// Destroys or enqueues the object.
	/// </summary>
	void DestroyObject (NetworkMessage m)
	{
		var msg = m.ReadMessage<ServerClientConstants.DestroyObject> ();
		ObjectCommunicator.ClientDestroyWorldObject (msg.destroyKey);
	}
		
	//NOTE: THIS IS A PRIVATE METHOD THAT REQUIRES UPDATES
	/// <summary>
	/// Destroys the objects.
	/// </summary>
	void CheckDestroyObjects ()
	{
		
		Task task = null;
		task = MultiThreading.doTask (clientThread, () => {
			
			if (gotWorld && DestroyCache.GetDestroyCount () > 0) {

				MultiThreading.doOnMainThread(() => {
					
					WorldObject obj = DestroyCache.Dequeue ();
					WorldObjectCache.Remove (obj.id);
					GameObject.Destroy (obj.gameObject);
					CheckDestroyObjects();

				});

			}else{

				//MultiThreading.doOnMainThread(() => CheckDestroyObjects ());
				MultiThreading.setOnCompletion(task, () => MultiThreading.doOnMainThread(() => Client.main.CheckDestroyObjects ()));
				//MultiThreading.setOnCompletion(task, () => CheckDestroyObjects ());

			}

		}, false);
	}
	#endregion

	public void TransferChanges(List<Change> changes){
		while (changes.Count > 0) {
			Change c = changes [0];
			if (gotWorld) {
				ObjectCommunicator.SendChangeMessage (c);
			} else {
				UpdateCache.Enqueue (c.id);
				clientSerializer.SerializeRequestUpdates();
			}
			changes.Remove (c);
		}
	}

	public void TransferUpdates(){
		string id = "";
		SerializableWorldObject cM = null;
		foreach (SerializableWorldObject c in clientSerializer.lastDeserializedUpdates) {
			
			id = c.id;
			cM = c;
			
			WorldObjectCache.SetObject(id, cM.DecompressObject ().GetComponent<WorldObject> (), true);
			
		}
	}

	/// <summary>
	/// Requests the send changes.
	/// Request to send the current changes.
	/// Stream the current converted changes to other clients
	/// </summary>
	public void CheckForChanges ()
	{

		Task task = null;
		task = MultiThreading.doTask (clientThread, () => {
			
			//If the list is not empty and there aren't any changes being streamed, set up a JSON string to stream
			if (ChangeCache.GetChangeCount () > 0) {

				MultiThreading.setOnCompletion (clientSerializer.SerializeChanges (), 
					() => clientSender.SendChanges ());
				ChangeCache.Clear ();

			} else {

				//MultiThreading.doOnMainThread(() => CheckForChanges ());
				MultiThreading.setOnCompletion(task, () => MultiThreading.doOnMainThread(() => Client.main.CheckForChanges ()));
				//MultiThreading.setOnCompletion(task, () => CheckForChanges ());

			}  
				
		}, false);

	}

}
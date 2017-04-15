using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;

public class MasterServer : MonoBehaviour {

	public static SortedList<string, Room> rooms = new SortedList<string, Room> ();//The server's rooms

	private static ServerStreamer serverStreamer { get; set; }
	private static ServerSaver serverSaver { get; set; }

	private static int masterServerThread { get; set; }

	static MasterServer(){

		if (YamlConfig.config != null) {
			
			MasterServerClient.main.StartClient ();

			serverStreamer = new ServerStreamer ();
			serverSaver = new ServerSaver ();

			masterServerThread = MultiThreading.startNewThread (int.Parse (Environment.GetEnvironmentVariable (YamlConfig.MASTER_MEMORY_ENV)));

		}
	}


	#region Server Functions
	/// <summary>
	/// Start this instance and the server. Registers handlers.
	/// </summary>
	public static void StartServer(){

		ConnectionConfig config = new ConnectionConfig();
		config.AddChannel(QosType.ReliableSequenced);
		config.AddChannel(QosType.ReliableFragmented);
		config.MaxSentMessageQueueSize = YamlConfig.config.maxSendMessages;
		config.PacketSize = YamlConfig.config.packetSize;
		config.FragmentSize = (ushort)(config.PacketSize - ((ushort)129));
		config.AddChannel(QosType.Unreliable);
		config.ConnectTimeout = 5000;
		config.MinUpdateTimeout = 10;
		config.DisconnectTimeout = 2000;
		config.PingTimeout = 500;
		NetworkServer.Configure(config, ServerClientConstants.maxConnections);
		NetworkServer.RegisterHandler(MsgType.Connect, OnClientConnected);
		NetworkServer.RegisterHandler(MsgType.Disconnect, OnClientDisconnected);

		NetworkServer.RegisterHandler(ServerClientConstants.JoinRoomId,  JoinRoom);
		NetworkServer.RegisterHandler (ServerClientConstants.RequestWorldId, GetWorld);
		NetworkServer.RegisterHandler (ServerClientConstants.RequestCreateObjectId, SendCreateObject);
		NetworkServer.RegisterHandler (ServerClientConstants.DestroyObjectRequestId, SendDestroyObject);

		serverStreamer.RegisterListeners ();

		DisableLogging.Logger.Log("Starting Server... ", Color.cyan);

		NetworkServer.Listen (int.Parse(Environment.GetEnvironmentVariable(YamlConfig.MASTER_PORT_ENV)));
	}

	/// <summary>
	/// Raises the client connected event.
	/// </summary>
	static void OnClientConnected(NetworkMessage mess) {
		
		DisableLogging.Logger.Log("Connected: " + mess.conn.connectionId, Color.green);

	}

	/// <summary>
	/// Raises the client disconnected event. Remove player from room, and remove room if room is empty.
	/// </summary>
	static void OnClientDisconnected(NetworkMessage mess) {
		
		DisableLogging.Logger.Log("Player left: "+mess.conn.connectionId, Color.red);

		if (Room.allPlayers.ContainsKey (mess.conn.connectionId)) {
			Room r = Room.allPlayers [mess.conn.connectionId].room;
			r.RemovePlayer (Room.allPlayers [mess.conn.connectionId]);
			DisableLogging.Logger.Log("Player remove from room id: " + r.id, Color.yellow);
		}

	}
	#endregion

	#region Server and Client Connecting
	/// <summary>
	/// Joins a room. If room is already created, 
	/// . If it isn't created, create the room.
	/// If the room is full, tell the client to disconnect.
	/// </summary>
	static void JoinRoom(NetworkMessage m){
		
		DisableLogging.Logger.Log("Connection Id " + m.conn.connectionId + " requested join room.", Color.yellow);

		var msg = m.ReadMessage<ServerClientConstants.JoinRoom>();
		int id = m.conn.connectionId;

		string roomID = msg.roomID;

		MultiThreading.doTask (masterServerThread, () => {
			
			if (rooms.ContainsKey (roomID) && rooms [roomID].totalPlayers <= ServerClientConstants.roomSize) {

				DisableLogging.Logger.Log ("Joined Room: " + roomID, Color.green);

				Player player = new Player ();
				player.deviceID = msg.deviceID;
				player.connectionID = m.conn.connectionId;
				player.room = rooms [roomID];

				rooms [roomID].AddPlayer (player);

				MultiThreading.doOnMainThread(() => { 
					
					NetworkServer.SendToClient (id, ServerClientConstants.JoinedId, new ServerClientConstants.Joined ());
					MasterServerClient.main.UpdateServer (Room.allPlayers.Count, "", "");

				});

			} else if (!rooms.ContainsKey (roomID)) {
			
				rooms.Add (roomID, new Room ());
				rooms [roomID].id = roomID;

				Player player = new Player ();
				player.deviceID = msg.deviceID;
				player.connectionID = m.conn.connectionId;
				player.room = rooms [roomID];

				rooms [roomID].AddPlayer (player);

				DisableLogging.Logger.Log ("Room Created: " + roomID, Color.green);

				MultiThreading.doOnMainThread(() => { 
					
					NetworkServer.SendToClient (id, ServerClientConstants.JoinedId, new ServerClientConstants.Joined ());
					MasterServerClient.main.UpdateServer (Room.allPlayers.Count, roomID, "");

				});

			} else {
			
				DisableLogging.Logger.Log ("Room is full: " + roomID, Color.red);

				MultiThreading.doOnMainThread(() =>  NetworkServer.SendToClient (id, ServerClientConstants.RoomFullId, new ServerClientConstants.RoomFull ()));

			}

		});
	}

	/// <summary>
	/// Gets the world from client. If its the clients turn, get the world. If the client is the host,
	/// tell the client to download the world from the database. If it isn't the players turn, tell them to
	/// wait. If host left in the middle of stream, reset the the stream.
	/// </summary>
	static void GetWorld(NetworkMessage m) {
		
		DisableLogging.Logger.Log("World set requested from: "+m.conn.connectionId, Color.green);

		int id = Room.allPlayers [m.conn.connectionId].room.host.connectionID;
		var r =  m.ReadMessage<ServerClientConstants.RequestWorld>();

		MultiThreading.doTask (masterServerThread, () => {
			
			if (m.conn.connectionId != id && (r.host == 0 || r.host == id)) {
				
				ServerClientConstants.RequestWorld setW = new ServerClientConstants.RequestWorld ();

				setW.id = m.conn.connectionId;
				setW.host = id;

				MultiThreading.doOnMainThread(() => NetworkServer.SendToClient (id, ServerClientConstants.SetWorldId, setW));

			} else if (m.conn.connectionId == id) {
				
				DisableLogging.Logger.Log ("Host is client. Skipping request...", Color.yellow);

				ServerClientConstants.SendWorld set = new ServerClientConstants.SendWorld ();

				set.done = 1;
				set.world = new byte[0];
				set.connId = m.conn.connectionId;

				MultiThreading.doOnMainThread(() => NetworkServer.SendToClient (id, ServerClientConstants.SendWorldId, set));

			} else if (r.host != id) {
				
				DisableLogging.Logger.Log ("Host left, restarting stream...", Color.yellow);

				ServerClientConstants.SendWorld set = new ServerClientConstants.SendWorld ();

				set.done = 3;
				set.world = new byte[0];
				set.connId = m.conn.connectionId;

				MultiThreading.doOnMainThread(() => NetworkServer.SendToClient (id, ServerClientConstants.SendWorldId, set));

			}

		});

	}
	#endregion

	#region Server Redirect to other Client

	/// <summary>
	/// Sends the create object.
	/// </summary>
	static void SendCreateObject(NetworkMessage m){
		var msg = m.ReadMessage<ServerClientConstants.CreateObject> ();

		DisableLogging.Logger.Log ("Create Object Requested: " + msg.trans, Color.green);

		MultiThreading.doTask (masterServerThread, () => {
			foreach (Player p in Room.allPlayers[m.conn.connectionId].room.players) {
			
				if (m.conn.connectionId != p.connectionID) {
					
					MultiThreading.doOnMainThread(() => NetworkServer.SendToClient (p.connectionID, ServerClientConstants.CreateObjectId, msg));

				}

			}
		});

	}

	/// <summary>
	/// Sends the destroy object.
	/// </summary>
	static void SendDestroyObject(NetworkMessage m){
		var msg = m.ReadMessage<ServerClientConstants.DestroyObject> ();

		DisableLogging.Logger.Log ("Destroy Object Requested: " + msg.destroyKey, Color.green);

		MultiThreading.doTask (masterServerThread, () => {
			foreach (Player p in Room.allPlayers[m.conn.connectionId].room.players) {
			
				if (m.conn.connectionId != p.connectionID) {
					
					MultiThreading.doOnMainThread(() => NetworkServer.SendToClient (p.connectionID, ServerClientConstants.DestroyObjectRequestId, msg) );

				}

			}
		});

	}
	#endregion

}
	
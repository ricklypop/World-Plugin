using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;

public class DirectoryServer {
	
	private static Dictionary<int, Server> servers = new Dictionary<int, Server> ();
	private static int directoryThreadID{ get; set; }

	static DirectoryServer(){
		
		directoryThreadID = MultiThreading.startNewThread (int.Parse(Environment.GetEnvironmentVariable(YamlConfig.DIRECTORY_MEMORY_ENV)));

	}

	#region Script Base Functions
	public static void StartServer(){

		DLog.Log("Starting Directory Server... ", Color.cyan);

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

		NetworkServer.Listen (int.Parse(Environment.GetEnvironmentVariable(YamlConfig.DIRECTORY_PORT_ENV)));

		NetworkServer.RegisterHandler(MsgType.Disconnect, OnDisconnect);

		NetworkServer.RegisterHandler(ServerClientConstants.RequestServerId, SendServerConnection);
		NetworkServer.RegisterHandler(ServerClientConstants.ConnectServerId, ConnectServer);
		NetworkServer.RegisterHandler(ServerClientConstants.UpdateServerId, UpdateServer);

		DLog.Log("Directory Server Started ", Color.cyan);

	}
	#endregion

	#region Directory Server Tools
	static Server CheckForRoom(string room){
		foreach (Server s in servers.Values)
			if (s.rooms.Contains (room))
				return s;
		return null;
	}

	static Server LeastPopulatedServer(){
		Server index = null;
		foreach (Server s in servers.Values)
			if (index == null) {
				index = s;
			} else if (s.numPlayers < index.numPlayers) {
				index = s;
			}
		return index;
	}
	#endregion

	#region Server Functions
	static void OnDisconnect(NetworkMessage m){

		if (servers.ContainsKey (m.conn.connectionId)) {
			DLog.Log ("Server disconnected: " + m.conn.connectionId, Color.yellow);
			servers.Remove (m.conn.connectionId);
		}

	}

	static void SendServerConnection(NetworkMessage m){
		
		var msg = m.ReadMessage<ServerClientConstants.RequestServer> ();
		int id = m.conn.connectionId;

		DLog.Log ("Got client: " + id, Color.cyan);

		try{
		MultiThreading.doTask (directoryThreadID, () => {

			Server sendTo = CheckForRoom (msg.room);
			if (sendTo == null)
				sendTo = LeastPopulatedServer ();

			ServerClientConstants.RequestServer message = new ServerClientConstants.RequestServer ();
			message.ip = sendTo.ip;
			message.port = sendTo.port;

			DLog.Log ("Assigned server: " + sendTo.ip + ", " + sendTo.port, Color.cyan);

			MultiThreading.doOnMainThread(() => 
				NetworkServer.SendToClient (id, ServerClientConstants.RequestServerId, message));

		});
		}catch(Exception e){
			Debug.Log (e);
		}

	}
		
	static void ConnectServer(NetworkMessage m){
		var msg = m.ReadMessage<ServerClientConstants.ConnectServer> ();

		Server server = new Server ();

		server.ip = msg.ip;
		server.port = msg.port;
		server.connectionID = m.conn.connectionId;
		servers.Add (m.conn.connectionId, server);

		DLog.Log ("Connected Server: " + server.ip + ":" + server.port, Color.yellow);

	}

	static void UpdateServer(NetworkMessage m){

		DLog.Log ("Updating Server: " + m.conn.connectionId, Color.yellow);

		var msg = m.ReadMessage<ServerClientConstants.UpdateServer> ();
		servers [m.conn.connectionId].numPlayers = msg.numPlayers;
		if(msg.addRoom != "" && msg.addRoom != null )
			servers [m.conn.connectionId].rooms.Add (msg.addRoom);
		if(msg.removeRoom != "" && msg.removeRoom != null )
			servers [m.conn.connectionId].rooms.Remove (msg.removeRoom);
	}
	#endregion
}

public class Server{
	public List<string> rooms = new List<string> ();
	public string ip{ get; set; }
	public int port { get; set; }
	public int numPlayers { get; set; }
	public int connectionID{ get; set; }
}
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System;

public class MasterServerClient {
	
	public static MasterServerClient main;

	private NetworkClient network;//The network
	private bool clientStarted;

	static MasterServerClient(){
		
		main = new MasterServerClient();

	}

	// Use this for initialization
	public void StartClient () {

		DisableLogging.Logger.Log ("Starting Server Client...", Color.cyan);

		network = new NetworkClient ();
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

		network.RegisterHandler (MsgType.Connect, OnClientConnect);

		network.Connect (Environment.GetEnvironmentVariable(YamlConfig.DIRECTORY_IP_ENV),
			int.Parse(Environment.GetEnvironmentVariable(YamlConfig.DIRECTORY_PORT_ENV)));

	}
		
	void OnClientConnect(NetworkMessage m){

		DisableLogging.Logger.Log ("Server Client Connected to Directory.", Color.cyan);

		ServerClientConstants.ConnectServer connect = new ServerClientConstants.ConnectServer ();

		connect.ip = YamlConfig.config.masterIP;
		connect.port = YamlConfig.config.masterPort;

		network.Send (ServerClientConstants.ConnectServerId, connect);

	}

	public void UpdateServer(int players, string addRoom, string removeRoom){

		ServerClientConstants.UpdateServer update = new ServerClientConstants.UpdateServer ();

		update.numPlayers = players;
		update.addRoom = addRoom;
		update.removeRoom = removeRoom;

		network.Send (ServerClientConstants.UpdateServerId, update);

	}

}

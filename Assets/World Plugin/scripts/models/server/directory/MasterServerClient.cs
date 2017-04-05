﻿using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class MasterServerClient : MonoBehaviour {
	public static MasterServerClient main;

	public NetworkClient network;//The network

	public string ip;
	public int port;

	private bool clientStarted;
	// Use this for initialization
	void StartClient () {
		
		main = this;

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

		network.Connect (YamlConfig.config.ip, YamlConfig.config.port);

	}

	public void UpdateServer(int players, string addRoom, string removeRoom){
		
		ServerClientConstants.UpdateServer update = new ServerClientConstants.UpdateServer ();

		update.numPlayers = players;
		update.addRoom = addRoom;
		update.removeRoom = removeRoom;

		network.Send (ServerClientConstants.UpdateServerId, update);

	}

	void Update () {
		if (YamlConfig.config != null && !clientStarted) {
			StartClient ();
			clientStarted = true;
		}
	}
		
	void OnClientConnect(NetworkMessage m){

		DisableLogging.Logger.Log ("Server Client Connected to Directory.", Color.cyan);

		ServerClientConstants.ConnectServer connect = new ServerClientConstants.ConnectServer ();

		connect.ip = ip;
		connect.port = port;

		network.Send (ServerClientConstants.ConnectServerId, connect);

	}

}
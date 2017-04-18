﻿using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Threading;

public class ClientDirectory {
	private NetworkClient network;
	private Client client;

	public bool wentToDirectory{ get; set; }

	public ClientDirectory(ClientSession clientSession){
		this.client = clientSession.client;
		this.network = clientSession.network;
	}

	public void RegisterOnNetwork ()
	{
		network.RegisterHandler (ServerClientConstants.RequestServerId, GetServer);
	}

	/// <summary>
	/// Reset this instance of the client.
	/// </summary>
	public void ConnectToServer (string ip, int port)
	{
		
		client.StopClient (false);

		MultiThreading.doTask (client.clientThread, () => {

			Thread.Sleep (1000);

			MultiThreading.doOnMainThread(() => {
				
				client.StartClient (ip, port);
				client.clientDirectory.wentToDirectory = true;

			});

		});

	}

	public void GetServer(NetworkMessage m){
		
		var msg = m.ReadMessage<ServerClientConstants.RequestServer> ();

		string ip = msg.ip;
		int port = msg.port;

		DLog.Log ("Connecting to: " + ip + ":" + port, Color.yellow);
		ConnectToServer (ip, port);

	}

	public void RequestServer(){
		
		ServerClientConstants.RequestServer server = new ServerClientConstants.RequestServer ();

		server.room = WorldDatabase.currentWorldID;

		network.Send (ServerClientConstants.RequestServerId, server);

	}
}

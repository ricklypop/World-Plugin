using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class ClientSession {
	public NetworkClient network{ get; set; }
	public Client client { get; set; }
	public ClientSerializer clientSerializer{ get; set; }
	public ClientLoadBalancer clientBalancer{ get; set; }

	public ClientSession(NetworkClient network, Client client, 
		ClientSerializer clientSerializer, ClientLoadBalancer clientBalancer){
		this.network = network;
		this.client = client;
		this.clientSerializer = clientSerializer;
		this.clientBalancer = clientBalancer;
	}
}

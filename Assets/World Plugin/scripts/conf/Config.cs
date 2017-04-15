using UnityEngine;
using System.Collections;

public class Config {
	public string address { get; set; }
	public string directoryIP { get; set; }
	public int directoryPort { get; set; }
	public int directoryMemory { get; set; }
	public string masterIP { get; set; }
	public int masterPort { get; set; }
	public int masterMemory { get; set; }

	public int version {get; set;}

	public int maxBytes{ get; set;}
	public ushort packetSize{ get; set;}
	public ushort maxSendMessages{ get; set;}
	public int minTotalPlayers{ get; set;}
}

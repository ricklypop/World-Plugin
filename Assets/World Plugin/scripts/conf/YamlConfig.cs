using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using YamlDotNet;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System;

public class YamlConfig {
	public static Config config;//The static loaded config

	#region Config Defaults
	public static readonly int MAX_BYTES = 1200;
	public static readonly ushort PACKET_SIZE = 1400;
	public static readonly ushort MAX_SEND_MESSAGES = 128;
	public static readonly int MIN_TOTAL_PLAYER = 1;

	public static readonly string CONFIG_ADDRESS = "http://10.18.248.102:8080/WorldService/ws";
	public static readonly string DIRECTORY_IP = "127.0.0.1";
	public static readonly int DIRECTORY_PORT = 8080;
	public static readonly int DIRECTORY_MEMORY = 1048576;
	public static readonly int VERSION = 1;
	#endregion

	public static readonly string WORLD_DATABASE_ADDRESS_ENV = "WORLD_DATABASE_ADDRESS";
	public static readonly string DIRECTORY_IP_ENV = "WORLD_DIRECTORY_ADDRESS";
	public static readonly string DIRECTORY_PORT_ENV = "WORLD_DIRECTORY_PORT";
	public static readonly string DIRECTORY_MEMORY_ENV = "WORLD_DIRECTORY_MEMORY";

	static YamlConfig(){
		new YamlConfig ();
	}

	#region Script Functions
	public YamlConfig(){
		string load = LoadConfig ();
		if (load == null || load == "") {
			CreateDefault ();
			SetEnviroment ();
		}else{
			StringReader input = new StringReader (load);
			Deserializer deserialize = new Deserializer (namingConvention: new CamelCaseNamingConvention());
			config = deserialize.Deserialize<Config> (input);
			SetEnviroment ();
			if (config.version != VERSION)
				CreateDefault ();
		}
	}
	#endregion

	void SetEnviroment(){
		if (Environment.GetEnvironmentVariable(WORLD_DATABASE_ADDRESS_ENV) == null) {
			Environment.SetEnvironmentVariable (WORLD_DATABASE_ADDRESS_ENV, config.address);
		}

		if (Environment.GetEnvironmentVariable(DIRECTORY_IP_ENV) == null) {
			Environment.SetEnvironmentVariable (DIRECTORY_IP_ENV, config.ip);
		}

		if (Environment.GetEnvironmentVariable(DIRECTORY_PORT_ENV) == null) {
			Environment.SetEnvironmentVariable (DIRECTORY_PORT_ENV, config.port.ToString());
		}
			
		if (Environment.GetEnvironmentVariable(DIRECTORY_PORT_ENV) == null) {
			Environment.SetEnvironmentVariable (DIRECTORY_MEMORY_ENV, config.memory.ToString());
		}

	}

	#region Config Functions
	/// <summary>
	/// Creates a default config.
	/// </summary>
	void CreateDefault(){ 
		config = new Config ();
		config.address = CONFIG_ADDRESS;
		config.ip = DIRECTORY_IP;
		config.port = DIRECTORY_PORT;
		config.memory = DIRECTORY_MEMORY;
		config.version = VERSION;

		config.maxBytes = MAX_BYTES;
		config.minTotalPlayers = MIN_TOTAL_PLAYER;
		config.packetSize = PACKET_SIZE;
		config.maxSendMessages = MAX_SEND_MESSAGES;

		StringBuilder stringBuilder = new StringBuilder ();
		StringWriter stringWriter = new StringWriter (stringBuilder);

		Serializer serializer = new Serializer ();
		serializer.Serialize (stringWriter, config);
		SaveConfig (stringBuilder.ToString());
	}

	/// <summary>
	/// Loads the config.
	/// </summary>
	/// <returns>The config.</returns>
	string LoadConfig ()
	{
		try {
			XmlSerializer xml = new XmlSerializer (typeof(string));
			FileStream file;
			file = new FileStream (Application.persistentDataPath + "/config.ini", FileMode.Open, FileAccess.Read, FileShare.None);
			string con = xml.Deserialize (file).ToString (); 
			file.Close ();
			return con;
		} catch (Exception e) {
			DisableLogging.Logger.Log ("File does not exist! Error: " + e.StackTrace, Color.yellow);
		}
		return null;
	}

	/// <summary>
	/// Saves the config.
	/// </summary>
	void SaveConfig (string config)
	{
		XmlSerializer xml = new XmlSerializer (typeof(string));
		FileStream file = File.Create (Application.persistentDataPath + "/config.ini");

		xml.Serialize (file, config);
		file.Close ();
	}
	#endregion
}

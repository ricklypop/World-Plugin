
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Networking;
using DisableLogging;
using System;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using UnityEngine;
using System.Net;
using System.Collections.Specialized;

public class WorldDatabase : Update
{
	#region Static Database Values

	public static string currentWorldID = SystemInfo.deviceUniqueIdentifier;

	public static List<SerializableWorldObject> world { get; set; }

	public static SortedList<CharString, string> worldList = new SortedList<CharString, string> ();
	public static bool gettingWorld = false;
	public static bool getLastWorld = false;
	public static string checkID = "";
	public static string worldName = "";
	public static string address = null;

	private static readonly string PLAYER_CRED = SystemInfo.deviceUniqueIdentifier;

	private static int databaseThreadID { get; set; }

	#endregion

	static WorldDatabase ()
	{
		databaseThreadID = MultiThreading.startNewThread (10485760);
		new WorldDatabase ();
	}

	public WorldDatabase () : base (){}
	public static void Start (){}

	public override void OnApplicationQuit (){}
	public override void OnUpdate ()
	{
		if (address == null && YamlConfig.config != null) {
			
			address = Environment.GetEnvironmentVariable (YamlConfig.WORLD_DATABASE_ADDRESS_ENV);

			if (address != null) {
				GetWorldList ();
				CheckID ();
			}
		}

		if (checkID == "true") {
			checkID = "";
			GetLastWorld ();
		} else if (checkID == "false") {
			checkID = "";
			World.GenerateWorld ();
			PutID ();
		}

		if (getLastWorld) {
			
			getLastWorld = false;
			Client.main.StartClient (Environment.GetEnvironmentVariable(YamlConfig.DIRECTORY_IP_ENV), 
				int.Parse(Environment.GetEnvironmentVariable(YamlConfig.DIRECTORY_PORT_ENV)));
			
		}
	}

	public static HttpWebRequest CreateWebRequest (string uri,
	                                               string requestMethod, string contentType)
	{
		HttpWebRequest req = null;

		req = (HttpWebRequest)WebRequest.Create (uri);
		req.KeepAlive = false;
		req.Method = requestMethod;

		req.ContentType = contentType;
		req.AllowAutoRedirect = false;

		return req;
	}

	#region Get Calls

	/// <summary>
	/// Gets the world list of ids.
	/// </summary>
	public static void GetWorldList ()
	{
		MultiThreading.doTask (databaseThreadID, () => {
			try {
				string uri = address + "/getWorldList";

				HttpWebRequest req = CreateWebRequest (uri, "GET", "text/html");
				req.Headers.Set (HttpRequestHeader.Authorization, "Basic " + System.Convert.ToBase64String (
					System.Text.Encoding.ASCII.GetBytes (PLAYER_CRED + ": ")));

				req.Timeout = 100000;

				WebResponse response = req.GetResponse ();
				Stream dataStream = response.GetResponseStream ();
				StreamReader reader = new StreamReader (dataStream);

				string data = reader.ReadToEnd ();

				worldList.Clear ();
				Dictionary<string, string> res = new Dictionary<string, string> ();
				if (data.Replace (" ", "") != "" && data.Replace (" ", "") != "{}")
					res = JsonConvert.DeserializeObject<Dictionary<string, string>> (data);
				if (response == null)
					res = new Dictionary<string, string> ();
				foreach (string key in res.Keys)
					worldList.Add (new CharString (key), res [key]);
			
				if (data.Replace (" ", "") == "")
					WorldDatabaseInit.main.SetRetry (GetWorldList);
				else
					WorldDatabaseInit.main.show = false;
				
			} catch (Exception e) {
				WorldDatabaseInit.main.SetRetry (GetWorld);
				Debug.Log (e);
			}
		});
	}

	/// <summary>
	/// Gets the world based on a given id.
	/// </summary>
	public static void GetWorld ()
	{
		MultiThreading.doTask (databaseThreadID, () => {
			try {
				gettingWorld = true;
				string uri = address + "/getWorld/" + currentWorldID;

				HttpWebRequest req = CreateWebRequest (uri, "GET", "text/html");
				req.Headers.Set (HttpRequestHeader.Authorization, "Basic " + System.Convert.ToBase64String (
					System.Text.Encoding.ASCII.GetBytes (PLAYER_CRED + ": ")));

				req.Timeout = 100000;

				WebResponse response = req.GetResponse ();
				Stream dataStream = response.GetResponseStream ();
				StreamReader reader = new StreamReader (dataStream);

				world = JsonConvert.DeserializeObject<List<SerializableWorldObject>> (reader.ReadToEnd ());

				if (world == null)
					world = new List<SerializableWorldObject> ();
				World.CreateWorld ();
				gettingWorld = false;
				WorldDatabaseInit.main.show = false;

				reader.Close ();
				response.Close ();
			} catch (Exception e) {
				WorldDatabaseInit.main.SetRetry (GetWorld);
				Debug.Log (e);
			}
		});
	}

	/// <summary>
	/// Gets the name of the world.
	/// </summary>
	public static void GetName ()
	{
		MultiThreading.doTask (databaseThreadID, () => {
			try {
				string uri = address + "/getWorldName/" + currentWorldID;
				HttpWebRequest req = CreateWebRequest (uri, "GET", "text/html");
				req.Headers.Set (HttpRequestHeader.Authorization, "Basic " + System.Convert.ToBase64String (
					System.Text.Encoding.ASCII.GetBytes (PLAYER_CRED + ": ")));

				req.Timeout = 10000;

				WebResponse response = req.GetResponse ();
				Stream dataStream = response.GetResponseStream ();
				StreamReader reader = new StreamReader (dataStream);

				worldName = reader.ReadToEnd ();
				WorldDatabaseInit.main.show = false;
				Debug.Log ("Response: " + worldName);

				reader.Close ();
				response.Close ();
			} catch (Exception e) {
				WorldDatabaseInit.main.SetRetry (GetName);
				Debug.Log (e);
			}
		});
	}

	/// <summary>
	/// Gets the last world the unique id was on.
	/// </summary>
	public static void GetLastWorld ()
	{
		MultiThreading.doTask (databaseThreadID, () => {
			try {
				getLastWorld = false;
				string uri = address + "/getLastWorld";

				HttpWebRequest req = CreateWebRequest (uri, "GET", "text/html");
				req.Headers.Set (HttpRequestHeader.Authorization, "Basic " + System.Convert.ToBase64String (
					System.Text.Encoding.ASCII.GetBytes (PLAYER_CRED + ": ")));

				req.Timeout = 10000;

				WebResponse response = req.GetResponse ();
				Stream dataStream = response.GetResponseStream ();
				StreamReader reader = new StreamReader (dataStream);

				currentWorldID = reader.ReadToEnd ();
				getLastWorld = true;
				WorldDatabaseInit.main.show = false;

				Debug.Log ("Response: " + currentWorldID);
				reader.Close ();
				response.Close ();
			} catch (Exception e) {
				WorldDatabaseInit.main.SetRetry (GetLastWorld);
				Debug.Log (e);
			}
		});
	}

	/// <summary>
	/// Checks the ID to see if it exists.
	/// </summary>
	public static void CheckID ()
	{
		MultiThreading.doTask (databaseThreadID, () => {
			try {
				string uri = address + "/checkID/" + PLAYER_CRED;

				HttpWebRequest req = CreateWebRequest (uri, "GET", "text/html");

				req.Timeout = 10000;

				WebResponse response = req.GetResponse ();
				Stream dataStream = response.GetResponseStream ();
				StreamReader reader = new StreamReader (dataStream);

				checkID = reader.ReadToEnd ();
				if (checkID.Replace (" ", "") == "")
					WorldDatabaseInit.main.SetRetry (CheckID);
				else
					WorldDatabaseInit.main.show = false;
			} catch (Exception e) {
				WorldDatabaseInit.main.SetRetry (GetLastWorld);
				Debug.Log (e);
			}
		});
	}

	#endregion

	#region Put Calls

	static string convertToForm(Dictionary<string, string> data){
		string postData = "";
		
		foreach (string key in data.Keys)
		{
			postData += WWW.EscapeURL(key) + "="
				+ WWW.EscapeURL(data[key]) + "&";
		}

		return postData;
	}

	/// <summary>
	/// Puts the device ID in the database.
	/// </summary>
	public static void PutID ()
	{
		MultiThreading.doTask (databaseThreadID, () => {
			try {
				string uri = address + "/putID";

				HttpWebRequest req = CreateWebRequest (uri, "POST", "application/x-www-form-urlencoded");
				req.Headers.Set (HttpRequestHeader.Authorization, "Basic " + System.Convert.ToBase64String (
					System.Text.Encoding.ASCII.GetBytes (PLAYER_CRED + ": ")));

				Dictionary<string, string> data = new Dictionary<string, string>();
				data.Add("id", PLAYER_CRED);
				
				byte[] byteArray = Encoding.UTF8.GetBytes (convertToForm(data));
				req.ContentLength = byteArray.Length;

				Stream dataStream = req.GetRequestStream ();
				
				dataStream.Write (byteArray, 0, byteArray.Length);
				dataStream.Close ();

				WebResponse response = req.GetResponse ();
				dataStream = response.GetResponseStream ();

				StreamReader reader = new StreamReader (dataStream);

				string responseFromServer = reader.ReadToEnd ();
				Debug.Log (responseFromServer);

				WorldDatabaseInit.main.show = false;

				PutLastWorld ();

				reader.Close ();
				dataStream.Close ();
				response.Close ();
			} catch (Exception e) {
				WorldDatabaseInit.main.SetRetry (PutID);
				Debug.Log (e);
			}
		});
	}

	/// <summary>
	/// Puts the world in the database.
	/// </summary>
	public static void PutWorld ()
	{
		MultiThreading.doTask (databaseThreadID, () => {
			try {
				string uri = address + "/putWorld";

				HttpWebRequest req = CreateWebRequest (uri, "POST", "application/x-www-form-urlencoded");
				req.Headers.Set (HttpRequestHeader.Authorization, "Basic " + System.Convert.ToBase64String (
					System.Text.Encoding.ASCII.GetBytes (PLAYER_CRED + ": ")));

				Dictionary<string, string> data = new Dictionary<string, string>();
				data.Add("world", JsonConvert.SerializeObject(world));
				data.Add("id", currentWorldID);

				byte[] byteArray = Encoding.UTF8.GetBytes (convertToForm(data));
				req.ContentLength = byteArray.Length;

				Stream dataStream = req.GetRequestStream ();
				
				dataStream.Write (byteArray, 0, byteArray.Length);
				dataStream.Close ();

				WebResponse response = req.GetResponse ();
				dataStream = response.GetResponseStream ();

				StreamReader reader = new StreamReader (dataStream); 

				string responseFromServer = reader.ReadToEnd ();
				Debug.Log (responseFromServer);

				WorldDatabaseInit.main.show = false;

				reader.Close ();
				dataStream.Close ();
				response.Close ();
			} catch (Exception e) {
				WorldDatabaseInit.main.SetRetry (PutID);
				Debug.Log (e);
			}
		});
	}

	/// <summary>
	/// Puts the last world id, the unique device id was on.
	/// </summary>
	public static void PutLastWorld ()
	{
		MultiThreading.doTask (databaseThreadID, () => {
			try {
				string uri = address + "/putLastWorld";

				HttpWebRequest req = CreateWebRequest (uri, "POST", "application/x-www-form-urlencoded");
				req.Headers.Set (HttpRequestHeader.Authorization, "Basic " + System.Convert.ToBase64String (
					System.Text.Encoding.ASCII.GetBytes (PLAYER_CRED + ": ")));

				Dictionary<string, string> data = new Dictionary<string, string>();
				data.Add("lastWorld", currentWorldID);

				byte[] byteArray = Encoding.UTF8.GetBytes (convertToForm(data));
				req.ContentLength = byteArray.Length;

				Stream dataStream = req.GetRequestStream ();
				
				dataStream.Write (byteArray, 0, byteArray.Length);
				dataStream.Close ();

				WebResponse response = req.GetResponse ();
				dataStream = response.GetResponseStream ();

				StreamReader reader = new StreamReader (dataStream);

				string responseFromServer = reader.ReadToEnd ();
				Debug.Log (responseFromServer);

				WorldDatabaseInit.main.show = false;

				reader.Close ();
				dataStream.Close ();
				response.Close ();
			} catch (Exception e) {
				WorldDatabaseInit.main.SetRetry (PutID);
				Debug.Log (e);
			}
		});
	}

	/// <summary>
	/// Puts the name of the world in the database.
	/// </summary>
	public static void PutWorldName ()
	{
		MultiThreading.doTask (databaseThreadID, () => {
			try {
				string uri = address + "/putWorldName";

				HttpWebRequest req = CreateWebRequest (uri, "POST", "application/x-www-form-urlencoded");
				req.Headers.Set (HttpRequestHeader.Authorization, "Basic " + System.Convert.ToBase64String (
					System.Text.Encoding.ASCII.GetBytes (PLAYER_CRED + ": ")));

				Dictionary<string, string> data = new Dictionary<string, string>();
				data.Add("worldName", worldName);

				byte[] byteArray = Encoding.UTF8.GetBytes (convertToForm(data));
				req.ContentLength = byteArray.Length;

				Stream dataStream = req.GetRequestStream ();
				
				dataStream.Write (byteArray, 0, byteArray.Length);
				dataStream.Close ();

				WebResponse response = req.GetResponse ();
				dataStream = response.GetResponseStream ();

				StreamReader reader = new StreamReader (dataStream);

				string responseFromServer = reader.ReadToEnd ();
				Debug.Log (responseFromServer);

				WorldDatabaseInit.main.show = false;

				reader.Close ();
				dataStream.Close ();
				response.Close ();
			} catch (Exception e) {
				WorldDatabaseInit.main.SetRetry (PutID);
				Debug.Log (e);
			}
		});
	}

	#endregion
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

public class ClientSerializer : ClientParser {
	//Recieving World
	public byte[] recievedWorld {get; set;}//The recieved JSON world string that will be converted when stream has ended
	
	//Recieving Updates
	public byte[] recievedUpdate { get; set; }
	public Dictionary<int, byte[]> recievedUpdateRequest{get; set;}//The recieved population, a JSON string representing a list of int ids
	
	//Recieiving Changes
	public Dictionary<int, byte[]> recievedChanges{get; set;}

	//Deserialization
	public List<SerializableWorldObject> lastDeserializedWorld{get; set;}
	public List<SerializableWorldObject> lastDeserializedUpdates{get; set;}
	public List<Change> lastDeserializedChanges{get; set;}


	public ClientSerializer(){

		recievedUpdate= new byte[0];
		recievedUpdateRequest =  new Dictionary<int, byte[]>();
		recievedWorld = new byte[0];
		recievedChanges =  new Dictionary<int, byte[]>();

	}

	public Task SerializeRequestedUpdates(int id){
		
		Queue<SerializableWorldObject> compressed = new Queue<SerializableWorldObject> ();
		
		
		List<int> worldObjects = JsonConvert.DeserializeObject<List<int>>(
			System.Text.Encoding.ASCII.GetString(CLZF2.Decompress(recievedUpdateRequest[id])));
		
		
		foreach(int i in worldObjects){
			compressed.Enqueue (WorldObjectCache.GetObjectByIndex(i).CompressWorldObject ());
		}

		return MultiThreading.doTask(parserThreadID, () => {
			recievedUpdateRequest[id] = CLZF2.Compress(System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject (compressed)));
		});
		
	}
	
	public Task SerializeRequestUpdates(){
		return MultiThreading.doTask(parserThreadID, () => {
			convertedUpdateRequest = CLZF2.Compress (System.Text.Encoding.ASCII.GetBytes (JsonConvert.SerializeObject (UpdateCache.UPDATEQUEUE)));
		});
	}
	
	public Task SerializeChanges ()
	{
		
		return MultiThreading.doTask(parserThreadID, () => {
			convertedChanges = CLZF2.Compress (System.Text.Encoding.ASCII.GetBytes (
				JsonConvert.SerializeObject (ChangeCache.CHANGEQUEUE)));
		});
		
	}
	
	public Task SerializeWorld (int id)
	{
		
		List<SerializableWorldObject> compressed = WorldObjectCache.GetCompressed ();
		
		
		if (!currentWorldIndexs.ContainsKey (id)) {
			
			currentWorldIndexs.Add (id, 0);
			convertedWorlds.Add (id, new byte[0]);
			
		}
		
		return MultiThreading.doTask(parserThreadID, () => {
			convertedWorlds [id] = CLZF2.Compress (System.Text.Encoding.ASCII.GetBytes (JsonConvert.SerializeObject (compressed)));
		});
		
	}

	public Task DeserializeWorld(){
		
		return MultiThreading.doTask (parserThreadID, () => {

			lastDeserializedWorld = JsonConvert.DeserializeObject <List<SerializableWorldObject>> (
				System.Text.Encoding.ASCII.GetString (CLZF2.Decompress (recievedWorld)));

			recievedWorld = new byte[0];

		});
		
	}
	
	public Task DeserializeUpdates(){
		
		return MultiThreading.doTask(parserThreadID, () => {

			lastDeserializedUpdates = JsonConvert.DeserializeObject<List<SerializableWorldObject>>(System.Text.Encoding.ASCII.GetString(
				CLZF2.Decompress(recievedUpdate)));

			recievedUpdate = new byte[0];

		});

	}

	public Task DeserializeChanges(int id){
		
		return MultiThreading.doTask(parserThreadID, () => {
			
			lastDeserializedChanges = JsonConvert.DeserializeObject<List<Change>> (System.Text.Encoding.ASCII.GetString(
				CLZF2.Decompress(recievedChanges[id])));

			recievedChanges[id] = new byte[0];

		});
		
	}
}

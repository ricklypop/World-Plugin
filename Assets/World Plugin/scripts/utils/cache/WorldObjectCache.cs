using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class WorldObjectCache {
	public static readonly SortedList<CharString, WorldObject> WORLDOBJECTS = 
		new SortedList<CharString, WorldObject> ();

	public enum Types{NONE = 0, TESTCUBE = 1, WORLDDATA = 2}

	public static void Add (string id, WorldObject worldObject)
	{
		WORLDOBJECTS.Add (new CharString (id), worldObject);
	}

	public static void Remove (string id)
	{
		WORLDOBJECTS [new CharString(id)] = null;
	}

	public static int GetLength ()
	{
		return WORLDOBJECTS.Count;
	}

	public static void SetObject (string id, WorldObject worldObject, bool destroy)
	{
		if(destroy)
			GameObject.Destroy(WORLDOBJECTS[new CharString(id)]);
		
		WORLDOBJECTS [new CharString(id)] = worldObject;

	}

    public static void NotifyPlayerLeft(int l)
    {
        foreach (WorldObject o in WORLDOBJECTS.Values)
        {
            o.PlayerLeft(l);
        }
    }

    public static List<SerializableWorldObject> GetCompressed ()
	{
		List<SerializableWorldObject> compressed = new List<SerializableWorldObject> ();

		foreach (WorldObject o in WORLDOBJECTS.Values){
			
			if (o != null) {
				compressed.Add (o.CompressWorldObject ());
			}

		}
		
		return compressed;
	}

	public static void ClearCache(){
		foreach (WorldObject o in  WORLDOBJECTS.Values)
			GameObject.Destroy (o.gameObject);
		WORLDOBJECTS.Clear ();
	}

	public static List<Entity> PrepareForBalance(){
		List<Entity> bal = new List<Entity> ();
		foreach (WorldObject w in WORLDOBJECTS.Values) {
			if (w.transform.GetComponent<Entity> () != null && (w.playerID == "" || w.playerID == null)) {
				
				w.transform.GetComponent<Entity> ().active = false;
				bal.Add (w.transform.GetComponent<Entity> ());

			} else if (w.transform.GetComponent<Entity> () != null
			           && w.playerID == SystemInfo.deviceUniqueIdentifier) {

				w.transform.transform.GetComponent<Entity> ().active = true;

			} else if (w.transform.GetComponent<Entity> () != null && (w.playerID != "" && w.playerID != null)) {
				
				w.transform.GetComponent<Entity> ().active = false;

			}
		}

		return bal;
	}

	public static WorldObject GetObject(string key){
		return WORLDOBJECTS [new CharString (key)];
	}

	public static WorldObject GetObjectByIndex(int index){
		return WORLDOBJECTS[WORLDOBJECTS.Keys[index]];
	}

    public static string GetKeyByIndex(int index)
    {
        return WORLDOBJECTS.Keys[index].s;
    }

    public static int GetIndexOfKey(string key){
		return WORLDOBJECTS.Keys.IndexOf(new CharString(key));
	}

	public static bool HasKey(string key){
		return WORLDOBJECTS.ContainsKey (new CharString(key));
	}

	public static int KeyCount(){
		return WORLDOBJECTS.Keys.Count;
	}

}

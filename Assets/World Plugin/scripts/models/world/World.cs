﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public abstract class World{
	public static Transform currentTerrain;
	public static HashSet<World> Instances = new HashSet<World>();

	/// <summary>
	/// Generates a new world.
	/// </summary>
	public static void GenerateWorld(){
		foreach(World instance in Instances)
			instance.OnGenerateWorld();
	}
		
	void Start(){
		Instances.Add (this);
	}

	abstract public void OnGenerateWorld();

	/// <summary>
	/// Saves the world.
	/// </summary>
	public static void SaveWorld(){
		if(WorldDatabase.world != null)
			WorldDatabase.world.Clear ();
		WorldDatabase.world.AddRange(WorldObjectCache.GetCompressed());
		WorldDatabase.PutWorld ();
	}

	public static Quaternion FindRotation(Vector3 from, Vector3 target){
		Vector3 direction = (target - from).normalized;
		Quaternion look = Quaternion.LookRotation(direction);
		return look;
	}

	/// <summary>
	/// Creates the world.
	/// </summary>
	public static void CreateWorld ()
	{
		
		foreach (SerializableWorldObject obj in WorldDatabase.world) {
			WorldObjectCache.Add (obj.id, obj.DecompressObject ().GetComponent<WorldObject> ());
		}

		Client.main.gotWorld = true;
		Client.main.clientBalancer.Balance ();

	}
}

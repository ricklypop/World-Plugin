﻿using System.Collections;
using System.Linq;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ClientParser
{
	#region Values Regarding Updating World Objects

	public Dictionary<int, int> currentRequestedUpdateIndexs { get; set; }
	public Dictionary<int, byte[]> convertedRequestedUpdates { get; set; }

	public int currentUpdateRequestIndex { get; set; }
	public byte[] convertedUpdateRequest { get; set; }

	#endregion

	#region Values Regarding Converting Changes That Need to be Sent

	public int currentChangesIndex { get; set; }
	public byte[] convertedChanges { get; set; }

	#endregion

	#region Values Regarding Converting the Current World 	for Other Clients

	public Dictionary<int, int> currentWorldIndexs{ get; set; }
	public Dictionary<int, byte[]> convertedWorlds{ get; set; }

	#endregion

	protected int parserThreadID { get; set; }


	public ClientParser(){
		currentRequestedUpdateIndexs = new Dictionary<int, int>();
		convertedRequestedUpdates = new Dictionary<int, byte[]>();
		currentWorldIndexs = new Dictionary<int, int> ();

		currentUpdateRequestIndex = 0;
		convertedUpdateRequest = new byte[0];

		currentChangesIndex = 0;
		convertedChanges = new byte[0];

		parserThreadID = MultiThreading.startNewThread (16777216);
	}


	#region Client Parsing

	/// <summary>
	/// Parses the update changes.
	/// Parse the converted update, based on the current index and bit length max
	/// </summary>
	/// <returns>The change updates.</returns>
	public Task ParseRequestUpdate ()
	{
		
		Task task = null;
		task = MultiThreading.doTask (parserThreadID, () => {
			int addAmount = YamlConfig.config.maxBytes;
			byte[] send = new byte[0];


			if (currentUpdateRequestIndex + addAmount < convertedUpdateRequest.Length) {
			
				send = new byte[addAmount];

				Buffer.BlockCopy (convertedUpdateRequest, currentUpdateRequestIndex, send, 0, send.Length);

			} else if (currentUpdateRequestIndex < convertedUpdateRequest.Length) {
			
				addAmount = convertedUpdateRequest.Length - currentUpdateRequestIndex;
				send = new byte[addAmount];

				Buffer.BlockCopy (convertedUpdateRequest, currentUpdateRequestIndex, send, 0, send.Length);

			}


			currentUpdateRequestIndex += YamlConfig.config.maxBytes;

			task.results = send;
		});

		return task;

	}

	/// <summary>
	/// Parses the updates.
	/// Parse the converted populations, based on the current index and bit length max
	/// </summary>
	/// <returns>The updates.</returns>
	public Task ParseUpdates (int index)
	{
		
		Task task = null;
		task = MultiThreading.doTask (parserThreadID, () => {
			int addAmount = YamlConfig.config.maxBytes;
			byte[] send = new byte[0];


			if (currentRequestedUpdateIndexs [index] + addAmount < convertedRequestedUpdates [index].Length) {
			
				send = new byte[addAmount];

				Buffer.BlockCopy (convertedRequestedUpdates [index], currentRequestedUpdateIndexs [index], send, 0, send.Length);

			} else if (currentRequestedUpdateIndexs [index] < convertedRequestedUpdates [index].Length) {
			
				addAmount = convertedRequestedUpdates [index].Length - currentRequestedUpdateIndexs [index];
				send = new byte[addAmount];

				Buffer.BlockCopy (convertedRequestedUpdates [index], currentRequestedUpdateIndexs [index], send, 0, send.Length);

			}


			currentRequestedUpdateIndexs [index] += YamlConfig.config.maxBytes;

			task.results = send;
		});

		return task;

	}

	/// <summary>
	/// Parses the world to send.
	/// Parse the converted world, based on the current index and bit length max
	/// </summary>
	/// <returns>The world to send.</returns>
	public Task ParseWorldSend (int index)
	{
		
		Task task = null;
		task = MultiThreading.doTask (parserThreadID, () => {
			int addAmount = YamlConfig.config.maxBytes;
			byte[] send = new byte[0];


			if (currentWorldIndexs [index] + addAmount < convertedWorlds [index].Length) {
			
				send = new byte[addAmount];

				Buffer.BlockCopy (convertedWorlds [index], currentWorldIndexs [index], send, 0, send.Length);

			} else if (currentWorldIndexs [index] < convertedWorlds [index].Length) {
			
				addAmount = convertedWorlds [index].Length - currentWorldIndexs [index];
				send = new byte[addAmount];

				Buffer.BlockCopy (convertedWorlds [index], currentWorldIndexs [index], send, 0, send.Length);
			}


			currentWorldIndexs [index] += YamlConfig.config.maxBytes;

			task.results = send;
		});

		return task;

	}

	/// <summary>
	/// Parses the changes to send.
	/// Parse the converted changes, based on the current index and bit length max
	/// </summary>
	/// <returns>The changes to send.</returns>
	public Task ParseChangesSend ()
	{
		Task task = null;
		task = MultiThreading.doTask (parserThreadID, () => {
			
			int addAmount = YamlConfig.config.maxBytes;
			byte[] send = new byte[0];


			if (currentChangesIndex + addAmount < convertedChanges.Length) {
			
				send = new byte[addAmount];

				Buffer.BlockCopy (convertedChanges, currentChangesIndex, send, 0, send.Length);

			} else if (currentChangesIndex < convertedChanges.Length) {
			
				addAmount = convertedChanges.Length - currentChangesIndex;
				send = new byte[addAmount];

				Buffer.BlockCopy (convertedChanges, currentChangesIndex, send, 0, send.Length);

			}


			currentChangesIndex += YamlConfig.config.maxBytes;

			task.results = send;

		});

		return task;
	}

	#endregion

	#region Add + Remove + Reset Functions

	public void AddRequestedIndex (int index)
	{
		
		convertedRequestedUpdates.Add (index, new byte[0]);
		currentRequestedUpdateIndexs.Add (index, 0);

	}

	public void ResetWorldParsing ()
	{
		
		currentWorldIndexs = new Dictionary<int, int> ();
		convertedWorlds = new Dictionary<int, byte[]> ();

	}

	public void ResetConvertedChanges ()
	{
		
		convertedChanges = new byte[0];
		currentChangesIndex = 0;

	}
     
	public void RemoveRequestedUpdate (int index)
	{
		
		currentRequestedUpdateIndexs.Remove (index);
		convertedRequestedUpdates.Remove (index);

	}

	public void RemoveConvertedWorld (int index)
	{
		
		currentWorldIndexs.Remove (index);
		convertedWorlds.Remove (index);

	}

	#endregion
}

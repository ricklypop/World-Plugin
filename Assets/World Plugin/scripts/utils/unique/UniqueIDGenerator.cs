 using UnityEngine;
using System.Collections;
using System;

public static class UniqueIDGenerator {
    public const string PREFIX_SEPERATOR = "|";

    /// <summary>
    /// Gets a new unique Id.
    /// Generates a random prefix position.
    /// ex: prefix=<random> <random>|abcd1234...
    /// </summary>
    /// <returns>The unique Id.</returns>
    public static string GetUniqueID(){
        string uniqueID = "";
        int random = UnityEngine.Random.Range(0, 99999);
        uniqueID = string.Format("{0}_{1:N}", uniqueID, Guid.NewGuid());
        uniqueID = random.ToString() + PREFIX_SEPERATOR + uniqueID.Substring(random.ToString().Length - 1);
        return uniqueID;
	}

	/// <summary>
	/// Gets a new unique Id.
	/// Sets prefix position to given value.
    /// ex: prefix = 10 10|abcd1234...
	/// </summary>
	/// <returns>The unique Id.</returns>
	public static string GetUniqueID(int position){
		string uniqueID = "";
		uniqueID = string.Format("{0}_{1:N}", uniqueID, Guid.NewGuid());
		uniqueID = position.ToString() + PREFIX_SEPERATOR + uniqueID.Substring (position.ToString().Length - 1);
		return uniqueID;
	}

    public static int GetPrefixPosition(string key)
    {
        return int.Parse(key.Substring(0, key.IndexOf(PREFIX_SEPERATOR)));
    }
}

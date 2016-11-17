using UnityEngine;
using System.Collections;

public class GuidTest : MonoBehaviour {

	void Update () {

		int random = Random.Range (0, 100);
        string key = UniqueIDGenerator.GetUniqueID();
        Debug.Log ("Random: " + random + " Key: " + key + " Prefix: " + UniqueIDGenerator.GetPrefixPosition(key));

	}

}
 
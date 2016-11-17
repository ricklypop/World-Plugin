using UnityEngine;
using System.Collections;

public class GetInheritComp : MonoBehaviour {
	void Start () {
        GetComponent<WorldObject>().playerID = "nick";
        Debug.Log(GetComponent<WorldObject>().playerID);
        GetComponent<Entity>().playerID = "dan";
        Debug.Log(GetComponent<WorldObject>().playerID);
        Debug.Log(GetComponent<Entity>().playerID);
    }
}

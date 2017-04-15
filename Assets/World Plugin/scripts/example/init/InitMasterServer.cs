using UnityEngine;
using System.Collections;

public class InitMasterServer : MonoBehaviour {

	void Update(){

		if (YamlConfig.config != null) {
			MasterServer.StartServer ();
			Destroy (this);
		}

	}

}

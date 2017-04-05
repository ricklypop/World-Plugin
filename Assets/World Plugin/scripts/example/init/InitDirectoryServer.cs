using UnityEngine;
using System.Collections;

public class InitDirectoryServer : MonoBehaviour {
	
	void Update(){

		if (YamlConfig.config != null) {
			DirectoryServer.StartServer ();
			Destroy (this);
		}

	}

}

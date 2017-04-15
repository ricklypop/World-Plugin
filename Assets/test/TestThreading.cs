using UnityEngine;
using System.Collections;
using System.IO;

public class TestThreading : MonoBehaviour {

	void Start () {

		var file = File.CreateText ("test");

		for (int x = 0; x < 10000; x++) {
			file.WriteLine ("Hello my name is nick fuck you world i need a big file please.");
		}

		file.Close ();

		int threadID = MultiThreading.startNewThread (1000000);

		MultiThreading.doTask (threadID, () => {

			var file1 = File.OpenText("test");

			string line = file1.ReadLine();
			string test = line;

			while(line != null){
				line = file1.ReadLine();
				//test += line;
			}

			Debug.Log("done");

		});

	}

}

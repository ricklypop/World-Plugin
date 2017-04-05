using UnityEngine;
using System.Collections;
using System;

public class WorldDatabaseInit: MonoBehaviour {
	public static WorldDatabaseInit main;

	public Transform display;

	public Transform currentDisplay{ get; set; }
	public bool show { get; set; }
	public bool repeat { get; set; }
	public Action retry{ get; set; }

	private float time;
	private float opactity;

	void Start(){
		main = this;
		WorldDatabase.Start();
	}

	void Update(){
		if (show && currentDisplay == null) {
			currentDisplay = Instantiate (display);
		} else if (show && currentDisplay != null) {
			if (opactity < 1)
				opactity += WorldConstants.OPACADD;
			currentDisplay.GetComponent<CanvasGroup> ().alpha = opactity;
		}else if(!show && currentDisplay != null){
			if (opactity > 0)
				opactity -= WorldConstants.OPACADD;
			currentDisplay.GetComponent<CanvasGroup> ().alpha = opactity;
			if (opactity == 0)
				Destroy (currentDisplay);
		}

		if (repeat && WorldConstants.RETRYTIME <= time)
			Retry ();
		else if(repeat)
			time += Time.deltaTime;
	}

	void Retry(){
		repeat = false;
		retry ();
		time = 0;
	}

	public void SetRetry(Action r){
		retry = r;
		show = true;
		repeat = true;
	}
}

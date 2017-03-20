using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System;
using Priority_Queue;
using System.Reflection;
using System.ComponentModel;

public class MultiThreading : Update
{

	public static bool mainIsRunning;
	public static Dictionary<int, TaskThread> activeThreads = new Dictionary<int, TaskThread> ();
	private static int currentID = 0;

	static MultiThreading ()
	{
		mainIsRunning = true;

		new MultiThreading ();

		Thread abortThread = new Thread (() => {

			try{
				bool running = true;
				while (running) {

					if (!mainIsRunning) {

						stopAll ();
						Thread.CurrentThread.Abort ();
						running = false;
						
					}

					Thread.Sleep (1000);
				}
			}catch(ThreadAbortException e){
				Debug.Log ("ABORTED THREAD ENDED");
			}

		}, 131072);

		abortThread.IsBackground = true;
		abortThread.Start ();

	}

	public MultiThreading () : base()
	{
	}
	
	public override void OnUpdate ()
	{
		mainIsRunning = Application.isPlaying;
		if (Thread.CurrentThread.ThreadState == ThreadState.Aborted)
			mainIsRunning = false;
	}
	
	public override void OnApplicationQuit ()
	{
		mainIsRunning = false;
	}

	public static int startNewThread (int memory)
	{
		if (SystemInfo.processorCount > activeThreads.Count) {
			TaskThread newThread = new TaskThread (currentID, memory);
			newThread.thread.IsBackground = true;
			newThread.Start ();
			activeThreads.Add (currentID, newThread);
			currentID ++;
			return currentID - 1;
		}
		return -1;
	}

	public static bool threadsIdle (int current)
	{
		bool idle = false;
		foreach (TaskThread thread in MultiThreading.activeThreads.Values) {
			if (thread.tasks.Count != 0 && thread != activeThreads [current]) {
				idle = true;
				break;
			}
		}

		return idle;
	}

	public static int loadBalanceTask (Action task)
	{
		SimplePriorityQueue<TaskThread> queue = new SimplePriorityQueue<TaskThread> ();
		foreach (TaskThread thread in activeThreads.Values) {

			queue.Enqueue (thread, thread.tasks.Count);

		}

		TaskThread t = queue.Dequeue ();
		t.tasks.Enqueue (task);
		Debug.Log ("NEW TASK FOR:" + t.id);
		return t.id;
	}

	public static void doTask (int threadID, Action task)
	{
		activeThreads [threadID].tasks.Enqueue (task);
		Debug.Log ("NEW TASK FOR:" + threadID);
	}

	public static void stopThread (int threadID)
	{
		activeThreads [threadID].stop = true;
	}

	public static void stopAll ()
	{

		Debug.Log ("ABORTING ALL THREADS");

		foreach (TaskThread taskThread in activeThreads.Values) {
			
			taskThread.stop = true;
			taskThread.thread.Abort ();
			GC.Collect ();
			
		}

		activeThreads = new Dictionary<int, TaskThread> ();
	}

}

public class TaskThread
{
	public Queue<Action> tasks = new Queue<Action> ();

	public bool stop { get; set; }

	public int id { get; set; }

	public Thread thread{ get; set; }
	
	public TaskThread (int id, int memory)
	{ 
		thread = new Thread (new ThreadStart (this.RunThread), memory); 
		this.id = id; 
	}

	public void Start ()
	{
		thread.Start ();
	}

	public void Join ()
	{
		thread.Join ();
	}

	public bool IsAlive { get { return thread.IsAlive; } }

	public virtual void RunThread ()
	{

		try {

			while (!stop) {
				if (tasks.Count != 0) {
					(tasks.Peek ())();
					tasks.Dequeue ();
					Debug.Log("TASK COMPLETED ON " + id);
				}

			}

		} catch (ThreadAbortException e) {
			Debug.Log (e);
		}

		MultiThreading.activeThreads.Remove(id);

	}
}

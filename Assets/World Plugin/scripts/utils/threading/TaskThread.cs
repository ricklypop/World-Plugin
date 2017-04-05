using System.Collections.Generic;
using System.Threading;
using DisableLogging;


public class TaskThread
{
	public Queue<Task> tasks = new Queue<Task> ();

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
					(tasks.Peek ().action)();
					Task task = tasks.Dequeue ();

					if(task.debugTask){
						Logger.Log("TASK " + task.taskID + " COMPLETED ON " + id, UnityEngine.Color.blue);
					}

					MultiThreading.onCompletion(task);
				}

			}

		} catch (ThreadAbortException e) {
			Logger.Log (e.ToString(), UnityEngine.Color.red);
		}

	}
}
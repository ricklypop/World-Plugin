 using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;


/// An entity is an object that can move and or contains it's own intelligence
/// </summary>
public class Entity : WorldObject {
	#region Public Entity Settings
	public bool active{ get; set; }
	public bool instantTurn{ get; set; }

    private int tempPlayer;
	#endregion

	#region Private Entity Values
	private float moveTime;
	private float rotateTime;
	private float deltaTime;
    #endregion

    #region Final Vars
    protected const string 
        MOVE_VELOCITY = "mv", 
        VELOCITY = "v", 
        DIRECTION = "d", 
        X = "x", 
        Y = "y",
        Z = "z", 
        ROTATION = "r", 
        ROTATION_VELOCITY = "rv",
        ROTATION_X = "rx",
        ROTATION_Y = "ry",
        ROTATION_Z = "rz",
        NAME = "n";
    #endregion

    #region Script Functions
    /// <summary>
    /// Start this instance.
    /// Check if required keys exist in the dictionary.
    /// "tP" stands for temporary player ownership.
    /// "mV" stands for movement velocity.
    /// "rV" stands for rotational velocity.
    /// "r" stands for rotation.
    /// </summary>
    public override void Start(){
        base.Start();

        RegisterMethod(ClientMove);
        RegisterMethod(ClientChangeTempPlayer);
        RegisterMethod(ClientRotate);
        RegisterMethod(ClientStopMove);
        RegisterMethod(ClientStopRotate);

		instantTurn = WorldConstants.DEFAULTINSTANTTURN;

        RegisterVar(MOVE_VELOCITY);
        RegisterVar(VELOCITY);
        RegisterVar(DIRECTION);
        RegisterVar(X);
        RegisterVar(Y);
        RegisterVar(Z);
        RegisterVar(ROTATION);
        RegisterVar(ROTATION_VELOCITY);
        RegisterVar(ROTATION_X);
        RegisterVar(ROTATION_Y);
        RegisterVar(ROTATION_Z);
        RegisterVar(NAME);
    }

    #region Extendable Functions
    public virtual void Act() {}
	#endregion

	/// <summary>
	/// Update this instance.
	/// The entity will run it's seperate intelligence if it is active in its respective local device
	/// or if the temporary player ownership is set to this connection ID.
	/// Moves the entity.
	/// 
	/// </summary>
	public virtual void Update () {

		if (LocalPlayerHasOwnership()) {

			Act ();

		}

		MoveEntity ();
		RotateEntity ();

    }

    public override void PlayerLeft(int id)
    {
        if(tempPlayer == id)
        {

            SetAttribute(VELOCITY, "0"); //Stop Moving
            SetAttribute(ROTATION_VELOCITY, "0"); //Stop Rotate
            tempPlayer = -1;//Reset Ownership

        }
    }

    public void MoveEntity(){

		Vector3 rotation = GetRotation ();

        float moveVelocity = GetAttributeFloat(MOVE_VELOCITY);

		if (moveVelocity != 0) {

			if (instantTurn) {

				transform.eulerAngles = new Vector3 (rotation.x, rotation.y, rotation.z);

			}

			transform.position += (transform.forward / WorldConstants.MOVEMENTFACTOR) * Time.deltaTime * moveVelocity;
			transform.position = new Vector3 (transform.position.x, transform.position.y, WorldConstants.DEFAULTZ);

			moveTime = 0;

		}

	}

	public void RotateEntity(){

		Vector3 rotation = GetRotation ();

		float rotationVelocity = GetAttributeFloat(ROTATION_VELOCITY);
		

		if (rotateTime > rotationVelocity && rotationVelocity != 0) {
			
			transform.eulerAngles = new Vector3(transform.eulerAngles.x + rotation.normalized.x, 
				transform.eulerAngles.y + rotation.normalized.y, transform.eulerAngles.z + rotation.normalized.z);
			rotateTime = 0;

		}

	}

	public Vector3 GetRotation(){
		
		SerializableTransform serializedRotation = GetAttributeTransform(ROTATION);
		Vector3 rotation = Vector3.zero;
		if (serializedRotation != null)
			rotation = serializedRotation.toPosition ();

		return rotation;
	}

	bool LocalPlayerHasOwnership ()
	{
		
		return (active && tempPlayer == -1) || tempPlayer == Client.main.clientBalancer.connectionID;
		
	}

	#endregion

	#region The Local Methods To Send Changes to Other Clients
	/// <summary>
	/// Move the local entity when the local player commands. Then sends this action to all other clients.
	/// </summary>
	/// <param name="newVelocity">New velocity.</param>
	/// <param name="direction">Direction.</param>
	public void LocalMove(float newVelocity, Quaternion direction){
		
		SetAttribute(MOVE_VELOCITY, newVelocity.ToString ());
        SetAttribute(ROTATION, JsonConvert.SerializeObject(new SerializableTransform(direction.eulerAngles)));

		Parameters p = new Parameters();
		p.AddParam (VELOCITY, newVelocity.ToString());
		p.AddParam(ROTATION_X, direction.eulerAngles.x.ToString());
		p.AddParam(ROTATION_Y, direction.eulerAngles.y.ToString());
		p.AddParam(ROTATION_Z, direction.eulerAngles.z.ToString());
		QueueChange (ClientMove, p);

	}


	/// <summary>
	/// Stops the local entity, as commanded by the local player. Then sends this action to all other clients.
	/// </summary>
	public void LocalStopMove(){
		
		SetAttribute(MOVE_VELOCITY, "0");

        Parameters p = new Parameters();
        p.AddParam (X, transform.position.x.ToString());
		p.AddParam(Y, transform.position.y.ToString());
		p.AddParam(Z, transform.eulerAngles.z.ToString());
		p.AddParam(ROTATION_X, transform.eulerAngles.x.ToString());
		p.AddParam(ROTATION_Y, transform.eulerAngles.y.ToString());

		QueueChange (ClientStopMove, p);

	}

	/// <summary>
	/// Rotates the local entity, as commanded by the local player. Then sends to other clients.
	/// </summary>
	/// <param name="newVelocity">New velocity.</param>
	/// <param name="x">The x coordinate.</param>
	/// <param name="y">The y coordinate.</param>
	/// <param name="z">The z coordinate.</param>
	public void LocalRotate(float newVelocity, float x, float y, float z){
		
		SetAttribute(ROTATION_VELOCITY, newVelocity.ToString());
		
		SetAttribute(ROTATION, JsonConvert.SerializeObject(new SerializableTransform(new Vector3 (x, y, z))));

        Parameters p = new Parameters();
        p.AddParam (VELOCITY , newVelocity.ToString());
		p.AddParam(X, x.ToString());
		p.AddParam(Y, y.ToString());
		p.AddParam(Z, z.ToString());

		QueueChange (ClientRotate, p);

	}

	/// <summary>
	/// Stops rotating the local entity, as commanded by the local player. Then sends to other clients.
	/// </summary>
	public void LocalStopRotate(){
		
		SetAttribute(ROTATION_VELOCITY, "0");

        Parameters p = new Parameters();
        p.AddParam(X, transform.eulerAngles.x.ToString());
		p.AddParam(Y, transform.eulerAngles.y.ToString());
		p.AddParam(Z, transform.eulerAngles.z.ToString());

		QueueChange(ClientStopRotate, p);

	}

	/// <summary>
	/// Changes the temporary player ownership of the local object, to the client's ID. Sends to other clients.
	/// </summary>
	public void LocalChangeTempPlayer(){
		
		if (tempPlayer == -1) {

			tempPlayer = Client.main.clientBalancer.connectionID;

            Parameters p = new Parameters();
			p.AddParam (NAME,  Client.main.clientBalancer.connectionID.ToString());

            QueueChange(ClientChangeTempPlayer, p);

		}

	}

	/// <summary>
	/// Removes the temporary player ownership of the local entity. Sends to other clients.
	/// </summary>
	public void LocalRemoveTempPlayer(){
		
		if (tempPlayer ==
			Client.main.clientBalancer.connectionID) {

            tempPlayer = -1;

            Parameters p = new Parameters();
            p.AddParam (NAME, "");

			QueueChange(ClientChangeTempPlayer, p);

		}

	}
	#endregion

	#region The Client Methods that Apply Changes on the Client's World Objects
	/// <summary>
	/// Moves the local entity when commanded from the ObjectCommunicator, as commanded by another client.
	/// </summary>
	/// <param name="par">Parameters.</param>
	public void ClientMove(Parameters p){
		
		float newVelocity = p.GetParamValueAsFloat(VELOCITY);

		float x = p.GetParamValueAsFloat(ROTATION_X);
		float y = p.GetParamValueAsFloat(ROTATION_Y);
		float z = p.GetParamValueAsFloat(ROTATION_Z);

		SetAttribute(ROTATION, JsonConvert.SerializeObject (new SerializableTransform (new Vector3 (x, y, z))));
		
		SetAttribute(VELOCITY, newVelocity.ToString ());
		
	}

	/// <summary>
	/// Stop this entity from moving on this client.
	/// </summary>
	/// <param name="par">Parameters.</param>
	public void ClientStopMove(Parameters p){
		
		float x = p.GetParamValueAsFloat(X);
		float y = p.GetParamValueAsFloat(Y);
		float z = p.GetParamValueAsFloat(Z);
		float rX = p.GetParamValueAsFloat(ROTATION_X);
		float rY = p.GetParamValueAsFloat(ROTATION_Y);

		SetAttribute(VELOCITY, "0");

		transform.position = new Vector3 (x, y, WorldConstants.DEFAULTZ);
		transform.eulerAngles = new Vector3 (rX, rY, z);

	}

	/// <summary>
	/// Rotates the local entity when commanded by the ObjectCommunicator, as commanded by another client.
	/// </summary>
	/// <param name="par">Parameters.</param>
	public void ClientRotate(Parameters p){
		
		float newVelocity = p.GetParamValueAsFloat(VELOCITY);
		float x = p.GetParamValueAsFloat(X);
		float y = p.GetParamValueAsFloat(Y);
		float z = p.GetParamValueAsFloat(DIRECTION);

		SetAttribute(ROTATION_VELOCITY, newVelocity.ToString ());

		SetAttribute(ROTATION, JsonConvert.SerializeObject(new SerializableTransform(new Vector3 (x, y, z))));
		
	}

	/// <summary>
	/// Stops rotating the local entity when commanded by the ObjectCommunicator, as commanded by another client.
	/// </summary>
	/// <param name="par">Parameters.</param>
	public void ClientStopRotate(Parameters p){
		
		float x = p.GetParamValueAsFloat(X);
		float y = p.GetParamValueAsFloat(Y);
		float z = p.GetParamValueAsFloat(Z);

		SetAttribute(ROTATION_VELOCITY, "0");

		transform.eulerAngles = new Vector3 (x, y, z);

	}

	/// <summary>
	/// Changes the local temp player when commanded by the ObjectCommunicator, as commanded by another client.
	/// </summary>
	/// <param name="par">Parameters.</param>
	public void ClientChangeTempPlayer(Parameters p){
		
		string name = p.GetParamValue(NAME);

        tempPlayer = int.Parse(name);

	}
	#endregion
}

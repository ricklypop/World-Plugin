using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

public class WorldObject: MonoBehaviour {
    #region Getters and Setters
    public WorldObjectCache.Types type { get; set; }
    public string id { get; set; }
    public string playerID { get; set; }

    private List<string> varRegister = new List<string>();
    private Dictionary<string, string> attributes = new Dictionary<string, string>();
    private List<Action<Parameters>> methods = new List<Action<Parameters>>();

    protected const string INDEX = "i";
    protected const string CHANGE = "c";

    #endregion

    #region Instantiate

    public virtual void Start()
    {
        RegisterMethod(ClientChangeVars);

        RegisterVar(INDEX);
        RegisterVar(CHANGE);
    }

    public void SetWorldObject(string id, string playerID, 
		WorldObjectCache.Types type, Dictionary<string, string> attributes)
    {
        this.id = id;
        this.playerID = playerID;
        this.type = type;
        this.attributes = attributes;
    }

    #endregion

    #region Internal Functions

    protected void RegisterMethod(Action<Parameters> method)
    {

        methods.Add(method);

    }

    protected string GetAttribute(string key)
    {
        if (attributes.ContainsKey(key))
            return attributes[key];
        else
        {
            return "";
        }

    }

    protected void SetAttribute(string key, string value)
    {
        if (attributes.ContainsKey(key))
            attributes[key] = value;
        else
            attributes.Add(key, value);

    }

    protected float GetAttributeFloat(string key)
    {

        if (attributes.ContainsKey(key) && attributes[key] != "")
        {
            return float.Parse(key);
        }
        else
        {
            return 0f;
        }

    }

    protected SerializableTransform GetAttributeTransform(string key)
    {

        if (attributes.ContainsKey(key) && attributes[key] != "")
        {
            return JsonConvert.DeserializeObject<SerializableTransform>(
                attributes[key]);
        }
        else
        {
            return null;
        }

    }

    public void RegisterVar(string varName)
    {
        varRegister.Add(varName);
    }

    public string GetVarName(int id)
    {
        return varRegister[id];
    }

    #endregion

    #region Change Handler Methods
    /// <summary>
    /// Queues the change.
    /// </summary>
    /// <param name="id">Identifier.</param>
    /// <param name="message">Message.</param>
    /// <param name="args">Arguments.</param>
    public void QueueChange(Action<Parameters> method, Parameters parameters){
        int func = methods.IndexOf(method);

		if( Client.main.clientBalancer.totalPlayers > YamlConfig.config.minTotalPlayers)
			ChangeCache.Enqueue(ObjectCommunicator.CreateMessage(id, func, parameters.Encode(varRegister)));
	}

    public void CallMethod(int func, Dictionary<int, string> args)
    {

        Parameters p = new Parameters();
        p.Decode(args, varRegister);

        methods[func](p);

    }

    #endregion

    #region Local Methods to Send Changes to Other Clients
    /// <summary>
    /// Adds or edits the index with the change.
    /// Sends to other clients.
    /// </summary>
    /// <param name="i">The index.</param>
    /// <param name="c">The change.</param>
    public void LocalChangeVars(string i, string c){
		if (attributes.ContainsKey (i))
			attributes [i] = c;
		else
			attributes.Add (i, c);
        Parameters p = new Parameters();
        p.AddParam(INDEX, i);
        p.AddParam(CHANGE, c);
        QueueChange (ClientChangeVars, p);
	}
	#endregion

	#region The Client Methods that Apply Changes on the Client's World Objects
	/// <summary>
	/// Adds or edits the index with the change.
	/// </summary>
	/// <param name="par">Parameters.</param>
	public void ClientChangeVars(Parameters p){
        string i = p.GetParamValue(INDEX);
		string c = p.GetParamValue(CHANGE);
        if (attributes.ContainsKey (i))
			attributes [i] = c;
		else
			attributes.Add (i, c);
	}
	#endregion

	#region Converion Methods
	/// <summary>
	/// Compresses the world object.
	/// </summary>
	/// <returns>The compressed object.</returns>
	public SerializableWorldObject CompressWorldObject(){
		return new SerializableWorldObject(transform, id, playerID,(int) type, attributes);
	}
    #endregion

    #region Event Methods
    public virtual void PlayerLeft(int id) { }
    #endregion
}

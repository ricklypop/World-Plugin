using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class Chest : WorldObject {
    public Dictionary<string, int> items = new Dictionary<string, int>();

    protected const string ITEM = "item";
    protected const string CHEST = "chest";

    new void Start () {
        base.Start();

        RegisterVar(CHEST);
        RegisterVar(ITEM);

        RegisterMethod(ClientAddItem);
        RegisterMethod(ClientRemoveItem);

        items = GetChest(); 
	}

    void Update()
    {
        //Check if player has opened chest
        //Open/close chest menu
        //Item movement logic
    }

    Dictionary<string, int> GetChest()
    {

        if (GetAttribute(CHEST) != "")
        {
            return JsonConvert.DeserializeObject<Dictionary<string, int>>(GetAttribute(CHEST));
        }
        else
        {
            return new Dictionary<string, int>();
        }

    }

    public void LocalAddItem(string item)
    {

        Parameters param = new Parameters();
        param.AddParam(ITEM, item);

        ClientAddItem(param);

        QueueChange(ClientAddItem, param);

    }

    public void ClientAddItem(Parameters p)
    {
        string item = p.GetParamValue(ITEM);

        if (items.ContainsKey(item))
        {
            items[item] += 1;
        }
        else
        {
            items.Add(item, 1);
        }

        SetAttribute(CHEST, JsonConvert.SerializeObject(items));

    }

    public void LocalRemoveItem(string item)
    {

        Parameters param = new Parameters();
        param.AddParam(ITEM, item);

        ClientAddItem(param);

        QueueChange(ClientRemoveItem, param);

    }

    public void ClientRemoveItem(Parameters p)
    {
        string item = p.GetParamValue(ITEM);

        if (items.ContainsKey(item) && items[item] - 1 != 0)
        {
            items[item] -= 1;
        }else if(items[item] - 1 == 0)
        {
            items.Remove(item);
        }

        SetAttribute(CHEST, JsonConvert.SerializeObject(items));

    }
}

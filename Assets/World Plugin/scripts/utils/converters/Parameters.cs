using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Parameters{
    private Dictionary<string, string> parameters = new Dictionary<string, string>();

    public void AddParam(string param, string value)
    {
        parameters.Add(param, value);
    }

    public string GetParamValue(string param)
    {
        return parameters[param];
    }

    public float GetParamValueAsFloat(string param)
    {
        return float.Parse(parameters[param]);
    }

    public Dictionary<int, string> Encode(List<string> register)
    {
        Dictionary<int, string> encoded = new Dictionary<int, string>();

        foreach(string param in parameters.Keys)
        {
            encoded.Add(register.IndexOf(param), parameters[param]);
        }

        return encoded;
    }

    public void Decode(Dictionary<int, string> encoded, List<string> register)
    {
        parameters = new Dictionary<string, string>();

        foreach (int id in encoded.Keys)
        {
            parameters.Add(register[id], encoded[id]);
        }
    }
}

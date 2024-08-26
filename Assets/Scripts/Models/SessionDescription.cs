using System;
using UnityEngine;


public class SessionDescription : IJsonObject<SessionDescription>
{
    public string SessionType;
    public string Sdp;

    public static SessionDescription FromJson(string jsonString)
    {
        return JsonUtility.FromJson<SessionDescription>(jsonString);
    }

    public string ConvertToJSON()
    {
        return JsonUtility.ToJson(this);
    }
}

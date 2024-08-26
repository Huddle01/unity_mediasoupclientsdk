using System;
using UnityEngine;

public class CandidateInit : IJsonObject<CandidateInit>
{
    public string Candidate;
    public string SdpMid;
    public int SdpMLineIndex;

    public static CandidateInit FromJson(string jsonString) 
    {
        return JsonUtility.FromJson<CandidateInit>(jsonString);
    }

    public string ConvertToJSON()
    {
        return JsonUtility.ToJson(this);
    }
}

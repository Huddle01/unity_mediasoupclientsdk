using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mediasoup.RtpParameter;
using Unity.WebRTC;

public class PlanBUtils
{
    public List<RtpEncodingParameters> GetRtpEncoding(object offerMediaObject, MediaStreamTrack track) 
    {
        return new List<RtpEncodingParameters>();
    }

    public void AddLegacySimulcast(object offerMediaObject, MediaStreamTrack track,int numStreams) 
    {
        
    }
}

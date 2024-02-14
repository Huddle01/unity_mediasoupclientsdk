using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;
using Mediasoup.RtpParameter;
using Mediasoup.SctpParameter;
using Mediasoup.Transports;

public interface IHandlerInterface
{
    void Run(HandlerRunOptions options);
}

public class HandlerInterface : IHandlerInterface
{
    public void Run(HandlerRunOptions options)
    {
        
    }
}

public class HandlerRunOptions 
{
	public string direction; //'send' | 'recv'
	public IceParameters iceParameters;
	public List<RTCIceCandidate> iceCandidates;
	public DtlsParameters dtlsParameters;
	public SctpParameters sctpParameters;
	public List<RTCIceServer> iceServers;
	public RTCIceTransportPolicy iceTransportPolicy;
	public object additionalSettings;
	public object proprietaryConstraints;
	public object extendedRtpCapabilities;
}



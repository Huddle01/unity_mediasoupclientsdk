using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;
using Mediasoup.RtpParameter;
using Mediasoup.SctpParameter;
using Mediasoup.Transports;
using System.Threading.Tasks;
using System;
using UnityAsyncAwaitUtil;
using Utilme.SdpTransform;
using Mediasoup.Ortc;

public class HandlerInterface
{
	private string _name;

	private SctpCapabilities sctpCapabilities;

	RTCSessionDescriptionAsyncOperation offer = null;

	private bool isClosed = false;
	private string direction;
	//private RemoteSdp remoteSdp;
	private Dictionary<string, RtpParameters> _sendingRtpParametersByKind = new Dictionary<string, RtpParameters>();
	private Dictionary<string, RtpParameters> _sendingRemoteRtpParametersByKind = new Dictionary<string, RtpParameters>();

	private DtlsRole _forcedLocalDtlsRole;
	private RTCPeerConnection pc;
	private Dictionary<string, RTCRtpTransceiver> _mapMidTransceiver = new Dictionary<string, RTCRtpTransceiver>();
	private readonly MediaStream sendStream;
	private bool _hasDataChannelMediaSection = false;
	private int _nextSendSctpStreamId = 0;
	private bool _transportReady = false;



	public HandlerInterface(string name) 
	{
		_name = name;
		_ = GetNativeRtpCapabilities();
		sctpCapabilities = new SctpCapabilities();
		sctpCapabilities.numStreams.MIS = 1024;
		sctpCapabilities.numStreams.OS = 1024;

		sendStream = new MediaStream();
	}


	public virtual string GetName() 
	{
		return "";
	}


	public virtual void Close()
	{

	}


	public async Task<RtpCapabilities> GetNativeRtpCapabilities() 
	{
		RTCConfiguration config = new RTCConfiguration
		{
			iceServers = new RTCIceServer[0],
			iceTransportPolicy = RTCIceTransportPolicy.All,
			bundlePolicy = RTCBundlePolicy.BundlePolicyMaxBundle
		};

		pc = new RTCPeerConnection(ref config);

		if (pc == null) { Debug.Log("pc is null"); }

		pc.AddTransceiver(TrackKind.Audio);
		pc.AddTransceiver(TrackKind.Video);

		_ = await CreateOffer(pc);
		Debug.Log(offer.Desc.sdp);

		Sdp sdp = offer.Desc.sdp.ToSdp();

		var nativeRtpCapabilities = CommonUtils.ExtractRtpCapabilities(sdp);

		// libwebrtc supports NACK for OPUS but doesn't announce it.
		OrtcUtils.AddNackSuppportForOpus(nativeRtpCapabilities);

		return nativeRtpCapabilities;
	}

	IEnumerator CreateOffer(RTCPeerConnection peerConnection) 
	{
		offer = peerConnection.CreateOffer();
		yield return offer;
	}

	public virtual SctpCapabilities GetNativeSctpCapabilities()
	{
		return sctpCapabilities;
	}


    public virtual void Run(HandlerRunOptions options)
    {
		AssertNotClosed();
		direction = options.direction;
		/*this._remoteSdp = new RemoteSdp({
			iceParameters,
			iceCandidates,
			dtlsParameters,
			sctpParameters,
		});*/




	}

	public virtual Task UpdateIceServers(List<RTCIceServer> iceServers) 
	{
		return null;
	}

	public virtual Task RestartIce(IceParameters iceParameters) 
	{
		return null;
	}

	public virtual Task<RTCStatsReport> GetTransportStats() 
	{
		return null;
	}

	public virtual Task<HandlerSendResult> Send(HandlerRunOptions options) 
	{
		return null;
	}

	public virtual Task StopSending() 
	{
		return null;
	}

	public virtual Task PauseSending()
	{
		return null;
	}

	public virtual Task ResumeSending()
	{
		return null;
	}

	public virtual Task ReplaceTrack(string localId, MediaStreamTrack track)
	{
		return null;
	}

	public virtual Task SetRtpEncodingParameters(string localId, object param) 
	{
		return null;
	}

	public virtual Task<RTCStatsReport> GetSenderStats()
	{
		return null;
	}

	public virtual Task<List<HandlerReceiveResult>> Receive(List<HandlerReceiveOptions> optionsList) 
	{
		return null;
	}

	public virtual Task<HandlerSendDataChannelResult> SendDataChannel(HandlerSendDataChannelOptions options)
	{
		return null;
	}

	public virtual Task StopReceiving(List<string> localIds) 
	{
		return null;
	}

	public virtual Task PauseReceiving(List<string> localIds)
	{
		return null;
	}

	public virtual Task ResumeReceiving(List<string> localIds)
	{
		return null;
	}

	public virtual Task<RTCStatsReport> GetReceiverStats(List<string> localIds)
	{
		return null;
	}

	public virtual Task<HandlerReceiveDataChannelResult> ReceiveDataChannel(HandlerReceiveDataChannelOptions options)
	{
		return null;
	}

	private async void SetupTransport() 
	{
	
	}

	private void AssertNotClosed() 
	{
	
	}

	private void AssertSendDirection()
	{

	}

	private void AssertRecvDirection()
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

public class HandlerSendResult 
{
	public string localId;
	public RtpParameters rtpParameters;
	public RTCRtpSender rtpSender;
}

public class HandlerReceiveOptions 
{
	public string trackId;
	public string kind;//'audio' | 'video'
	public RtpParameters rtpParameters;
	public string streamId;
}

public class HandlerReceiveResult
{
	public string localId;
	public MediaStreamTrack track;
	public RTCRtpReceiver rtpReceiver;
}

public class HandlerSendDataChannelOptions : SctpStreamParameters 
{

}

public class HandlerSendDataChannelResult 
{
	public RTCDataChannel dataChannel;
	public SctpStreamParameters sctpStreamParameters;
}

public class HandlerReceiveDataChannelResult 
{
	public RTCDataChannel dataChannel;
}

public class HandlerReceiveDataChannelOptions 
{
	public SctpStreamParameters sctpStreamParameters;
	public string label;
	public string protocol;
}
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
using Mediasoup.Internal;

public class HandlerInterface : EnhancedEventEmitter<HandlerEvents>
{
	private string _name;

	private SctpCapabilities sctpCapabilities;

	private bool isClosed = false;
	private string direction;
	private RemoteSdp remoteSdp;
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
		if (isClosed) return;
		isClosed = true;

		if (pc!=null) 
		{
			pc.Close();
		}

		_ = Emit("@close");
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

		try
		{
			pc.AddTransceiver(TrackKind.Audio);
			pc.AddTransceiver(TrackKind.Video);

			RTCSessionDescriptionAsyncOperation offer = await CreateOfferAsync(pc);

			Debug.Log(offer.Desc.sdp);

			try
			{
				pc.Close();
			}
			catch (Exception ex) { }
			
			Sdp sdp = offer.Desc.sdp.ToSdp();

			var nativeRtpCapabilities = CommonUtils.ExtractRtpCapabilities(sdp);

			// libwebrtc supports NACK for OPUS but doesn't announce it.
			OrtcUtils.AddNackSuppportForOpus(nativeRtpCapabilities);

			return nativeRtpCapabilities;
		}
		catch (Exception ex) 
		{
			pc.Close();
			throw new Exception(ex.Message);
		}
		
	}

	public virtual SctpCapabilities GetNativeSctpCapabilities()
	{
		return sctpCapabilities;
	}


    public virtual void Run(HandlerRunOptions options)
    {
		AssertNotClosed();
		direction = options.direction;
		remoteSdp = new RemoteSdp(
			options.iceParameters,
			options.iceCandidates,
			options.dtlsParameters,
			options.sctpParameters,
			null,
			false
		);

		_sendingRtpParametersByKind.Add("audio",Ortc.GetSendingRtpParameters(MediaKind.audio, options.extendedRtpCapabilities));
		_sendingRtpParametersByKind.Add("video", Ortc.GetSendingRtpParameters(MediaKind.video, options.extendedRtpCapabilities));

		_sendingRemoteRtpParametersByKind.Add("audio", Ortc.GetSendingRemoteRtpParameters(MediaKind.audio, options.extendedRtpCapabilities));
		_sendingRemoteRtpParametersByKind.Add("video", Ortc.GetSendingRemoteRtpParameters(MediaKind.video, options.extendedRtpCapabilities));

		if (options.dtlsParameters!=null && options.dtlsParameters.role != DtlsRole.auto) 
		{
			_forcedLocalDtlsRole = options.dtlsParameters.role == DtlsRole.server ? DtlsRole.server : DtlsRole.client;
		}

		RTCConfiguration config = new RTCConfiguration
		{
			iceServers = new RTCIceServer[0],
			iceTransportPolicy = RTCIceTransportPolicy.All,
			bundlePolicy = RTCBundlePolicy.BundlePolicyMaxBundle,
		};

		pc = new RTCPeerConnection(ref config);

		pc.OnIceGatheringStateChange += (state) => 
		{
			_ = Emit("@icegatheringstatechange",state);
		};


		pc.OnConnectionStateChange += (state) => 
		{
			_ = Emit("@connectionstatechange", state);
		};

		pc.OnIceConnectionChange += (state) => 
		{
			switch (state) 
			{
				case RTCIceConnectionState.Checking:
					_ = Emit("@connectionstatechange", "connecting");
					break;

				case RTCIceConnectionState.Connected:
				case RTCIceConnectionState.Completed:
					_ = Emit("@connectionstatechange", "connected");
					break;

				case RTCIceConnectionState.Failed:
					_ = Emit("@connectionstatechange", "failed");
					break;

				case RTCIceConnectionState.Disconnected:
					_ = Emit("@connectionstatechange", "disconnected");
					break;

				case RTCIceConnectionState.Closed:
					_ = Emit("@connectionstatechange", "closed");
					break;
			}
		};




	}

	public virtual void UpdateIceServers(List<RTCIceServer> iceServers) 
	{
		RTCConfiguration config = pc.GetConfiguration();

		config.iceServers = iceServers.ToArray();

		pc.SetConfiguration(ref config);

	}

	public virtual async void RestartIce(IceParameters iceParameters) 
	{
		// Provide the remote SDP handler with new remote ICE parameters.
		if (remoteSdp != null) 
		{
			remoteSdp.UpdateIceParameters(iceParameters);
		}

		if (!_transportReady) return;

		if (direction == "send")
		{
			RTCSessionDescriptionAsyncOperation offer = await CreateOfferIceRestartAsync(pc);

			_ = await SetLocalDescriptionAsync(pc, offer.Desc);
			_ = await SetRemoteDescriptionAsync(pc);

		}
		else 
		{
			RTCSessionDescription offerDesc = new RTCSessionDescription
			{
				type = RTCSdpType.Offer,
				sdp = remoteSdp.GetSdp()
			};

			_ = await SetLocalDescriptionAsync(pc, offerDesc);
			_ = await SetRemoteDescriptionAsync(pc);
		}

	}

	public virtual async Task<RTCStatsReport> GetTransportStats() 
	{
		RTCStatsReportAsyncOperation statsOp = await GetPcStats(pc);
		return statsOp.Value;
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

	IEnumerator<RTCSessionDescriptionAsyncOperation> CreateOfferAsync(RTCPeerConnection peerConnection)
	{
		RTCSessionDescriptionAsyncOperation _offer = peerConnection.CreateOffer();
		yield return _offer;
	}

	IEnumerator<RTCSessionDescriptionAsyncOperation> CreateOfferIceRestartAsync(RTCPeerConnection peerConnection)
	{
		RTCOfferAnswerOptions options = new RTCOfferAnswerOptions
		{
			iceRestart = true
		};
		RTCSessionDescriptionAsyncOperation _offer = peerConnection.CreateOffer(ref options);
		yield return _offer;
	}

	IEnumerator SetLocalDescriptionAsync(RTCPeerConnection peerConnection, RTCSessionDescription offerDesc)
	{
		RTCSessionDescription sessionDescription = new RTCSessionDescription
		{
			type = offerDesc.type,
			sdp = offerDesc.sdp
		};

		RTCSetSessionDescriptionAsyncOperation localDesc = peerConnection.SetLocalDescription();
		yield return localDesc;
	}

	IEnumerator SetRemoteDescriptionAsync(RTCPeerConnection peerConnection)
	{
		RTCSessionDescription sessionDescription = new RTCSessionDescription 
		{
			type = RTCSdpType.Answer,
			sdp = remoteSdp.GetSdp()
		};

		RTCSetSessionDescriptionAsyncOperation localDesc = peerConnection.SetRemoteDescription(ref sessionDescription);
		yield return localDesc;
	}

	IEnumerator<RTCStatsReportAsyncOperation> GetPcStats(RTCPeerConnection peerConnection)
	{
		RTCStatsReportAsyncOperation report = pc.GetStats();
		yield return report;
	}


}

public class HandlerRunOptions 
{
	public string direction; //'send' | 'recv'
	public IceParameters iceParameters;
	public List<IceCandidate> iceCandidates;
	public DtlsParameters dtlsParameters;
	public SctpParameters sctpParameters;
	public List<RTCIceServer> iceServers;
	public RTCIceTransportPolicy iceTransportPolicy;
	public object additionalSettings;
	public object proprietaryConstraints;
	public RtpCapabilities extendedRtpCapabilities;
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

public class HandlerEvents 
{

}
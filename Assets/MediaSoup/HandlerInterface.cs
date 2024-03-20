using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;
using Mediasoup.RtpParameter;
using Mediasoup.SctpParameter;
using Mediasoup.Transports;
using System.Threading.Tasks;
using System;
using Utilme.SdpTransform;
using Mediasoup.Ortc;
using Mediasoup.Internal;
using Mediasoup;
using System.Linq;
using Newtonsoft.Json;
using Cysharp.Threading.Tasks;

public class HandlerInterface : EnhancedEventEmitter<HandlerEvents>
{
    public class SctpNumStreams
    {
        public const int OS = 1024;
        public const int MIS = 1024;
    }

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
        //_ = GetNativeRtpCapabilities();
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

        if (pc != null)
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

        foreach (var item in nativeRtpCapabilities.Codecs)
        {
            //Debug.Log($"codec values {item.MimeType}");
        }

        // libwebrtc supports NACK for OPUS but doesn't announce it.
        OrtcUtils.AddNackSuppportForOpus(nativeRtpCapabilities);

        return nativeRtpCapabilities;


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

        Debug.Log("Remote SDP Run: " + JsonConvert.SerializeObject(options.extendedRtpCapabilities));

        _sendingRtpParametersByKind.Add("audio", ORTC.GetSendingRtpParameters(MediaKind.AUDIO, options.extendedRtpCapabilities));
        _sendingRtpParametersByKind.Add("video", ORTC.GetSendingRtpParameters(MediaKind.VIDEO, options.extendedRtpCapabilities));

        _sendingRemoteRtpParametersByKind.Add("audio", ORTC.GetSendingRemoteRtpParameters(MediaKind.AUDIO, options.extendedRtpCapabilities));
        _sendingRemoteRtpParametersByKind.Add("video", ORTC.GetSendingRemoteRtpParameters(MediaKind.VIDEO, options.extendedRtpCapabilities));

        if (options.dtlsParameters != null && options.dtlsParameters.role != DtlsRole.auto)
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
            _ = Emit("@icegatheringstatechange", state);
        };


        pc.OnConnectionStateChange += (state) =>
        {
            Debug.Log($"connectionstatechange to {state.ToString()}");
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

    public virtual async Task UpdateIceServers(List<RTCIceServer> iceServers)
    {
        RTCConfiguration config = pc.GetConfiguration();

        config.iceServers = iceServers.ToArray();

        pc.SetConfiguration(ref config);

    }

    public virtual async Task RestartIce(IceParameters iceParameters)
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

            RTCSessionDescription sessionDescription = new RTCSessionDescription
            {
                type = RTCSdpType.Answer,
                sdp = remoteSdp.GetSdp()
            };

            var iceRestartOP = await SetRemoteDescriptionAsync(pc, sessionDescription);

            Debug.Log($"Is IceRestart Completed: {iceRestartOP.IsDone}, Error: {(iceRestartOP.IsError ? iceRestartOP.Error : "No Error")}");
            Debug.Log($"PC SignalingState State ${pc.SignalingState}");

        }
        else
        {
            RTCSessionDescription sessionDescription1 = new RTCSessionDescription
            {
                type = RTCSdpType.Offer,
                sdp = remoteSdp.GetSdp()
            };

            _ = await SetRemoteDescriptionAsync(pc, sessionDescription1);

            RTCSessionDescription offerDesc = new RTCSessionDescription
            {
                type = RTCSdpType.Offer,
                sdp = remoteSdp.GetSdp()
            };

            var iceRestartOP = await SetLocalDescriptionAsync(pc, offerDesc);

            Debug.Log($"Is IceRestart Completed: {iceRestartOP.IsDone}, Error: {(iceRestartOP.IsError ? iceRestartOP.Error : "No Error")}");
            Debug.Log($"PC SignalingState State ${pc.SignalingState}");

        }

    }

    public virtual async Task<RTCStatsReport> GetTransportStats()
    {
        RTCStatsReportAsyncOperation statsOp = await GetPcStats(pc);
        return statsOp.Value;
    }

    public virtual async Task<HandlerSendResult> Send(HandlerSendOptions options)
    {
        if (options.encodings != null && options.encodings.Count > 1)
        {
            int idx = 0;
            foreach (var encoding in options.encodings)
            {
                encoding.Rid = idx.ToString();
                idx++;
            }

            // Set rid and verify scalabilityMode in each encoding.
            // NOTE: Even if WebRTC allows different scalabilityMode (different number
            // of temporal layers) per simulcast stream, we need that those are the
            // same in all them, so let's pick up the highest value.
            // NOTE: If scalabilityMode is not given, Chrome will use L1T3.

            int nextRid = 1;
            int maxTemporalLayers = 1;

            foreach (var encoding in options.encodings)
            {
                int temporalLayers = !string.IsNullOrEmpty(encoding.ScalabilityMode) ?
                                        ScalabilityMode.Parse(encoding.ScalabilityMode).TemporalLayers :
                                        3;

                if (temporalLayers > maxTemporalLayers)
                {
                    maxTemporalLayers = temporalLayers;
                }
            }

            foreach (var encoding in options.encodings)
            {
                encoding.Rid = nextRid++.ToString();
                encoding.ScalabilityMode = $"L1T{maxTemporalLayers}";
            }

        }

        RtpParameters sendingRtpParameters = Utils.Clone(_sendingRtpParametersByKind![options.track.Kind.ToString().ToLower()]);
        Debug.Log("Sending RTP Parameters: " + JsonConvert.SerializeObject(sendingRtpParameters));
        sendingRtpParameters.Codecs = ORTC.ReduceCodecs(sendingRtpParameters.Codecs, options.codec);

        RtpParameters sendingRemoteRtpParameters = Utils.Clone(_sendingRemoteRtpParametersByKind![options.track.Kind.ToString().ToLower()]);
        sendingRemoteRtpParameters.Codecs = ORTC.ReduceCodecs(sendingRemoteRtpParameters.Codecs, options.codec);

        Tuple<int, string> mediaSectionIdx = remoteSdp.GetNextMediaSectionIdx();
        Debug.Log($"mediaSectionIdx value {mediaSectionIdx.Item2}");

        RTCRtpTransceiverInit transceiverInit = new RTCRtpTransceiverInit
        {
            streams = new MediaStream[1],
            sendEncodings = options.GetRTCRtpTransceivers(options.encodings),
        };

        transceiverInit.direction = RTCRtpTransceiverDirection.SendOnly;
        transceiverInit.streams[0] = sendStream;

        RTCRtpTransceiver transceiver = pc.AddTransceiver(options.track, transceiverInit);

        RTCSessionDescriptionAsyncOperation offer = await CreateOfferAsync(pc);

        Sdp localSdp = offer.Desc.sdp.ToSdp();

        if (!_transportReady)
        {
            DtlsRole localRole = DtlsRole.client;
            SetupTransport(localRole, localSdp);
        }

        Debug.Log($"SetLocalDescriptionAsync");

        var localDescSetupOp = await SetLocalDescriptionAsync(pc, offer.Desc);

        Debug.Log($"LocalDescSetupOp complete: {localDescSetupOp.IsDone}");

        Debug.Log($"PC State complete: {pc.SignalingState.ToString()}");

        // We can now get the transceiver.mid.
        string localId = transceiver.Mid;

        sendingRtpParameters.Mid = localId;

        localSdp = pc.LocalDescription.sdp.ToSdp();


        Debug.Log($"PC State: {pc.SignalingState.ToString()}");

        MediaDescription offerMediaObject = localSdp.MediaDescriptions[mediaSectionIdx.Item1];

        // Set RTCP CNAME.
        if (sendingRtpParameters.Rtcp != null)
        {
            sendingRtpParameters.Rtcp.CNAME = CommonUtils.GetCName(offerMediaObject);
        }

        Debug.Log($"PC State complete: {pc.SignalingState.ToString()}");

        // Set RTP encodings by parsing the SDP offer if no encodings are given.
        if (options.encodings == null)
        {
            sendingRtpParameters.Encodings = UnifiedPlanUtils.GetRtpEncodingParameters(offerMediaObject);
        }
        // Set RTP encodings by parsing the SDP offer and complete them with given
        // one if just a single encoding has been given. 
        else if (options.encodings.Count == 1)
        {
            List<RtpEncodingParameters> newEncodings = UnifiedPlanUtils.GetRtpEncodingParameters(offerMediaObject);
            options.encodings[0] = newEncodings[0];
            sendingRtpParameters.Encodings = newEncodings;
        }
        else
        {
            sendingRtpParameters.Encodings = options.encodings;
        }

        remoteSdp.Send(offerMediaObject, mediaSectionIdx.Item2, sendingRtpParameters, sendingRemoteRtpParameters, options.codecOptions, true);

        Debug.Log($"PC State complete: {pc.SignalingState.ToString()}");

        RTCSessionDescription sessionDescription = new RTCSessionDescription
        {
            type = RTCSdpType.Answer,
            sdp = remoteSdp.GetSdp()
        };

        Debug.Log("Remote SDP: " + remoteSdp.GetSdp());

        Debug.Log($"Session Description: {sessionDescription.sdp}");
        Debug.Log($"PC State complete: {pc.SignalingState.ToString()}");
        //Debug.Log($"Media FMts: {remoteSdp}");

        var remoteSdbSetupOp = await SetRemoteDescriptionAsync(pc, sessionDescription);

        Debug.Log($"remoteSdbSetupOp complete: {remoteSdbSetupOp.IsDone}, Error: {(remoteSdbSetupOp.IsError ? remoteSdbSetupOp.Error.ToString() : "No Error")}");

        Debug.Log($"PC State complete: {pc.SignalingState.ToString()}");

        // Store in the map.
        _mapMidTransceiver.Add(localId, transceiver);

        HandlerSendResult result = new HandlerSendResult
        {
            localId = localId,
            rtpParameters = sendingRtpParameters,
            rtpSender = transceiver.Sender
        };

        return result;
    }
    public virtual async void StopSending(string localId)
    {
        if (isClosed) return;
        RTCRtpTransceiver transceiver = null;
        bool isTransreceiverExist = _mapMidTransceiver.TryGetValue(localId, out transceiver);
        if (!isTransreceiverExist)
        {
            throw new Exception("associated RTCRtpTransceiver not found");
        }
        transceiver.Sender.ReplaceTrack(null);
        pc.RemoveTrack(transceiver.Sender);
        bool mediaSectionClosed = remoteSdp.CloseMediaSection(transceiver.Mid);
        if (mediaSectionClosed)
        {
            transceiver.Stop();
        }
        RTCSessionDescriptionAsyncOperation offer = await CreateOfferAsync(pc);
        _ = await SetLocalDescriptionAsync(pc, offer.Desc);
        RTCSessionDescription answer = new RTCSessionDescription
        {
            type = RTCSdpType.Answer,
            sdp = remoteSdp.GetSdp()
        };
        _ = await SetRemoteDescriptionAsync(pc, answer);
        _mapMidTransceiver.Remove(localId);
    }
    public virtual async void PauseSending(string localId)
    {
        RTCRtpTransceiver transceiver = null;
        bool isTransreceiverExist = _mapMidTransceiver.TryGetValue(localId, out transceiver);
        if (!isTransreceiverExist)
        {
            throw new Exception("associated RTCRtpTransceiver not found");
        }
        transceiver.Direction = RTCRtpTransceiverDirection.Inactive;
        remoteSdp.PauseMediaSection(localId);
        RTCSessionDescriptionAsyncOperation offer = await CreateOfferAsync(pc);
        _ = await SetLocalDescriptionAsync(pc, offer.Desc);
        RTCSessionDescription answer = new RTCSessionDescription
        {
            type = RTCSdpType.Answer,
            sdp = remoteSdp.GetSdp()
        };
        _ = await SetRemoteDescriptionAsync(pc, answer);
    }
    public virtual async void ResumeSending(string localId)
    {
        RTCRtpTransceiver transceiver = null;
        bool isTransreceiverExist = _mapMidTransceiver.TryGetValue(localId, out transceiver);
        if (!isTransreceiverExist)
        {
            throw new Exception("associated RTCRtpTransceiver not found");
        }
        transceiver.Direction = RTCRtpTransceiverDirection.SendOnly;
        RTCSessionDescriptionAsyncOperation offer = await CreateOfferAsync(pc);
        _ = await SetLocalDescriptionAsync(pc, offer.Desc);
        RTCSessionDescription answer = new RTCSessionDescription
        {
            type = RTCSdpType.Answer,
            sdp = remoteSdp.GetSdp()
        };
        _ = await SetRemoteDescriptionAsync(pc, answer);
    }
    public virtual void ReplaceTrack(string localId, MediaStreamTrack track)
    {
        if (track != null)
        {
            Debug.Log($"ReplcaTrack() [localId {localId}, trackId {track.Id}]");
        }
        else
        {
            Debug.Log($"ReplcaTrack() [localId {localId}, no trackId]");
        }
        RTCRtpTransceiver transceiver = null;
        bool isTransreceiverExist = _mapMidTransceiver.TryGetValue(localId, out transceiver);
        if (!isTransreceiverExist)
        {
            throw new Exception("associated RTCRtpTransceiver not found");
        }
        _ = transceiver.Sender.ReplaceTrack(track);
    }
    public virtual async void SetMaxSpatialLayer(string localId, int spatialLayer)
    {
        RTCRtpTransceiver transceiver = null;
        bool isTransreceiverExist = _mapMidTransceiver.TryGetValue(localId, out transceiver);
        if (!isTransreceiverExist)
        {
            throw new Exception("associated RTCRtpTransceiver not found");
        }
        RTCRtpSendParameters parameters = transceiver.Sender.GetParameters();
        for (int idx = 0; idx < parameters.encodings.Length; idx++)
        {
            var encoding = parameters.encodings[idx];
            if (idx <= spatialLayer)
            {
                encoding.active = true;
            }
            else
            {
                encoding.active = false;
            }
        }
        RTCError error = transceiver.Sender.SetParameters(parameters);
        remoteSdp.MuxMediaSectionSimulcast(localId, parameters.encodings.ToList());
        RTCSessionDescriptionAsyncOperation offer = await CreateOfferAsync(pc);
        _ = await SetLocalDescriptionAsync(pc, offer.Desc);
        RTCSessionDescription answer = new RTCSessionDescription
        {
            type = RTCSdpType.Answer,
            sdp = remoteSdp.GetSdp()
        };
        _ = await SetRemoteDescriptionAsync(pc, answer);
    }
    public virtual async void SetRtpEncodingParameters(string localId, RtpEncodingParameters param)
    {
        RTCRtpTransceiver transceiver = null;
        bool isTransreceiverExist = _mapMidTransceiver.TryGetValue(localId, out transceiver);
        if (!isTransreceiverExist)
        {
            throw new Exception("associated RTCRtpTransceiver not found");
        }
        RTCRtpSendParameters parameters = transceiver.Sender.GetParameters();
        for (int idx = 0; idx < parameters.encodings.Length; idx++)
        {
            parameters.encodings[idx] = new RTCRtpEncodingParameters
            {
                maxBitrate = param.MaxBitrate,
                maxFramerate = param.MaxFramerate,
                rid = param.Rid,
                scaleResolutionDownBy = param.ScaleResolutionDownBy
            };
        }
        transceiver.Sender.SetParameters(parameters);
        remoteSdp.MuxMediaSectionSimulcast(localId, parameters.encodings.ToList());
        RTCSessionDescriptionAsyncOperation offer = await CreateOfferAsync(pc);
        _ = await SetLocalDescriptionAsync(pc, offer.Desc);
        RTCSessionDescription answer = new RTCSessionDescription
        {
            type = RTCSdpType.Answer,
            sdp = remoteSdp.GetSdp()
        };
        _ = await SetRemoteDescriptionAsync(pc, answer);
    }
    public virtual async Task<RTCStatsReport> GetSenderStats(string localId)
    {
        RTCRtpTransceiver transceiver = null;
        bool isTransreceiverExist = _mapMidTransceiver.TryGetValue(localId, out transceiver);
        if (!isTransreceiverExist)
        {
            throw new Exception("associated RTCRtpTransceiver not found");
        }
        RTCStatsReportAsyncOperation statsOp = await GetTransreceiverStats(transceiver);
        return statsOp.Value;
    }
    public virtual async Task<HandlerSendDataChannelResult> SendDataChannel(HandlerSendDataChannelOptions options)
    {
        RTCDataChannelInit channelInit = new RTCDataChannelInit
        {
            negotiated = true,
            id = _nextSendSctpStreamId,
        };
        RTCDataChannel dataChannel = pc.CreateDataChannel(options.label, channelInit);

        // Increase next id.
        _nextSendSctpStreamId = ++_nextSendSctpStreamId % SctpNumStreams.MIS;

        // If this is the first DataChannel we need to create the SDP answer with
        // m=application section.

        if (_hasDataChannelMediaSection)
        {
            RTCSessionDescriptionAsyncOperation offer = await CreateOfferAsync(pc);
            Sdp localSdpObject = offer.Desc.sdp.ToSdp();
            MediaDescription offerMediaObject = localSdpObject.MediaDescriptions.FirstOrDefault(x => x.Media == MediaType.Application);
            if (!_transportReady)
            {
                SetupTransport(DtlsRole.client, localSdpObject);
            }
            _ = await SetLocalDescriptionAsync(pc, offer.Desc);
            remoteSdp.SendSctpAssociation(offerMediaObject);
            RTCSessionDescription answer = new RTCSessionDescription
            {
                type = RTCSdpType.Answer,
                sdp = remoteSdp.GetSdp()
            };
            _ = await SetRemoteDescriptionAsync(pc, answer);
            _hasDataChannelMediaSection = true;
        }
        SctpStreamParameters sctpStreamParameters = new SctpStreamParameters
        {
            streamId = options.streamId,
            ordered = options.ordered,
            maxPacketLifeTime = options.maxPacketLifeTime,
            maxRetransmits = options.maxRetransmits
        };
        return new HandlerSendDataChannelResult
        {
            dataChannel = dataChannel,
            sctpStreamParameters = sctpStreamParameters
        };
    }
    public virtual async Task<List<HandlerReceiveResult>> Receive(List<HandlerReceiveOptions> optionsList)
    {
        List<HandlerReceiveResult> results = new List<HandlerReceiveResult>();
        Dictionary<string, string> mapLocalId = new Dictionary<string, string>();
        foreach (HandlerReceiveOptions options in optionsList)
        {
            string trackId = options.trackId;
            string kind = options.kind;
            RtpParameters rtpParameters = options.rtpParameters;

            Debug.Log("Handler | Receive() | Option.RtpParameters: " + JsonConvert.SerializeObject(rtpParameters));

            string streamId = options.streamId;
            Debug.Log($"receive() [trackId:{trackId}, kind:{kind}]");
            string localId = rtpParameters.Mid ?? _mapMidTransceiver.Count.ToString();
            mapLocalId.Add(trackId, localId);
            MediaKind mediaKind = MediaKind.AUDIO;
            if (kind.Contains("vi"))
            {
                mediaKind = MediaKind.VIDEO;
            }
            else if (kind.Contains("app"))
            {
                mediaKind = MediaKind.APPLICATION;
            }
            remoteSdp.Receive(localId, mediaKind, rtpParameters, streamId ?? rtpParameters.Rtcp.CNAME ?? string.Empty, trackId);
        }

        Debug.Log($"Generating Offer for SDP: {remoteSdp.GetSdp()}");


        RTCSessionDescription offer = new RTCSessionDescription
        {
            type = RTCSdpType.Offer,
            sdp = remoteSdp.GetSdp()
        };

        Debug.Log($"Receive() | Setting Remote Description {remoteSdp.GetSdp()}");

        //Debug.Log($"Receive | SDP Offer: {offer.GetStringValue()}");


        Debug.Log("Peer connection state: " + pc.SignalingState.ToString());

        RTCSetSessionDescriptionAsyncOperation remoteDescOp = await SetRemoteDescriptionAsync(pc, offer);

        Debug.Log("Remote Description Setup Complete?: " + remoteDescOp.IsDone + " Error: "  + remoteDescOp.Error.message);

        Debug.Log("Peer connection state: " + pc.SignalingState.ToString());

        RTCSessionDescriptionAsyncOperation answer = await CreateAnswerAsync(pc);

        Debug.Log("Answer generated: " + answer.Desc.type + " Error: " + answer.Error.message);

        Sdp localSdpObject = answer.Desc.sdp.ToSdp();

        Debug.Log("Answer SDP: " + localSdpObject);

        foreach (HandlerReceiveOptions options in optionsList)
        {
            var trackId = options.trackId;
            var rtpParameters = options.rtpParameters;
            var localId = mapLocalId[trackId];
            MediaDescription answerMediaObject = localSdpObject.MediaDescriptions.FirstOrDefault
            (
            x => x.Attributes.Mid.Id == localId
            );

            // May need to modify codec parameters in the answer based on codec
            // parameters in the offer.

            CommonUtils.ApplyCodecParameters(rtpParameters, answerMediaObject);
        }

        RTCSessionDescription answerDes = new RTCSessionDescription
        {
            type = RTCSdpType.Answer,
            sdp = localSdpObject.ToText()
        };

        if (!_transportReady)
        {
            SetupTransport(DtlsRole.client, localSdpObject);
        }

        _ = await SetLocalDescriptionAsync(pc, answerDes);

        foreach (HandlerReceiveOptions options in optionsList)
        {
            var trackId = options.trackId;
            var localId = mapLocalId[trackId];
            RTCRtpTransceiver transceiver = pc.GetTransceivers().FirstOrDefault(t => t.Mid == localId);
            if (transceiver == null)
            {
                throw new Exception("new RTCRtpTransceiver not found");
            }
            else
            {
                _mapMidTransceiver.Add(localId, transceiver);
                results.Add(new HandlerReceiveResult
                {
                    localId = localId,
                    track = transceiver.Receiver.Track,
                    rtpReceiver = transceiver.Receiver
                });
            }
        }

        Debug.Log("Receive Complete");
        return results;
    }
    public virtual async Task StopReceiving(List<string> localIds)
    {
        if (isClosed) return;
        foreach (string localId in localIds)
        {
            RTCRtpTransceiver transceiver = _mapMidTransceiver[localId];
            if (transceiver == null)
            {
                throw new Exception("associated RTCRtpTransceiver not found");
            }
            remoteSdp.CloseMediaSection(transceiver.Mid);
        }
        RTCSessionDescription offerDesc = new RTCSessionDescription
        {
            type = RTCSdpType.Offer,
            sdp = remoteSdp.GetSdp()
        };
        _ = await SetRemoteDescriptionAsync(pc, offerDesc);
        RTCSessionDescriptionAsyncOperation answer = await CreateAnswerAsync(pc);
        _ = await SetLocalDescriptionAsync(pc, answer.Desc);
        foreach (string localId in localIds)
        {
            _mapMidTransceiver.Remove(localId);
        }
    }
    public virtual async void PauseReceiving(List<string> localIds)
    {
        foreach (string localId in localIds)
        {
            RTCRtpTransceiver transceiver = _mapMidTransceiver[localId];
            if (transceiver == null)
            {
                throw new Exception("associated RTCRtpTransceiver not found");
            }
            transceiver.Direction = RTCRtpTransceiverDirection.Inactive;
            remoteSdp.PauseMediaSection(transceiver.Mid);
        }
        RTCSessionDescription offerDesc = new RTCSessionDescription
        {
            type = RTCSdpType.Offer,
            sdp = remoteSdp.GetSdp()
        };
        _ = await SetRemoteDescriptionAsync(pc, offerDesc);
        RTCSessionDescriptionAsyncOperation answer = await CreateAnswerAsync(pc);
        _ = await SetLocalDescriptionAsync(pc, answer.Desc);
    }
    public virtual async Task ResumeReceiving(List<string> localIds)
    {
        foreach (string localId in localIds)
        {
            RTCRtpTransceiver transceiver = _mapMidTransceiver[localId];
            if (transceiver == null)
            {
                throw new Exception("associated RTCRtpTransceiver not found");
            }
            transceiver.Direction = RTCRtpTransceiverDirection.RecvOnly;
            remoteSdp.ResumeReceivingMediaSection(transceiver.Mid);
        }
        RTCSessionDescription offerDesc = new RTCSessionDescription
        {
            type = RTCSdpType.Offer,
            sdp = remoteSdp.GetSdp()
        };
        _ = await SetRemoteDescriptionAsync(pc, offerDesc);
        RTCSessionDescriptionAsyncOperation answer = await CreateAnswerAsync(pc);
        _ = await SetLocalDescriptionAsync(pc, answer.Desc);
    }
    public virtual async Task<RTCStatsReport> GetReceiverStats(string localId)
    {
        RTCRtpTransceiver transceiver = _mapMidTransceiver[localId];
        if (transceiver == null)
        {
            throw new Exception("associated RTCRtpTransceiver not found");
        }
        RTCStatsReportAsyncOperation statsReport = await GetTransreceiverStats(transceiver);
        return statsReport.Value;
    }
    public virtual async Task<RTCDataChannel> ReceiveDataChannel(HandlerReceiveDataChannelOptions options)
    {
        var sctpStreamParameters = options.sctpStreamParameters;
        var label = options.label;
        var protocol = options.protocol;
        var streamId = sctpStreamParameters.streamId;
        var ordered = sctpStreamParameters.ordered;
        var maxPacketLifeTime = sctpStreamParameters.maxPacketLifeTime;
        var maxRetransmits = sctpStreamParameters.maxRetransmits;
        RTCDataChannelInit channelInit = new RTCDataChannelInit
        {
            negotiated = true,
            id = streamId,
        };
        RTCDataChannel dataChannel = pc.CreateDataChannel(options.label, channelInit);

        // If this is the first DataChannel we need to create the SDP offer with
        // m=application section.
        if (_hasDataChannelMediaSection)
        {
            remoteSdp.ReceiveSctpAssociation(false);
            RTCSessionDescription offerDesc = new RTCSessionDescription
            {
                type = RTCSdpType.Offer,
                sdp = remoteSdp.GetSdp()
            };
            _ = await SetRemoteDescriptionAsync(pc, offerDesc);
            RTCSessionDescriptionAsyncOperation answer = await CreateAnswerAsync(pc);
            if (!_transportReady)
            {
                Sdp localSdpObject = answer.Desc.sdp.ToSdp();
                SetupTransport(DtlsRole.client, localSdpObject);
            }
            _ = await SetLocalDescriptionAsync(pc, answer.Desc);
            _hasDataChannelMediaSection = true;
        }
        return dataChannel;
    }
    private async void SetupTransport(DtlsRole role, Sdp localSdpObject)
    {
        if (localSdpObject == null)
        {
            localSdpObject = pc.CurrentLocalDescription.sdp.ToSdp();
        }
        DtlsParameters dtlsParameters = CommonUtils.ExtractDtlsParameters(localSdpObject);
        dtlsParameters.role = role;
        remoteSdp.UpdateDtlsRole(role);
        Debug.Log("Emitting @connect");
        Action callBack = DefaultCallback;
        Action<Exception> errBack = ErrBack;
        _ = await SafeEmit("@connect", dtlsParameters, callBack, errBack);
        _transportReady = true;
    }

    public void DefaultCallback()
    {
        Debug.Log("Action completed successfully");
    }

    public void ErrBack(Exception exception)
    {
        throw exception;
    }

    private void AssertNotClosed()
    {
        if (isClosed)
        {
            throw new InvalidOperationException("method called in a closed handler");
        }
    }
    private void AssertSendDirection()
    {
        if (direction != "send")
        {
            throw new InvalidOperationException("Method can only be called for handlers with 'send' direction");
        }
    }
    private void AssertRecvDirection()
    {
        if (direction != "recv")
        {
            throw new InvalidOperationException("Method can only be called for handlers with 'recv' direction");
        }
    }
    async UniTask<RTCSessionDescriptionAsyncOperation> CreateOfferAsync(RTCPeerConnection peerConnection)
    {
        RTCSessionDescriptionAsyncOperation offer = peerConnection.CreateOffer();
        var wrapper = new RTCSessionDescriptionAsyncOperationWrapper(offer);
        await wrapper.WaitForCompletionAsync();
        return offer;
    }
    async UniTask<RTCSessionDescriptionAsyncOperation> CreateAnswerAsync(RTCPeerConnection peerConnection)
    {
        RTCSessionDescriptionAsyncOperation answer = peerConnection.CreateAnswer();
        var wrapper = new RTCSessionDescriptionAsyncOperationWrapper(answer);
        await wrapper.WaitForCompletionAsync();
        return answer;
    }
    async UniTask<RTCSessionDescriptionAsyncOperation> CreateOfferIceRestartAsync(RTCPeerConnection peerConnection)
    {

        RTCOfferAnswerOptions options = new RTCOfferAnswerOptions
        {
            iceRestart = true
        };

        RTCSessionDescriptionAsyncOperation _offer = peerConnection.CreateOffer(ref options);
        var wrapper = new RTCSessionDescriptionAsyncOperationWrapper(_offer);
        await wrapper.WaitForCompletionAsync();
        return _offer;
    }

    async UniTask<RTCSetSessionDescriptionAsyncOperation> SetLocalDescriptionAsync(RTCPeerConnection peerConnection, RTCSessionDescription offerDesc)
    {
        RTCSetSessionDescriptionAsyncOperation localDesc = peerConnection.SetLocalDescription(ref offerDesc);
        var wrapper = new RTCSetSessionDescriptionAsyncOperationWrapper(localDesc);
        await wrapper.WaitForCompletionAsync();
        return localDesc;
    }

    async UniTask<RTCSetSessionDescriptionAsyncOperation> SetRemoteDescriptionAsync(RTCPeerConnection peerConnection, RTCSessionDescription sessionDescription)
    {
        RTCSetSessionDescriptionAsyncOperation localDesc = peerConnection.SetRemoteDescription(ref sessionDescription);
        var wrapper = new RTCSetSessionDescriptionAsyncOperationWrapper(localDesc);
        await wrapper.WaitForCompletionAsync();
        return localDesc;
    }

    IEnumerator<RTCStatsReportAsyncOperation> GetPcStats(RTCPeerConnection peerConnection)
    {
        RTCStatsReportAsyncOperation report = pc.GetStats();
        yield return report;
    }

    IEnumerator<RTCStatsReportAsyncOperation> GetTransreceiverStats(RTCRtpTransceiver transreceiver)
    {
        RTCStatsReportAsyncOperation report = transreceiver.Sender.GetStats();
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
    public RTCIceTransportPolicy? iceTransportPolicy;
    public object additionalSettings;
    public object proprietaryConstraints;
    public ExtendedRtpCapabilities extendedRtpCapabilities;
}
public class HandlerSendOptions
{
    public MediaStreamTrack track;
    public List<RtpEncodingParameters> encodings = new List<RtpEncodingParameters>();
    public ProducerCodecOptions codecOptions = new ProducerCodecOptions();
    public RtpCodecCapability codec = new RtpCodecCapability();
    public RTCRtpEncodingParameters[] GetRTCRtpTransceivers(List<RtpEncodingParameters> rtps)
    {
        List<RTCRtpEncodingParameters> rtpEncoding = new List<RTCRtpEncodingParameters>();
        foreach (RtpEncodingParameters rtp in rtps)
        {
            RTCRtpEncodingParameters temp = new RTCRtpEncodingParameters();
            if (rtp.MaxBitrate.HasValue)
                temp.maxBitrate = (ulong)rtp.MaxBitrate.Value;
            if (rtp.MaxFramerate.HasValue)
                temp.maxFramerate = (uint)rtp.MaxFramerate.Value;
            temp.rid = rtp.Rid;
            if (rtp.ScaleResolutionDownBy.HasValue)
                temp.scaleResolutionDownBy = rtp.ScaleResolutionDownBy;
            rtpEncoding.Add(temp);
        }
        return rtpEncoding.ToArray();
    }
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

public class RTCSetSessionDescriptionAsyncOperationWrapper
{
    private readonly RTCSetSessionDescriptionAsyncOperation operation;
    private readonly TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

    public bool IsDone { get; private set; }

    public RTCSetSessionDescriptionAsyncOperationWrapper(RTCSetSessionDescriptionAsyncOperation operation)
    {
        this.operation = operation;
        Task.Run(() => CheckCompletion());
    }

    private async Task CheckCompletion()
    {
        while (!operation.IsDone)
        {
            await Task.Delay(100); // Adjust delay as necessary
        }

        IsDone = true;
        tcs.SetResult(true);
    }

    public Task WaitForCompletionAsync()
    {
        return tcs.Task;
    }
}

public class RTCSessionDescriptionAsyncOperationWrapper
{
    private readonly RTCSessionDescriptionAsyncOperation operation;
    private readonly TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

    public bool IsDone { get; private set; }

    public RTCSessionDescriptionAsyncOperationWrapper(RTCSessionDescriptionAsyncOperation operation)
    {
        this.operation = operation;
        Task.Run(() => CheckCompletion());
    }

    private async Task CheckCompletion()
    {
        while (!operation.IsDone)
        {
            await Task.Delay(100); // Adjust delay as necessary
        }

        IsDone = true;
        tcs.SetResult(true);
    }

    public Task WaitForCompletionAsync()
    {
        return tcs.Task;
    }
}

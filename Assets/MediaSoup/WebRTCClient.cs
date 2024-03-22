using UnityEngine;
using System;
using WebSocketSharp;
using Mediasoup.Ortc;
using Mediasoup.RtpParameter;
using Mediasoup.Transports;
using Mediasoup;
using Newtonsoft.Json;
using Mediasoup.SctpParameter;
using System.Collections.Generic;
using System.Threading.Tasks;
using Utilme.SdpTransform;
using Cysharp.Threading.Tasks;
using Mediasoup.Internal;

namespace Unity.WebRTC.Huddle01.SDK
{

    public class WebRTCClient : EnhancedEventEmitter
    {

        public class SctpNumStreams
        {
            public const int OS = 1024;
            public const int MIS = 1024;
        }

        private string _name;
        private string direction;
        private bool isClosed = false;
        private bool _hasDataChannelMediaSection = false;
        private bool _transportReady = false;
        private int _nextSendSctpStreamId = 0;

        private RemoteSdp remoteSdp;
        private DtlsRole _forcedLocalDtlsRole;
        private SctpCapabilities sctpCapabilities;

        private Dictionary<string, RtpParameters> _sendingRtpParametersByKind = new Dictionary<string, RtpParameters>();
        private Dictionary<string, RtpParameters> _sendingRemoteRtpParametersByKind = new Dictionary<string, RtpParameters>();
        private Dictionary<string, RTCRtpTransceiver> _mapMidTransceiver = new Dictionary<string, RTCRtpTransceiver>();

        private readonly MediaStream sendStream;


        static bool makingOffer = false;
        bool ready = false;
        private RTCPeerConnection localPC;
        RTCSessionDescription remoteSDP, localSDP;
        
        public delegate void SendLocalDescriptionDelegate(RTCSessionDescription rTCSessionDescription);
        private SendLocalDescriptionDelegate _sendLocalDescriptionDelegate;

        public delegate void SendCandidateMessageDelegate(string candidate);
        private SendCandidateMessageDelegate _sendCandidateMessageDelegate;


        public WebRTCClient(SendLocalDescriptionDelegate sendLocalDescriptionDelegate, SendCandidateMessageDelegate sendCandidateMessageDelegate)
        {
            _sendLocalDescriptionDelegate = sendLocalDescriptionDelegate;
            _sendCandidateMessageDelegate = sendCandidateMessageDelegate;
        }

        public WebRTCClient()
        {
            _sendLocalDescriptionDelegate = null;
            _sendCandidateMessageDelegate = null;
        }

        /********************************************************************************************************************/

        public void setDelegateOnTrack(DelegateOnTrack onTrackCallBack)
        {
            localPC.OnTrack = onTrackCallBack;
        }

        private void CreatePeerConnection()
        {
            var configuration = GetSelectedSdpSemantics();

            localPC = new RTCPeerConnection(ref configuration);

            localPC.OnIceCandidate += (RTCIceCandidate candidate) =>
            {
                Debug.Log("ICE Candidate Created: " + candidate.Candidate);
                //SendCandidateMessage(candidate.Candidate);
            };

            localPC.OnIceConnectionChange = state =>
            {
                switch (state)
                {
                    case RTCIceConnectionState.Connected:
                    case RTCIceConnectionState.New:
                    case RTCIceConnectionState.Checking:
                    case RTCIceConnectionState.Closed:
                    case RTCIceConnectionState.Completed:
                    case RTCIceConnectionState.Disconnected:
                    case RTCIceConnectionState.Failed:
                    case RTCIceConnectionState.Max:
                        Debug.Log("IceConnectionState: " + state);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(state), state, null);
                };
            };

             //Negotiation Needed is only fired in Stable state
             //Stable means either the PC is new or negotiation is completed.
            localPC.OnNegotiationNeeded = async () =>
            {
                Debug.Log("Negotitation Need at PC");
                try
                {
                    //makingOffer = true;

                    // Creats a fresh-offer as no negotiation is underway
                    //await localPC.SetLocalDescription();

                    //localSDP = localPC.LocalDescription;

                    //SendLocalDescriptionMessage();
                }
                catch (Exception ex)
                {
                    Debug.Log("Error on setting and sending local description: " + ex.Message);
                }
                finally
                {
                    makingOffer = false;
                }
            };

        }

        private static RTCConfiguration GetSelectedSdpSemantics()
        {
            RTCConfiguration config = default;
            config.iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } };
            return config;
        }

        /****************************************************************************************************************/

        private void SendLocalDescriptionMessage() {
            _sendLocalDescriptionDelegate?.Invoke(localSDP);
        }

        private void SendCandidateMessage(string candidate)
        {
            _sendCandidateMessageDelegate?.Invoke(candidate);
        }

        /*****************************************************************************************************************/

        public virtual void Run(HandlerRunOptions options)
        {
            remoteSdp = new RemoteSdp(
                options.iceParameters,
                options.iceCandidates,
                options.dtlsParameters,
                options.sctpParameters,
                null,
                false
            );
            direction = options.direction;

            Debug.Log("Remote SDP Run: " + JsonConvert.SerializeObject(options.extendedRtpCapabilities));

            _sendingRtpParametersByKind.Add("audio", ORTC.GetSendingRtpParameters(MediaKind.AUDIO, options.extendedRtpCapabilities));
            _sendingRtpParametersByKind.Add("video", ORTC.GetSendingRtpParameters(MediaKind.VIDEO, options.extendedRtpCapabilities));

            _sendingRemoteRtpParametersByKind.Add("audio", ORTC.GetSendingRemoteRtpParameters(MediaKind.AUDIO, options.extendedRtpCapabilities));
            _sendingRemoteRtpParametersByKind.Add("video", ORTC.GetSendingRemoteRtpParameters(MediaKind.VIDEO, options.extendedRtpCapabilities));

            if (options.dtlsParameters != null && options.dtlsParameters.role != DtlsRole.auto)
            {
                _forcedLocalDtlsRole = options.dtlsParameters.role == DtlsRole.server ? DtlsRole.server : DtlsRole.client;
            }

            CreatePeerConnection();
        }

        public class WebRTCClientSendOptions : HandlerSendOptions {
            public string remoteSDP;
        }

        public virtual async Task<HandlerSendResult> Send(WebRTCClientSendOptions options)
        {
            if (options.encodings != null && options.encodings.Count > 1)
            {

                // Set rid and verify scalabilityMode in each encoding.
                // NOTE: Even if WebRTC allows different scalabilityMode (different number
                // of temporal layers) per simulcast stream, we need that those are the
                // same in all them, so let's pick up the highest value.
                // NOTE: If scalabilityMode is not given, Chrome will use L1T3.

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

                int idx = 0;
                foreach (var encoding in options.encodings)
                {
                    encoding.Rid = $"r{idx}";
                    encoding.ScalabilityMode = $"L1T{maxTemporalLayers}";
                    idx++;
                }

            }

            RtpParameters sendingRtpParameters = Utils.Clone(_sendingRtpParametersByKind![options.track.Kind.ToString().ToLower()]);
            Debug.Log("Sending RTP Parameters: " + JsonConvert.SerializeObject(sendingRtpParameters));
            sendingRtpParameters.Codecs = ORTC.ReduceCodecs(sendingRtpParameters.Codecs, options.codec);

            RtpParameters sendingRemoteRtpParameters = Utils.Clone(_sendingRemoteRtpParametersByKind![options.track.Kind.ToString().ToLower()]);
            sendingRemoteRtpParameters.Codecs = ORTC.ReduceCodecs(sendingRemoteRtpParameters.Codecs, options.codec);
            Debug.Log("Sending Remote RTP Parameters: " + JsonConvert.SerializeObject(sendingRemoteRtpParameters));

            Tuple<int, string> mediaSectionIdx = remoteSdp.GetNextMediaSectionIdx();
            //Debug.Log($"mediaSectionIdx value {mediaSectionIdx.Item2}");

            RTCRtpTransceiverInit transceiverInit = new RTCRtpTransceiverInit
            {
                streams = new MediaStream[1],
                sendEncodings = options.GetRTCRtpTransceivers(options.encodings),
            };

            transceiverInit.direction = RTCRtpTransceiverDirection.SendOnly;
            transceiverInit.streams[0] = sendStream;

            RTCRtpTransceiver transceiver = localPC.AddTransceiver(options.track, transceiverInit);

            RTCSessionDescriptionAsyncOperation offer = await CreateOfferAsync(localPC);

            Sdp localSdp = offer.Desc.sdp.ToSdp();

            Debug.Log("Local SDP: " + offer.Desc.sdp);

            if (!_transportReady)
            {
                DtlsRole localRole = DtlsRole.client;
                SetupTransport(localRole, localSdp);
            }

            Debug.Log($"SetLocalDescriptionAsync");

            var localDescSetupOp = await SetLocalDescriptionAsync(localPC, offer.Desc);

            Debug.Log($"LocalDescSetupOp complete: {localDescSetupOp.IsDone}");

            Debug.Log($"PC State complete: {localPC.SignalingState.ToString()}");

            // We can now get the transceiver.mid.
            string localId = transceiver.Mid;

            sendingRtpParameters.Mid = localId;

            localSdp = localPC.LocalDescription.sdp.ToSdp();

            Debug.Log($"PC State: {localPC.SignalingState.ToString()}");

            MediaDescription offerMediaObject = localSdp.MediaDescriptions[mediaSectionIdx.Item1];

            // Set RTCP CNAME.
            if (sendingRtpParameters.Rtcp != null)
            {
                sendingRtpParameters.Rtcp.CNAME = CommonUtils.GetCName(offerMediaObject);
            }

            Debug.Log($"PC State complete: {localPC.SignalingState.ToString()}");

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
                Debug.Log("Otherwise Encoding");
                sendingRtpParameters.Encodings = options.encodings;
                Debug.Log("New Encodings: " + JsonConvert.SerializeObject(sendingRtpParameters.Encodings));
            }

            Debug.Log($"PC State complete: {localPC.SignalingState.ToString()}");

            RTCSessionDescription sessionDescription = new RTCSessionDescription
            {
                type = RTCSdpType.Answer,
                sdp = options.remoteSDP
            };

            Debug.Log("Remote SDP: " + options.remoteSDP);

            Debug.Log($"Session Description: {sessionDescription.sdp}");
            Debug.Log($"PC State complete: {localPC.SignalingState}");
            //Debug.Log($"Media FMts: {remoteSdp}");

            var remoteSdbSetupOp = await SetRemoteDescriptionAsync(localPC, sessionDescription);

            Debug.Log($"remoteSdbSetupOp complete: {remoteSdbSetupOp.IsDone}, Error: {remoteSdbSetupOp.Error.message}");

            Debug.Log($"PC State complete: {localPC.SignalingState}");

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

        private async void SetupTransport(DtlsRole role, Sdp localSdpObject)
        {
            if (localSdpObject == null)
            {
                localSdpObject = localPC.CurrentLocalDescription.sdp.ToSdp();
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
            //Debug.Log("Action completed successfully");
        }

        public void ErrBack(Exception exception)
        {
            throw exception;
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
            RTCSetSessionDescriptionAsyncOperation remoteDesc = peerConnection.SetRemoteDescription(ref sessionDescription);
            var wrapper = new RTCSetSessionDescriptionAsyncOperationWrapper(remoteDesc);
            await wrapper.WaitForCompletionAsync();
            return remoteDesc;
        }

        IEnumerator<RTCStatsReportAsyncOperation> GetPcStats(RTCPeerConnection peerConnection)
        {
            RTCStatsReportAsyncOperation report = peerConnection.GetStats();
            yield return report;
        }

        IEnumerator<RTCStatsReportAsyncOperation> GetTransreceiverStats(RTCRtpTransceiver transreceiver)
        {
            RTCStatsReportAsyncOperation report = transreceiver.Sender.GetStats();
            yield return report;
        }

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


}

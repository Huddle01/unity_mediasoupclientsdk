using System.Collections;
using System.Collections.Generic;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

public class SimpleMediaStreamReceiver : MonoBehaviour
{
    [SerializeField]
    private RawImage _receiveImage;

    private RTCPeerConnection _connection;

    private WebSocket _webSocket;
    private string _clientId;

    private bool _hasReceivedOffer = false;
    private SessionDescription _receiveOfferSessionDescTemp;

    private string _senderIp;
    private string _senderPort;



    // Start is called before the first frame update
    void Start()
    {
        InitClient("192.168.1.5", 8080);
    }

    private void OnDestroy()
    {
        _connection.Close();
    }

    public void InitClient(string serverIp, int serverPort)
    {
        int port = serverPort == 0 ? 8080 : serverPort;
        _clientId = gameObject.name;

        _webSocket = new WebSocket($"ws://{serverIp}:{port}/{nameof(SimpleDataChannelService)}");
        _webSocket.OnMessage += (sender, e) =>
        {
            SIgnalingMessage signalingMessage = new SIgnalingMessage(e.Data);

            switch (signalingMessage.Type)
            {
                case SignalingMessageType.OFFER:
                    Debug.Log($"{_clientId} - Got OFFER from Maximus: {signalingMessage.Message}");
                    _receiveOfferSessionDescTemp = SessionDescription.FromJson(signalingMessage.Message);
                    _hasReceivedOffer = true;
                    break;

                case SignalingMessageType.CANDIDATE:
                    Debug.Log($"{_clientId} - Got CANDIDATE from Maximus: {signalingMessage.Message}");

                    CandidateInit candidateInit = CandidateInit.FromJson(signalingMessage.Message);
                    RTCIceCandidateInit init = new RTCIceCandidateInit();
                    init.sdpMid = candidateInit.SdpMid;
                    init.sdpMLineIndex = candidateInit.SdpMLineIndex;
                    init.candidate = candidateInit.Candidate;

                    RTCIceCandidate candidate = new RTCIceCandidate(init);

                    _connection.AddIceCandidate(candidate);
                    break;

                default:
                    Debug.Log($"{_clientId} - Maximus says : {e.Data}");
                    break;

            }
        };

        _webSocket.Connect();

        _connection = new RTCPeerConnection();
        _connection.OnIceCandidate = candidate =>
        {
            var candidateInit = new CandidateInit()
            {
                SdpMid = candidate.SdpMid,
                SdpMLineIndex = candidate.SdpMLineIndex ?? 0,
                Candidate = candidate.Candidate
            };

            _webSocket.Send($"CANDIDATE!" + candidateInit.ConvertToJSON());
        };

        _connection.OnIceConnectionChange = state =>
        {
            Debug.Log(state);
        };

        _connection.OnTrack = e =>
        {
            if (e.Track is VideoStreamTrack video) 
            {
                video.OnVideoReceived += tex =>
                 {
                     _receiveImage.texture = tex;
                 };
            }
        };

        StartCoroutine(WebRTC.Update());

    }


    // Update is called once per frame
    void Update()
    {
        if (_hasReceivedOffer)
        {
            _hasReceivedOffer = !_hasReceivedOffer;
            StartCoroutine(CreateAnswer());
        }
    }

    public IEnumerator CreateAnswer()
    {
        RTCSessionDescription offerSessionDesc = new RTCSessionDescription();
        offerSessionDesc.type = RTCSdpType.Offer;
        offerSessionDesc.sdp = _receiveOfferSessionDescTemp.Sdp;

        RTCSetSessionDescriptionAsyncOperation remoteDescOp = _connection.SetRemoteDescription(ref offerSessionDesc);
        yield return remoteDescOp;

        RTCSessionDescriptionAsyncOperation answer = _connection.CreateAnswer();
        yield return answer;

        RTCSessionDescription answerDesc = answer.Desc;
        RTCSetSessionDescriptionAsyncOperation localDescOp = _connection.SetLocalDescription(ref answerDesc);
        yield return localDescOp;

        SessionDescription answerSessionDesc = new SessionDescription()
        {
            SessionType = answerDesc.type.ToString(),
            Sdp = answerDesc.sdp
        };

        _webSocket.Send("ANSWER!" + answerSessionDesc.ConvertToJSON());
    }


}

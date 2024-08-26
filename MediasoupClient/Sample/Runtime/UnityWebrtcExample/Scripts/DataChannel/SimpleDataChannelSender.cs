using System;
using System.Collections;
using System.Collections.Generic;
using Unity.WebRTC;
using UnityEngine;
using WebSocketSharp;

public class SimpleDataChannelSender : MonoBehaviour
{
    [SerializeField]
    private bool _sendMessageViaChannel = false;

    private RTCPeerConnection _connection;
    private RTCDataChannel _dataChannel;

    private WebSocket _webSocket;
    private string _clientId;

    private bool _hasAnswerReceived = false;
    private SessionDescription _receiveAnswerSessionDescTemp;


    // Start is called before the first frame update
    void Start()
    {
        InitClient("192.168.1.5", 8080);
    }

    // Update is called once per frame
    void Update()
    {
        if (_hasAnswerReceived) 
        {
            _hasAnswerReceived = !_hasAnswerReceived;
            StartCoroutine(SetRemoteDesc());
        }

        if (_sendMessageViaChannel) 
        {
            _sendMessageViaChannel = !_sendMessageViaChannel;
            _dataChannel.Send("TEST! TEST TEST");
        }
    }

    private void OnDestroy()
    {
        _dataChannel.Close();
        _connection.Close();
    }

    public void InitClient(string serverIp, int serverPort)
    {
        int port = serverPort == 0 ? 8080 : serverPort;
        _clientId = gameObject.name;

        _webSocket = new WebSocket($"ws://{serverIp}:{port}/{nameof(SimpleDataChannelService)}");

        _webSocket.OnMessage += (sender, e)=>
        {
            var requestArray = e.Data.Split("!");
            var requestType = requestArray[0];
            var requestData = requestArray[1];

            switch (requestType)
            {
                case "ANSWER":
                    Debug.Log($"{_clientId} - Got ANSWER from Maximus: {requestData}");
                    _receiveAnswerSessionDescTemp = SessionDescription.FromJson(requestData);
                    _hasAnswerReceived = true;
                    break;

                case "CANDIDATE":
                    Debug.Log($"{_clientId} - Got CANDIDATE from Maximus: {requestData}");

                    CandidateInit candidateInit = CandidateInit.FromJson(requestData);
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

        _dataChannel = _connection.CreateDataChannel("sendChannel");

        _dataChannel.OnOpen = () => 
        {
            Debug.Log("Senders open channel");
        };

        _dataChannel.OnClose = () =>
        {
            Debug.Log("Senders closed channel");
        };

        _connection.OnNegotiationNeeded = () => 
        {
            StartCoroutine(CreateOffer());
        };

    }

    private IEnumerator CreateOffer() 
    {
        RTCSessionDescriptionAsyncOperation offer = _connection.CreateOffer();
        yield return offer;

        RTCSessionDescription offerDesc = offer.Desc;
        RTCSetSessionDescriptionAsyncOperation localDescOp = _connection.SetLocalDescription(ref offerDesc);
        yield return localDescOp;

        SessionDescription OfferSessionDesc = new SessionDescription() 
        {
            SessionType = offerDesc.type.ToString(),
            Sdp = offerDesc.sdp
        };

        _webSocket.Send("OFFER!" + OfferSessionDesc.ConvertToJSON());
    }

    private IEnumerator SetRemoteDesc() 
    {
        RTCSessionDescription answerSessionDesc = new RTCSessionDescription();
        answerSessionDesc.type = RTCSdpType.Answer;
        answerSessionDesc.sdp = _receiveAnswerSessionDescTemp.Sdp;

        RTCSetSessionDescriptionAsyncOperation remoteDescOp = _connection.SetRemoteDescription(ref answerSessionDesc);
        yield return remoteDescOp;

    }

}

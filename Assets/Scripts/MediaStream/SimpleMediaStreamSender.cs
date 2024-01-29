using System.Collections;
using System.Collections.Generic;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

public class SimpleMediaStreamSender : MonoBehaviour
{
    [SerializeField]
    private Camera _cameraStream;
    [SerializeField]
    private RawImage _sourceImage;

    private RTCPeerConnection _connection;
    private MediaStream _videoStream;
    private VideoStreamTrack _videoStreamTrack;

    private WebSocket _webSocket;
    private string _clientId;

    private bool _hasAnswerReceived;
    private SessionDescription _receiveAnswerSessionDescTemp;

    private WebCamTexture _webCamTexture;
    private Texture _webCamStreamingTexture;
    [SerializeField]
    private Toggle _isWebCamOn;

    // Start is called before the first frame update
    void Start()
    {
        InitClient("192.168.1.5", 8080);
    }

    private void OnDestroy()
    {
        _videoStreamTrack.Stop();
        _connection.Close();

        if (_webCamTexture != null)
        {
            _webCamTexture.Stop();
            _webCamTexture = null;
        }

        _webSocket.Close();
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
                case SignalingMessageType.ANSWER:
                    Debug.Log($"{_clientId} - Got ANSWER from Maximus: {signalingMessage.Message}");
                    _receiveAnswerSessionDescTemp = SessionDescription.FromJson(signalingMessage.Message);
                    _hasAnswerReceived = true;
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

        _connection.OnNegotiationNeeded = () =>
        {
            StartCoroutine(CreateOffer());
        };

        if (_isWebCamOn.isOn)
        {
            StartCoroutine(CaptureWebCamVideo());
        }
        else 
        {
            _videoStreamTrack = _cameraStream.CaptureStreamTrack(1280, 720);
            _sourceImage.texture = _cameraStream.targetTexture;
            _connection.AddTrack(_videoStreamTrack);
        }
        

        StartCoroutine(WebRTC.Update());

    }


    // Update is called once per frame
    void Update()
    {
        if (_hasAnswerReceived)
        {
            _hasAnswerReceived = !_hasAnswerReceived;
            StartCoroutine(SetRemoteDesc());
        }
    }

    private IEnumerator CaptureWebCamVideo() 
    {
        WebCamDevice userCameraDevice = WebCamTexture.devices[0];
        _webCamTexture = new WebCamTexture(userCameraDevice.name, 1280, 720, 30);
        _webCamTexture.Play();
        
        yield return new WaitUntil(() => _webCamTexture.didUpdateThisFrame);

        //_webCamStreamingTexture = new Texture2D(1280, 720, GraphicsFormat.B8G8R8A8_UNorm, TextureCreationFlags.None);

        _videoStreamTrack = new VideoStreamTrack(_webCamStreamingTexture);
        _sourceImage.texture = _webCamStreamingTexture;
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

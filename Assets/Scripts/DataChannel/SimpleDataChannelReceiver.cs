using System.Collections;
using Unity.WebRTC;
using UnityEngine;
using WebSocketSharp;

public class SimpleDataChannelReceiver : MonoBehaviour
{
    private RTCPeerConnection _connection;
    private RTCDataChannel _dataChannel;

    private WebSocket _webSocket;
    private string _clientId;

    private bool _hasReceivedOffer = false;
    private SessionDescription _receiveOfferSessionDescTemp;

    // Start is called before the first frame update
    void Start()
    {
        InitClient("192.168.1.5", 8080);
    }

    private void Update()
    {
        if (_hasReceivedOffer) 
        {
            _hasReceivedOffer = !_hasReceivedOffer;
            StartCoroutine(CreateAnswer());
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
        _webSocket.OnMessage += (sender, e) => 
        {
            var requestArray = e.Data.Split("!");
            var requestType = requestArray[0];
            var requestData = requestArray[1];

            switch (requestType) 
            {
                case "OFFER" :
                    Debug.Log($"{_clientId} - Got OFFER from Maximus: {requestData}");
                    _receiveOfferSessionDescTemp = SessionDescription.FromJson(requestData);
                    _hasReceivedOffer = true;
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

        _connection.OnDataChannel = channel => 
        {
            _dataChannel = channel;
            _dataChannel.OnMessage = bytes =>
            {
                var message = System.Text.Encoding.UTF8.GetString(bytes);
                Debug.Log($"Receiver Received: " + message);
            };
        };

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

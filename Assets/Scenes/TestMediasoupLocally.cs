using Mediasoup;
using Mediasoup.Transports;
using Mediasoup.Types;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Net.WebSockets;
using System;
using System.Threading;
using System.Text;
using Mediasoup.RtpParameter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Mediasoup.SctpParameter;
using Unity.WebRTC;

public class TestMediasoupLocally : MonoBehaviour
{
    public Device DeviceObj;
    public RtpCapabilities RtpCapabilitiesObj;

    public Transport<AppData> SendTransport;
    public Transport<AppData> ReceiveTransport;

    public Producer<AppData> ProducerObj;
    public Consumer<AppData> ConsumerObj;

    private WebCamTexture _webCamTexture;
    private Texture _webCamStreamingTexture;

    [SerializeField]
    private RawImage _localVideoRawImage;

    [SerializeField]
    private RawImage _remoteVideoSource;

    private ClientWebSocket _websocket;

    private CancellationToken _tokenSource;

    private string _socketUrl = "ws://localhost:8080";

    private int _producerId;

    public ProducerOptions<AppData> ProducerOptionsObj;

    // Start is called before the first frame update
    async void Start()
    {
        ProducerOptionsObj = new ProducerOptions<AppData>
        {
            encodings = {   new RtpEncodingParameters
                            {
                                Rid = "r0",
                                MaxBitrate = 100000,
                                ScalabilityMode = "S1T3"
                            },
                            new RtpEncodingParameters
                            {
                                Rid = "r1",
                                MaxBitrate = 300000,
                                ScalabilityMode = "S1T3"
                            },new RtpEncodingParameters
                            {
                                Rid = "r2",
                                MaxBitrate = 900000,
                                ScalabilityMode = "S1T3"
                            }
                        },

            codecOptions = { videoGoogleStartBitrate = 1000 }
        };

        _websocket = new ClientWebSocket();

        Uri serverUrl = new Uri(_socketUrl);
        _tokenSource = new CancellationToken();
        await _websocket.ConnectAsync(serverUrl, CancellationToken.None);
        Debug.Log($"Connection successfully with {_websocket.State.ToString()}");
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void GetLocalVideo()
    {
        StartCoroutine(CaptureWebCamVideo());
        ProducerOptionsObj.track = new VideoStreamTrack(_webCamTexture);
    }

    public async void GetRtpCapabilities()
    {
        var data = new { type = "getRtpCapabilities", data = "" };
        var encodedPayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));

        Debug.Log($"Sending Message: getRtpCapabilities");
        await _websocket.SendAsync(encodedPayload, WebSocketMessageType.Text, true, CancellationToken.None);

        string receivedMessage = await ReceiveMessage(_websocket);
        Debug.Log($"Received: {receivedMessage}");

        var jsonParsed = JObject.Parse(receivedMessage);

        var receivedData = jsonParsed["data"]["rtpCapabilities"].ToString();

        Debug.Log($"receivedData: {receivedData}");

        RtpCapabilitiesObj = JsonConvert.DeserializeObject<RtpCapabilities>(receivedData);
        Debug.Log(RtpCapabilitiesObj.Codecs.Count);
    }


    public async void CreateDevice()
    {
        DeviceObj = new Device();
        await DeviceObj.Load(RtpCapabilitiesObj);
    }

    public async void CreateSendTransport()
    {
        if (DeviceObj == null) return;
        var data = new { type = "createWebRtcTransport", data = new { sender = true } };
        var encoded = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
        await _websocket.SendAsync(encoded, WebSocketMessageType.Text, true, CancellationToken.None);

        string receivedMessage = await ReceiveMessage(_websocket);
        Debug.Log($"Received: {receivedMessage}");
        if (string.IsNullOrEmpty(receivedMessage)) return;

        var jsonParsed = JObject.Parse(receivedMessage);

        string id = (string)jsonParsed["id"];
        
        IceParameters iceParameters = JsonConvert.DeserializeObject<IceParameters>(jsonParsed["data"]["iceParameters"].ToString());
        
        List<IceCandidate> iceCandidates = JsonConvert.DeserializeObject<List<IceCandidate>>(jsonParsed["data"]["iceCandidates"].ToString());

        DtlsParameters dtlsParameters = JsonConvert.DeserializeObject<DtlsParameters>(jsonParsed["data"]["dtlsParameters"].ToString());

        SctpParameters sctpParameters = null; 
        if (jsonParsed["data"]["sctpParameters"] != null) JsonConvert.DeserializeObject<SctpParameters>(jsonParsed["data"]["sctpParameters"].ToString());
        
        List<RTCIceServer> iceServers = null;
        if (jsonParsed["data"]["iceServers"] != null) JsonConvert.DeserializeObject<List<RTCIceServer>>(jsonParsed["data"]["iceServers"].ToString());
        
        RTCIceTransportPolicy iceTransportPolicy = RTCIceTransportPolicy.All;
        if (jsonParsed["data"]["iceTransportPolicy"] != null) JsonConvert.DeserializeObject<RTCIceTransportPolicy>(jsonParsed["data"]["iceTransportPolicy"].ToString());
        
        object additionalSettings = null;
        if (jsonParsed["data"]["additionalSettings"] != null) JsonConvert.DeserializeObject<object>(jsonParsed["data"]["additionalSettings"].ToString());
        
        object proprietaryConstraints = null;
        if (jsonParsed["data"]["proprietaryConstraints"] != null) JsonConvert.DeserializeObject<object>(jsonParsed["data"]["proprietaryConstraints"].ToString());
        
        AppData appData = new AppData();
        SendTransport = DeviceObj.CreateSendTransport(id, iceParameters, iceCandidates, dtlsParameters, sctpParameters, iceServers,
                                                iceTransportPolicy, additionalSettings, proprietaryConstraints, appData);

        SendTransport.On("connect", async (args) =>
        {
            var parameters = (Tuple<DtlsParameters, Action, Action<string>>)args[0];
            DtlsParameters dtlsParams = parameters.Item1;

            var responseData = new Dictionary<string, object>
            {
                { "transportId", SendTransport.id },
                { "dtlsParameters", dtlsParams}
            };

            //convert disctionary to json
            string responsePayload = JsonConvert.SerializeObject(responseData);

            var data = new { type = "transport-connect", data = responsePayload };

            var encodedPayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));

            await _websocket.SendAsync(encodedPayload, WebSocketMessageType.Text, true, CancellationToken.None);

            parameters.Item2?.Invoke();

        });

        SendTransport.On("produce", async (args) =>
        {
            var parameters = (Tuple<TrackKind, RtpParameters, AppData, Action<int>>)args[0];
            //todo write logic to get producer id

            var responseData = new Dictionary<string, object>
            {
                { "transportId", SendTransport.id },
                { "kind", parameters.Item1.ToString()},
                { "rtpParameters", parameters.Item2},
                { "appData", parameters.Item3}
            };


            string responsePayload = JsonConvert.SerializeObject(responseData);

            var data = new { type = "transport-produce", data = responsePayload };

            var encodedPayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));

            await _websocket.SendAsync(encodedPayload, WebSocketMessageType.Text, true, CancellationToken.None);

            string receivedResult = await ReceiveMessage(_websocket);

            _producerId = int.Parse(receivedResult);

            parameters.Item4?.Invoke(_producerId);

        });
    }

    public async void ConnectSendTransportAndProduce()
    {
        
        ProducerObj = await SendTransport.ProduceAsync(GetProducerId, ProducerOptionsObj);

        ProducerObj.On("trackended", async (args) =>
        {
            Debug.Log("Track ended");
        });

        ProducerObj.On("transportclose", async (args) =>
        {
            Debug.Log("Transport ended");
        });

    }

    private async Task<int> GetProducerId(TrackKind kind, RtpParameters rtp, AppData _appdata)
    {
        return _producerId;
    }

    public async void CreateRevcTransport()
    {
        if (DeviceObj == null) return;

        var encoded = Encoding.UTF8.GetBytes("createWebRtcTransport");
        await _websocket.SendAsync(encoded, WebSocketMessageType.Text, true, CancellationToken.None);

        string receivedMessage = await ReceiveMessage(_websocket);

        if (string.IsNullOrEmpty(receivedMessage)) return;
        var jsonParsed = JObject.Parse(receivedMessage);


        string id = (string)jsonParsed["id"];
        IceParameters iceParameters = JsonConvert.DeserializeObject<IceParameters>((string)jsonParsed["iceParameters"]);
        List<IceCandidate> iceCandidates = JsonConvert.DeserializeObject<List<IceCandidate>>((string)jsonParsed["iceParameters"]);
        DtlsParameters dtlsParameters = JsonConvert.DeserializeObject<DtlsParameters>((string)jsonParsed["dtlsParameters"]);
        SctpParameters sctpParameters = JsonConvert.DeserializeObject<SctpParameters>((string)jsonParsed["sctpParameters"]);
        List<RTCIceServer> iceServers = JsonConvert.DeserializeObject<List<RTCIceServer>>((string)jsonParsed["iceServers"]);
        RTCIceTransportPolicy iceTransportPolicy = JsonConvert.DeserializeObject<RTCIceTransportPolicy>((string)jsonParsed["iceTransportPolicy"]);
        object additionalSettings = JsonConvert.DeserializeObject<object>((string)jsonParsed["additionalSettings"]);
        object proprietaryConstraints = JsonConvert.DeserializeObject<object>((string)jsonParsed["proprietaryConstraints"]);
        AppData appData = new AppData();

        ReceiveTransport = DeviceObj.CreateRecvTransport(id, iceParameters, iceCandidates, dtlsParameters, sctpParameters, iceServers,
                                                iceTransportPolicy, additionalSettings, proprietaryConstraints, appData);


        ReceiveTransport.On("connect", async (args) =>
        {
            var parameters = (Tuple<DtlsParameters, Action, Action<string>>)args[0];
            DtlsParameters dtlsParams = parameters.Item1;

            var responseData = new Dictionary<string, object>
            {
                { "transportId", ReceiveTransport.id },
                { "dtlsParameters", dtlsParams}
            };

            //convert disctionary to json
            string responsePayload = JsonConvert.SerializeObject(responseData);

            var data = new { type = "transport-recv-connect", data = responsePayload };

            var encodedPayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));

            await _websocket.SendAsync(encodedPayload, WebSocketMessageType.Text, true, CancellationToken.None);

            parameters.Item2?.Invoke();

        });



    }

    public async void ConnectRevcTransportAndConsume()
    {
        var responseData = new Dictionary<string, object>
        {
            { "rtpCapabilities", DeviceObj.GetRtpCapabilities() }
        };

        string responsePayload = JsonConvert.SerializeObject(responseData);

        var data = new { type = "consume", data = responsePayload };

        var encodedPayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
        await _websocket.SendAsync(encodedPayload, WebSocketMessageType.Text, true, CancellationToken.None);

        string receivedMessage = await ReceiveMessage(_websocket);

        if (string.IsNullOrEmpty(receivedMessage)) return;

        var jsonParsed = JObject.Parse(receivedMessage);


        string id = (string)jsonParsed["id"];
        string producerId = (string)jsonParsed["producerId"];
        string mediaKind = (string)jsonParsed["kind"];
        RtpParameters rtpParameters = JsonConvert.DeserializeObject<RtpParameters>((string)jsonParsed["rtpParameters"]);

        ConsumerOptions options = new ConsumerOptions
        {
            id = id,
            producerId = producerId,
            kind = mediaKind,
            rtpParameters = rtpParameters
        };

        ConsumerObj = await ReceiveTransport.ConsumeAsync<AppData>();

        VideoStreamTrack track = ConsumerObj.track as VideoStreamTrack;

        track.OnVideoReceived += tex =>
        {
            _remoteVideoSource.texture = tex;
        };

    }

    private IEnumerator CaptureWebCamVideo()
    {
        WebCamDevice userCameraDevice = WebCamTexture.devices[0];
        _webCamTexture = new WebCamTexture(userCameraDevice.name, 1280, 720, 30);
        _webCamTexture.Play();

        yield return new WaitUntil(() => _webCamTexture.didUpdateThisFrame);

        Vector2 textureSize = _webCamTexture.texelSize;
        float aspectRatio = textureSize.x / textureSize.y;
        RectTransform videoRect = _localVideoRawImage.GetComponent<RectTransform>();

        videoRect.sizeDelta = new Vector2(videoRect.sizeDelta.x, videoRect.sizeDelta.y * aspectRatio);
        //_webCamStreamingTexture = new Texture2D(1280, 720, GraphicsFormat.B8G8R8A8_UNorm, TextureCreationFlags.None);
        _webCamStreamingTexture = _webCamTexture;

        _localVideoRawImage.texture = _webCamStreamingTexture;
    }

    private async Task<string> ReceiveMessage(ClientWebSocket webSocket)
    {
        byte[] buffer = new byte[1024];
        StringBuilder result = new StringBuilder();

        while (webSocket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult response = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (response.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
            else
            {
                result.Append(Encoding.UTF8.GetString(buffer, 0, response.Count));

                if (response.EndOfMessage)
                {
                    break;
                }
            }
        }

        return result.ToString();
    }

}

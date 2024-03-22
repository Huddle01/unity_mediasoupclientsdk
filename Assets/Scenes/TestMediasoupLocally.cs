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

    private string _socketUrl = "ws://localhost:8081";

    private string _producerId;

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

            codecOptions = { videoGoogleStartBitrate = 1000 },
        };

        _websocket = new ClientWebSocket();

        Uri serverUrl = new Uri(_socketUrl);
        _tokenSource = new CancellationToken();
        await _websocket.ConnectAsync(serverUrl, CancellationToken.None);
        Debug.Log($"Connection successfully with {_websocket.State.ToString()}");

        StartCoroutine(WebRTC.Update());
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void GetLocalVideo()
    {
        StartCoroutine(CaptureWebCamVideo());
        VideoStreamTrack videoStreamTrack = new VideoStreamTrack(_webCamTexture);
        videoStreamTrack.Enabled = true;
        ProducerOptionsObj.track = videoStreamTrack;

        Debug.Log("Video Stream State: " + videoStreamTrack.ReadyState);
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
        Debug.Log("RTP Capabilites: " + JsonConvert.SerializeObject(RtpCapabilitiesObj));
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

        string id = (string)jsonParsed["data"]["id"];

        IceParameters iceParameters = JsonConvert.DeserializeObject<IceParameters>(jsonParsed["data"]["iceParameters"].ToString());

        List<IceCandidate> iceCandidates = JsonConvert.DeserializeObject<List<IceCandidate>>(jsonParsed["data"]["iceCandidates"].ToString());
        Debug.Log($"Ice Candidate received from server: {jsonParsed["data"]["iceCandidates"]}");

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
            //Debug.Log("Send Transport connected");
            DtlsParameters dtlsParameters = (DtlsParameters)args[0];
            Action callBack = (Action)args[1];
            Action<Exception> errBack = (Action<Exception>)args[2];

            try
            {
                var responseData = new Dictionary<string, object>
            {
                { "transportId", SendTransport.id },
                { "dtlsParameters", dtlsParameters}
            };

                var responseObj = new { type = "transport-connect", data = responseData };

                var encodedPayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(responseObj));

                await _websocket.SendAsync(encodedPayload, WebSocketMessageType.Text, true, CancellationToken.None);

                callBack();
            }
            catch (Exception ex)
            {
                errBack(ex);
            }

        });

        SendTransport.On("produce", async (args) =>
        {
            Debug.Log("Send Transport starting to produce");
        });

        SendTransport.On("restartIce", async (args) =>
        {
            Debug.Log("Restarting Ice Request send to server");
            var responseData = new Dictionary<string, object>
            {
                { "iceCandidates", iceCandidates}
            };
            var responseObj = new { type = "transport-connect", data = responseData };
            var encodedPayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(responseObj));
            await _websocket.SendAsync(encodedPayload, WebSocketMessageType.Text, true, CancellationToken.None);
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

    private async Task<string> GetProducerId(TrackKind kind, RtpParameters rtp, AppData _appdata)
    {
        RtpParameters rtpParameters = rtp;

        rtpParameters.Codecs[0].RtcpFeedback = ProducerOptionsObj.codec.RtcpFeedback;

        var responseData = new
        {
            transportId = SendTransport.id,
            kind = kind == TrackKind.Audio ? "audio" : "video",
            rtpParameters = rtpParameters,
        };

        var data = new { type = "transport-produce", data = responseData };

        var transportProduceResponseString = JsonConvert.SerializeObject(data);

        Debug.Log("Transport-Produce response: " + transportProduceResponseString);

        var encodedPayload = Encoding.UTF8.GetBytes(transportProduceResponseString);

        Debug.Log($"transport-produce request: {encodedPayload}");

        await _websocket.SendAsync(encodedPayload, WebSocketMessageType.Text, true, CancellationToken.None);

        string receivedResult = await ReceiveMessage(_websocket);

        _producerId = receivedResult;
        Debug.Log($"ProduceID {_producerId}");
        return _producerId;
    }

    public async void CreateRevcTransport()
    {
        if (DeviceObj == null) return;
        var data = new { type = "createWebRtcTransport", data = new { sender = false } };
        var encoded = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
        await _websocket.SendAsync(encoded, WebSocketMessageType.Text, true, CancellationToken.None);

        string receivedMessage = await ReceiveMessage(_websocket);
        Debug.Log($"Received: {receivedMessage}");
        if (string.IsNullOrEmpty(receivedMessage)) return;

        var jsonParsed = JObject.Parse(receivedMessage);

        string id = (string)jsonParsed["data"]["id"];

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

        ReceiveTransport = DeviceObj.CreateRecvTransport(id, iceParameters, iceCandidates, dtlsParameters, sctpParameters, iceServers,
                                                iceTransportPolicy, additionalSettings, proprietaryConstraints, appData);

        Debug.Log("Recv transport connected");


        ReceiveTransport.On("connect", async (args) =>
        {
            Debug.Log("Receive Transport connected");
            DtlsParameters dtlsParameters = (DtlsParameters)args[0];
            Action callBack = (Action)args[1];
            Action<Exception> errBack = (Action<Exception>)args[2];

            try
            {
                var responseData = new Dictionary<string, object>
            {
                { "transportId", SendTransport.id },
                { "dtlsParameters", dtlsParameters}
            };


                var data = new { type = "transport-recv-connect", data = responseData };

                var encodedPayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));

                await _websocket.SendAsync(encodedPayload, WebSocketMessageType.Text, true, CancellationToken.None);

                callBack();
            }
            catch (Exception ex)
            {
                errBack(ex);
            }
        });
    }

    public async void ConnectRevcTransportAndConsume()
    {
        Debug.Log("ConnectRevcTransportAndConsume()");

        var responseData = new
        {
            rtpCapabilities = DeviceObj.GetRtpCapabilities()
        };

        var data = new { type = "consume", data = responseData };

        Debug.Log(JsonConvert.SerializeObject(data));
        var encodedPayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
        await _websocket.SendAsync(encodedPayload, WebSocketMessageType.Text, true, CancellationToken.None);

        string receivedMessage = await ReceiveMessage(_websocket);
        Debug.Log($"Received: {receivedMessage}");

        if (string.IsNullOrEmpty(receivedMessage)) return;

        var jsonParsed = JObject.Parse(receivedMessage)["data"]["params"];

        string id = (string)jsonParsed["id"];
        string producerId = (string)jsonParsed["producerId"];
        string mediaKind = (string)jsonParsed["kind"];

        Debug.Log($"id {id}, producerId: {producerId}, mediaKind: {mediaKind}");

        Debug.Log($"RtpParameters: {jsonParsed["rtpParameters"].ToString()}");

        RtpParameters rtpParameters = JsonConvert.DeserializeObject<RtpParameters>(jsonParsed["rtpParameters"].ToString());

        //Debug.Log($"id: {id}, producerId: {producerId}, mediaKind: {mediaKind}, rtpParameters: {JsonConvert.SerializeObject(rtpParameters)}");

        ConsumerOptions options = new ConsumerOptions
        {
            id = id,
            producerId = producerId,
            kind = mediaKind.Trim().ToLower(),
            rtpParameters = rtpParameters
        };

        Debug.Log("Create Consumer Obj");

        ReceiveTransport.ConsumeAsync<AppData>(options, HandleConsumer);
    }

    private void HandleConsumer(Consumer<AppData> consumer)
    {
        Debug.Log("Consumer Creation Done");
        ConsumerObj = consumer;

        Debug.Log("Consumer Obj null?? " + ConsumerObj == null);
        Debug.Log("Is track null?? " + ConsumerObj.track == null);

        VideoStreamTrack track = ConsumerObj.track as VideoStreamTrack;

        track.Enabled = true;

        track.OnVideoReceived += tex =>
        {
            _remoteVideoSource.texture = tex;
        };

        Debug.Log("Emitting consume resume");
        var consumeResponse = new { type = "consumer-resume" };
        var consumeResponsePayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(consumeResponse));
        _websocket.SendAsync(consumeResponsePayload, WebSocketMessageType.Text, true, CancellationToken.None);
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

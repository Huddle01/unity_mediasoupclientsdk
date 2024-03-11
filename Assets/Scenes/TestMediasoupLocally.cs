using Mediasoup;
using Mediasoup.Transports;
using Mediasoup.Types;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class TestMediasoupLocally : MonoBehaviour
{
    public Device DeviceObj;

    public Transport<AppData> SendTransport;
    public Transport<AppData> ReceiveTransport;

    private WebCamTexture _webCamTexture;
    private Texture _webCamStreamingTexture;

    [SerializeField]
    private RawImage _localVideoRawImage;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GetLocalVideo() 
    {
        StartCoroutine(CaptureWebCamVideo());
    }

    public async Task GetRtpCapabilities() 
    {
    
    }

    public void CreateDevice() 
    {
        DeviceObj = new Device();
    }

    public void CreateSendTransport()
    {
        if (DeviceObj == null) return;
        //SendTransport = DeviceObj.CreateSendTransport();
    }

    public void ConnectSendTransportAndProduce()
    {

    }

    public void CreateRevcTransport()
    {
        if (DeviceObj == null) return;
        //ReceiveTransport = DeviceObj.CreateRecvTransport();
    }

    public void ConnectRevcTransportAndConsume()
    {
        
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

        videoRect.sizeDelta = new Vector2(videoRect.sizeDelta.x, videoRect.sizeDelta.y* aspectRatio);
        //_webCamStreamingTexture = new Texture2D(1280, 720, GraphicsFormat.B8G8R8A8_UNorm, TextureCreationFlags.None);
        _webCamStreamingTexture = _webCamTexture;

        _localVideoRawImage.texture = _webCamStreamingTexture;
    }

}

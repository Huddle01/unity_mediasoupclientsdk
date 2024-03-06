using Mediasoup.RtpParameter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mediasoup.Transports;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class TestHandler : MonoBehaviour
{
    [SerializeField]
    private RtpParameters rtpParam;

    public string myname;

    // Start is called before the first frame update
    HandlerInterface handler;

    AwaitQueue awaitQueue;
    async void Start()
    {
        GetJson();
        awaitQueue = new AwaitQueue();
        handler = new HandlerInterface("Unity");
        RegiserEvent();

        DtlsParameters dtls = new DtlsParameters();
        dtls.role = DtlsRole.server;

        _ = handler.Emit("connect", dtls);

        await ResumePendingConsumers();
        await ResumePendingConsumers1();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void GetJson() 
    {
        Debug.Log(JsonConvert.SerializeObject(rtpParam));
    }

    private void RegiserEvent() 
    {
        handler.On("connect", async args =>
        {
            Debug.Log("Onconnect");
            var dtls = (dynamic)args[0];
            if (dtls is DtlsParameters) 
            {
                DtlsParameters temp = dtls as DtlsParameters;
                Debug.Log(temp.role);
            }
        });

        handler.On("connect", async args =>
        {
            Debug.Log("On connect 2");
            var dtls = (dynamic)args[0];
            if (dtls is DtlsParameters)
            {
                DtlsParameters temp = dtls as DtlsParameters;
                Debug.Log(temp.role);
            }
        });
    }

    public async Task ResumePendingConsumers()
    {
        try
        {
            await awaitQueue.Push(async () =>
            {
                Debug.Log("ResumePendingConsumers");
                await Task.Delay(1000);
                return new object();
            }, "transport.resumePendingConsumers");

        }
        finally
        {
            Debug.Log("Finally block");
        }
    }

    public async Task ResumePendingConsumers1()
    {
        try
        {
            await awaitQueue.Push(async () =>
            {
                Debug.Log("ResumePendingConsumers1");
                await Task.Delay(1000);
                return new object();
            }, "transport.resumePendingConsumers1");

        }
        finally
        {
            Debug.Log("Finally block 1");
        }
    }
}

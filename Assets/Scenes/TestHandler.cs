using Mediasoup.RtpParameter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mediasoup.Transports;

public class TestHandler : MonoBehaviour
{
    // Start is called before the first frame update
    HandlerInterface handler;
    async void Start()
    {
        handler = new HandlerInterface("Unity");
        RegiserEvent();

        DtlsParameters dtls = new DtlsParameters();
        dtls.role = DtlsRole.server;

        _ = handler.Emit("connect", dtls);
    }

    // Update is called once per frame
    void Update()
    {
        
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
    }
}

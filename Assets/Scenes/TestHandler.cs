using Mediasoup.RtpParameter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System;

public class TestHandler : MonoBehaviour
{
    // Start is called before the first frame update
    async void Start()
    {

        Console.WriteLine("Start");
        HandlerInterface h = new HandlerInterface("Unity");

        var obj = new RtpHeaderExtension {
            Direction = RtpHeaderExtensionDirection.SendReceive,
            Kind = MediaKind.VIDEO,
            PreferredId = 1,
            PreferredEncrypt = false,
            Uri = RtpHeaderExtensionUri.Mid
        };

        var jsonString = JsonConvert.SerializeObject(obj);
        Debug.Log(jsonString);
        Console.WriteLine(jsonString);
    }

    // Update is called once per frame
    void Update()
    {
        Console.WriteLine("Update");
    }
}

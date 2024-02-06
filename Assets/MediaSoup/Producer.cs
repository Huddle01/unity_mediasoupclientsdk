using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;
using System;
using Mediasoup.RtpParameter;

namespace Mediasoup
{
    interface IProducer
    {
        string _id { private get; private set; }
        string _localId { private get; private set; }
        bool _closed { private get; private set; }

        RTCRtpSender _rtpSender { private get; private set; }
        string _producerId { private get; private set; }

        MediaStreamTrack _track { private get; private set; }

        RtpParameters _rtpParameters { private get; private set; }

        bool _paused { private get; private set; }

        ProducerAppData _appData { private get; private set; }



    }

    public class Producer : EnhancedEventEmitter<ProducerEvents>, IProducer
    {


    }
}

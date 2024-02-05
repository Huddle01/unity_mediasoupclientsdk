using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Mediasoup.SctpParameter
{
    public class SctpParameters
    {
        public int port;
    }

    [Serializable]
    public class SctpCapabilities 
    {
        public NumSctpStreams numStreams;
        public int OS;
        public int MIS;
        public int maxMessageSize;
    }

    [Serializable]
    public class NumSctpStreams 
    {
        public int OS;
        public int MIS;
    }

    [Serializable]
    public class SctpStreamParameters 
    {
        public int streamId;
        public bool? ordered { get; set; }
        public int? maxPacketLifeTime { get; set; }
        public int? maxRetransmits { get; set; }
        public string label;
        public string protocol;
    }

}



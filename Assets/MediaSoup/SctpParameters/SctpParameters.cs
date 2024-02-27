using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Mediasoup.SctpParameter
{
    public class SctpParameters
    {
        public int port;
        public int os;
        public int mis;
        public int maxMessageSize;
    }

    [Serializable]
    public class SctpCapabilities 
    {
        public NumSctpStreams numStreams;

        public SctpCapabilities() 
        {
            numStreams = new NumSctpStreams();
        }
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



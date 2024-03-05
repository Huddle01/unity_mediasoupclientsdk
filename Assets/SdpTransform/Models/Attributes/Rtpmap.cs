using System;
using System.Collections.Generic;
using System.Text;

namespace Utilme.SdpTransform
{
    public class Rtpmap
    {
        public const string Label = "rtpmap:";
        public byte PayloadType { get; set; }
        public string EncodingName { get; set; }
        public uint ClockRate { get; set; }
        public byte? Channels { get; set; }
    }
}

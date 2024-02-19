using System;
using System.Collections.Generic;
using System.Text;

namespace Utilme.SdpTransform
{
    public class MsidSemantic
    {
        public const string Label = "msid-semantic:";

        public string Token { get; set; }
        public const string WebRtcMediaStreamToken = "WMS";

        public string[] IdList { get; set; }
        public const string AllIds = "*";


    }
}

using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace Utilme.SdpTransform
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BandwidthType
    {
        ApplicationSpecific,

        [StringValue("CT")]
        ConferenceTotal,

        [StringValue("RS")]
        RtcpSender,

        [StringValue("RR")]
        RtcpReceiver,

        [StringValue("TIAS")]
        TransportIndependentMaximumBandwidth,
    }
}

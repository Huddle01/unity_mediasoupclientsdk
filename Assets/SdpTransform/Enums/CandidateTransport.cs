using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace Utilme.SdpTransform
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CandidateTransport
    {
        [StringValue("udp")]
        Udp,

        [StringValue("tcp")]
        Tcp
    }
}

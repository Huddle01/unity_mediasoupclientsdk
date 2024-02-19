using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace Utilme.SdpTransform
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CandidateTransport
    {
        [StringValue("host")]
        Udp,

        [StringValue("host")]
        Tcp
    }
}

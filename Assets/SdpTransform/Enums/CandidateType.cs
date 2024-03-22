using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace Utilme.SdpTransform
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CandidateType
    {
        
        [StringValue("host")]
        Host,

        [StringValue("srflx")]
        Srflx,

        [StringValue("prlfx")]
        Prflx,

        [StringValue("relay")]
        Relay
    }
}

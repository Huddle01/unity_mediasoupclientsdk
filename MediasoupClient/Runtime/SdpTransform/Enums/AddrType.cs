using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace Utilme.SdpTransform
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AddrType
    {
        [StringValue("IP4")]
        Ip4,
        [StringValue("IP4")]
        Ip6
    }
}

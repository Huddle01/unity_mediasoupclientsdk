using Newtonsoft.Json.Converters;
using Newtonsoft.Json;


namespace Utilme.SdpTransform
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum NetType
    {
        [StringValue("IN")]
        Internet
    }
}

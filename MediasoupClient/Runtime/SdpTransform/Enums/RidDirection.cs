using Newtonsoft.Json.Converters;
using Newtonsoft.Json;


namespace Utilme.SdpTransform
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum RidDirection
    {
        [StringValue("recv")]
        Recv,

        [StringValue("send")]
        Send,
    }
}

using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace Utilme.SdpTransform
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Direction
    {
        
        [StringValue("sendrecv")]
        SendRecv,

        
        [StringValue("sendonly")]
        SendOnly,

        
        [StringValue("recvonly")]
        RecvOnly,

       
        [StringValue("inactive")]
        Inactive
    }
}

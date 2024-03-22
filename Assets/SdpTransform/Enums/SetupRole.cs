using Newtonsoft.Json.Converters;
using Newtonsoft.Json;


namespace Utilme.SdpTransform
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SetupRole
    {
        [StringValue("active")]
        Active,

        [StringValue("passive")]
        Passive,

        [StringValue("actpass")]
        ActPass,

        [StringValue("holdconn")]
        HoldConn
    }
}

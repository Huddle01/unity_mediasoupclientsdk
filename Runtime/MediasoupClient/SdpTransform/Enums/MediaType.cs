using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace Utilme.SdpTransform
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MediaType
    {
        [StringValue("audio")]
        Audio,

        [StringValue("video")]
        Video,

        [StringValue("text")]
        Text,

        [StringValue("application")]
        Application,

        [StringValue("message")]
        Message,
    }
}

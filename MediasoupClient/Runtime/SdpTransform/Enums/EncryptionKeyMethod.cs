using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace Utilme.SdpTransform
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EncryptionKeyMethod
    {
        [StringValue("clear")]
        Clear,

        [StringValue("base64")]
        Base64,

        [StringValue("uri")]
        Uri,

        [StringValue("prompt")]
        Prompt
    }
}

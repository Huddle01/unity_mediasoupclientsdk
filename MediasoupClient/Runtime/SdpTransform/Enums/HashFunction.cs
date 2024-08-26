using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace Utilme.SdpTransform
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum HashFunction
    {
        [StringValue("sha-1")]
        Sha1,

        [StringValue("sha-224")]
        Sha224,

        [StringValue("sha-256")]
        Sha256,
        
        [StringValue("sha-384")]
        Sha384,

        [StringValue("sha-512")]
        Sha512,

        [StringValue("md2")]
        Md2,

        [StringValue("md5")]
        Md5
    }
}

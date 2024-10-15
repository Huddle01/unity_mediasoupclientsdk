using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace Utilme.SdpTransform
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum GroupSemantics
    {
        [StringValue("LS")]
        LipSynchronization,

        [StringValue("FID")]
        FlowIdentification,

        [StringValue("BUNDLE")]
        Bundle
    }
}

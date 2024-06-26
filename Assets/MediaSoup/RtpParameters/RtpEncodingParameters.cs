using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace Mediasoup.RtpParameter
{
    public class RtpEncodingParameters
    {


        [JsonProperty("active")]
        public bool Active { get; set; }

        /// <summary>
        /// The media SSRC.
        /// </summary>

        [JsonProperty("ssrc", NullValueHandling =NullValueHandling.Ignore)]
        public uint? Ssrc { get; set; }

        /// <summary>
        /// The RID RTP extension value. Must be unique.
        /// </summary>
        [JsonProperty("rid")]
        public string? Rid { get; set; }

        /// <summary>
        /// Codec payload type this encoding affects. If unset, first media codec is
        /// chosen.
        /// </summary>
        [JsonProperty("codecPayloadType", NullValueHandling = NullValueHandling.Ignore)]
        public byte? CodecPayloadType { get; set; }

        /// <summary>
        /// RTX stream information. It must contain a numeric ssrc field indicating
        /// the RTX SSRC.
        /// </summary>
        [JsonProperty("rtx", NullValueHandling = NullValueHandling.Ignore)]
        public Rtx? Rtx { get; set; }

        /// <summary>
        /// It indicates whether discontinuous RTP transmission will be used. Useful
        /// for audio (if the codec supports it) and for video screen sharing (when
        /// static content is being transmitted, this option disables the RTP
        /// inactivity checks in mediasoup). Default false.
        /// </summary>
        [JsonProperty("dtx")]
        public bool Dtx { get; set; }

        /// <summary>
        /// Number of spatial and temporal layers in the RTP stream (e.g. 'L1T3').
        /// See webrtc-svc.
        /// </summary>
        [JsonProperty("scalabilityMode")]
        public string? ScalabilityMode { get; set; }

        /// <summary>
        /// Unused.
        /// </summary>
        [JsonProperty("scaleResolutionDownBy", NullValueHandling = NullValueHandling.Ignore)]
        public int? ScaleResolutionDownBy { get; set; }

        /// <summary>
        /// MaxBitrate.
        /// </summary>
        [JsonProperty("maxBitrate")]
        public uint? MaxBitrate { get; set; }


        [JsonProperty("maxFramerate", NullValueHandling = NullValueHandling.Ignore)]
        public uint? MaxFramerate { get; set; }

        [JsonProperty("adaptivePtime", NullValueHandling = NullValueHandling.Ignore)]
        public bool? AdaptivePtime { get; set;  }

        [JsonProperty("priority", NullValueHandling = NullValueHandling.Ignore)]
        public PriorityLevel? priority { get; set; }

        [JsonProperty("networkPriority", NullValueHandling = NullValueHandling.Ignore)]
        public PriorityLevel? networkPriority { get; set; }


        [JsonConverter(typeof(StringEnumConverter))]
        public enum PriorityLevel {
            [EnumMember(Value = "very-low")]
            VeryLow,

            [EnumMember(Value = "low")]
            Low,

            [EnumMember(Value = "medium")]
            Medium,

            [EnumMember(Value = "high")]
            High
        }

    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;
using System;
using Newtonsoft.Json;
using System.Text.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace Mediasoup.RtpParameter
{

    [Serializable]
    public class RtpParameters
    {
        /// <summary>
        /// The MID RTP extension value as defined in the BUNDLE specification.
        /// </summary>
        [JsonProperty("mid")]
        public string? Mid { get; set; }

        /// <summary>
        /// Media and RTX codecs in use.
        /// </summary>
        [JsonProperty("codecs")]
        public List<RtpCodecParameters> Codecs { get; set; }

        /// <summary>
        /// RTP header extensions in use.
        /// </summary>
        [JsonProperty("headerExtensions")]
        public List<RtpHeaderExtensionParameters>? HeaderExtensions { get; set; }

        /// <summary>
        /// Transmitted RTP streams and their settings.
        /// </summary>
        [JsonProperty("encodings")]
        public List<RtpEncodingParameters> Encodings { get; set; } = new List<RtpEncodingParameters>();

        /// <summary>
        /// Parameters used for RTCP.
        /// </summary>
        [JsonProperty("rtcp")]
        public RtcpParameters Rtcp { get; set; } = new RtcpParameters();
    }

    [Serializable]
    public class ExtendedRtpCapabilities
    {
        public List<ExtendedRtpCodecCapability> codecs = new List<ExtendedRtpCodecCapability>();
        public List<ExtendedRtpHeaderExtension> headerExtensions = new List<ExtendedRtpHeaderExtension>();
    }

    public class ExtendedRtpHeaderExtension : RtpHeaderExtension
    {
        public byte sendId;
        public byte recvId;
    }

    public class ExtendedRtpCodecCapability
    {
        public string mimeType;
        public MediaKind kind;
        public uint clockRate;
        public byte? channels;
        public byte? localPayloadType;
        public byte? localRtxPayloadType;
        public byte? remotePayloadType;
        public byte? remoteRtxPayloadType;
        public IDictionary<string, object>? localParameters;
        public IDictionary<string, object>? remoteParameters;
        public List<RtcpFeedback> rtcpFeedback = new List<RtcpFeedback>();
    }

    [Serializable]
    public class RtcpFeedback
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// parameter. Nullable.
        /// </summary>
        [JsonProperty("parameter")]
        public string? Parameter { get; set; }
    }


    [Serializable]
    public class Rtx
    {
        public uint Ssrc { get; set; }
    }


    [JsonConverter(typeof(StringEnumConverter))]
    public enum MediaKind
    {
        [EnumMember(Value = "audio")]
        AUDIO,

        [EnumMember(Value = "video")]
        VIDEO,

        [EnumMember(Value = "application")]
        APPLICATION
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum RtpHeaderExtensionDirection
    {
        [EnumMember(Value = "sendrecv")]
        SendReceive,

        [EnumMember(Value = "sendonly")]
        SendOnly,

        [EnumMember(Value = "recvonly")]
        ReceiveOnly,

        [EnumMember(Value = "inactive")]
        Inactive
    }
}

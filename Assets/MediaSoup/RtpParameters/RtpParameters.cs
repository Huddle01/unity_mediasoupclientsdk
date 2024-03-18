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

    //[Serializable]
    //public class RtpCapabilities
    //{
    //    public List<RtpCodecCapability> codecs = new List<RtpCodecCapability>();
    //    public List<RtpHeaderExtension> headerExtensions = new List<RtpHeaderExtension>();
    //}

    //[Serializable]
    //public class RtpHeaderExtension
    //{
    //    public Nullable<MediaKind> kind;
    //    public string uri;
    //    public int preferredId;
    //    public bool? preferredEncrypt { get; set; }
    //    public RtpHeaderExtensionDirection? direction { get; set; }
    //}

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

    //[Serializable]
    //public class RtpEncodingParameters
    //{
    //    public int ssrc;
    //    public string rid;
    //    public int codecPayloadType;
    //    public bool? dtx { get; set; }
    //    public string scalabilityMode;
    //    public int scaleResolutionDownBy;
    //    public int maxBitrate;
    //    public int maxFramerate;
    //    public bool adaptivePtime;
    //    public string priority;
    //    public string networkPriority;
    //    public RtxParameters? rtx;

    //    /*
    //     priority?: 'very-low' | 'low' | 'medium' | 'high';
    // networkPriority?: 'very-low' | 'low' | 'medium' | 'high';
    //    */

    //    public class RtxParameters
    //    {
    //        public int ssrc { get; set; }
    //    }

    //}

    [Serializable]
    public class Rtx
    {
        public uint Ssrc { get; set; }
    }



    //public enum RtpHeaderExtensionUri
    //{
    //    UrnIetfParamsRtpHdrextSdesMid,
    //    UrnIetfParamsRtpHdrextSdesRtpStreamId,
    //    UrnIetfParamsRtpHdrextSdesRepairedRtpStreamId,
    //    HttpToolsIetfAvtextFramemarking07,
    //    UrnIetfParamsRtpHdrextFramemarking,
    //    UrnIetfParamsRtpHdrextSsrcAudioLevel,
    //    Urn3gppVideoOrientation,
    //    UrnIetfParamsRtpHdrextToffset,
    //    HttpIetfOrgIdDraftHolmerRmcatTransportWideCcExtensions01,
    //    HttpWwwIetfOrgIdDraftHolmerRmcatTransportWideCcExtensions01,
    //    HttpWwwWebrtcOrgExperimentsRtpHdrextAbsSendTime,
    //    HttpWwwWebrtcOrgExperimentsRtpHdrextAbsCaptureTime
    //}

    /***

    export type RtpHeaderExtensionUri =
    | 'urn:ietf:params:rtp-hdrext:sdes:mid'
    | 'urn:ietf:params:rtp-hdrext:sdes:rtp-stream-id'
    | 'urn:ietf:params:rtp-hdrext:sdes:repaired-rtp-stream-id'
    | 'http://tools.ietf.org/html/draft-ietf-avtext-framemarking-07'
    | 'urn:ietf:params:rtp-hdrext:framemarking'
    | 'urn:ietf:params:rtp-hdrext:ssrc-audio-level'
    | 'urn:3gpp:video-orientation'
    | 'urn:ietf:params:rtp-hdrext:toffset'
    | 'http://www.ietf.org/id/draft-holmer-rmcat-transport-wide-cc-extensions-01'
    | 'http://www.webrtc.org/experiments/rtp-hdrext/abs-send-time'
    | 'http://www.webrtc.org/experiments/rtp-hdrext/abs-capture-time';
    **/

    //[Serializable]
    //public class RtpHeaderExtensionParameters
    //{
    //    public string uri;
    //    public int id;
    //    public bool? encrypt { get; set; }
    //    public dynamic? parameters { get; set; }
    //}

    [JsonConverter(typeof(StringEnumConverter))]
    public enum MediaKind
    {
        AUDIO,
        VIDEO,
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;
using System;
using Newtonsoft.Json;
using System.Text.Json;
using Newtonsoft.Json.Converters;

namespace Mediasoup.RtpParameter
{
    
    [Serializable]
    public class RtpParameters: ICloneable
    {
        public string? mid;
        public List<RtpCodecParameters> codecs = new List<RtpCodecParameters>();
        public List<RtpHeaderExtensionParameters> headerExtensions = new List<RtpHeaderExtensionParameters>();
        public List<RtpEncodingParameters> encodings = new List<RtpEncodingParameters>();
        public RtcpParameters rtcpParameters;


        public object Clone()
        {
            RtpParameters rtpParameter = new RtpParameters();
            rtpParameter.mid = this.mid;
            rtpParameter.rtcpParameters = this.rtcpParameters;
            rtpParameter.encodings = this.encodings;
            rtpParameter.headerExtensions = this.headerExtensions;
            rtpParameter.codecs = this.codecs;

            return rtpParameter;
        }
    }

    [Serializable]
    public class RtpCapabilities 
    {
        public List<RtpCodecCapability> codecs = new List<RtpCodecCapability>();
        public List<RtpHeaderExtension> headerExtensions = new List<RtpHeaderExtension>();
    }

    [Serializable]
    public class RtpCodecCapability
    {
        public string mimeType;
        public int clockRate;
        public int channels;
        public Dictionary<string, string> parameters = new Dictionary<string, string>();
        public List<RtcpFeedback> rtcpFeedback = new List<RtcpFeedback>();

        // This field is not present in the original typescript definition for RtpCodecParameters
        public Nullable<MediaKind> kind;
        public int preferredPayloadType;
    }

    [Serializable]
    public class RtpHeaderExtension 
    {
        public Nullable<MediaKind> kind;
        public string uri;
        public int preferredId;
        public bool? preferredEncrypt { get; set; }
        public RtpHeaderExtensionDirection? direction { get; set; }
    }

    [Serializable]
    public class RtpCodecParameters: RtpCodecCapability
    {
        public int payloadType;
    }


    [Serializable]
    public class ExtendedRtpCapabilities
    {
        public List<ExtendedRtpCodecCapability> codecs = new List<ExtendedRtpCodecCapability>();
        public List<ExtendedRtpHeaderExtension> headerExtensions = new List<ExtendedRtpHeaderExtension>();
    }

    public class ExtendedRtpHeaderExtension: RtpHeaderExtension {
        public int sendId;
        public int recvId;
    }

    public class ExtendedRtpCodecCapability
    {
        public string mimeType;
        public MediaKind kind;
        public int clockRate;
        public int channels;
        public int localPayloadType;
        public Nullable<int> localRtxPayloadType;
        public int remotePayloadType;
		public Nullable<int> remoteRtxPayloadType;
        public Dictionary<string, string> localParameters;
        public Dictionary<string, string> remoteParameters;
        public List<RtcpFeedback> rtcpFeedback = new List<RtcpFeedback>();
    }

    [Serializable]
    public class RtcpFeedback 
    {
        public string type;
        public string parameters;
    }

    [Serializable]
    public class RtpEncodingParameters 
    {
        public int ssrc;
        public string rid;
        public int codecPayloadType;
        public bool? dtx { get; set; }
        public string scalabilityMode;
        public int scaleResolutionDownBy;
        public int maxBitrate;
        public int maxFramerate;
        public bool adaptivePtime;
        public string priority;
        public string networkPriority;
        public RtxParameters? rtx;

        /*
         priority?: 'very-low' | 'low' | 'medium' | 'high';
	    networkPriority?: 'very-low' | 'low' | 'medium' | 'high';
        */

        public class RtxParameters
        {
            public int ssrc { get; set; }
        }

    }



    public enum RtpHeaderExtensionUri
    {
        UrnIetfParamsRtpHdrextSdesMid,
        UrnIetfParamsRtpHdrextSdesRtpStreamId,
        UrnIetfParamsRtpHdrextSdesRepairedRtpStreamId,
        HttpToolsIetfAvtextFramemarking07,
        UrnIetfParamsRtpHdrextFramemarking,
        UrnIetfParamsRtpHdrextSsrcAudioLevel,
        Urn3gppVideoOrientation,
        UrnIetfParamsRtpHdrextToffset,
        HttpIetfOrgIdDraftHolmerRmcatTransportWideCcExtensions01,
        HttpWwwIetfOrgIdDraftHolmerRmcatTransportWideCcExtensions01,
        HttpWwwWebrtcOrgExperimentsRtpHdrextAbsSendTime,
        HttpWwwWebrtcOrgExperimentsRtpHdrextAbsCaptureTime
    }

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

    [Serializable]
    public class RtpHeaderExtensionParameters 
    {
        public string uri;
        public int id;
        public bool? encrypt { get; set; }
        public dynamic? parameters { get; set; }
    }

    [Serializable]
    public class RtcpParameters 
    {
        public string cname;
        public bool? reducedSize { get; set; }
        public bool mux;
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum MediaKind
    {
        audio,
        video,
        application
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum RtpHeaderExtensionDirection
    {
        sendrecv,
        sendonly,
        recvonly,
        inactive
    }
}
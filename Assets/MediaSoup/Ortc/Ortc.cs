using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Mediasoup.RtpParameter;
using Mediasoup.SctpParameter;
using System;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Dynamic;
using System.Linq;

namespace Mediasoup.Ortc 
{
    public class Ortc
    {
        public static void ValidateRtpCapabilities(RtpCapabilities caps) 
        {
            if (caps == null)
            {
                throw new ArgumentNullException(nameof(caps));
            }

            caps.codecs ??= new();

            foreach (var codec in caps.codecs)
            {
                ValidateRtpCodecCapability(codec);
            }

            // headerExtensions is optional. If unset, fill with an empty array.
            caps.headerExtensions ??= new();

            foreach (var ext in caps.headerExtensions)
            {
                ValidateRtpHeaderExtension(ext);
            }
        }

        public static void ValidateRtpCodecCapability(RtpCodecCapability codec) 
        {
            if (codec==null) 
            {
                throw new ArgumentNullException(nameof(codec));
            }

            // mimeType is mandatory.
            if (string.IsNullOrEmpty(codec.mimeType)) 
            {
                throw new InvalidOleVariantTypeException("missing codec.mimeType");
            }

            Regex mimeTypeRegex = new Regex(@"^(audio|video)/(.+)", RegexOptions.ECMAScript | RegexOptions.IgnoreCase);

            if (!mimeTypeRegex.IsMatch(codec.mimeType)) 
            {
                throw new ArgumentException("invalid codec.mimeType");
            }

            // Just override kind with media component of mimeType.
            codec.kind = codec.mimeType.ToLower().StartsWith(nameof(MediaKind.video))
                ? MediaKind.video
                : MediaKind.audio;


            // channels is optional. If unset, set it to 1 (just if audio).
            if (codec is { kind: MediaKind.audio })
            {
                codec.channels = 1;
            }
            else
            {
                codec.channels =-1;
            }

            // parameters is optional. If unset, set it to an empty object.
            if (codec.parameters == null)
                codec.parameters = new Dictionary<string, object>();

            foreach (var (key, val) in codec.parameters)
            {
                var value = val;

                if (value == null)
                {
                    codec.parameters[key] = string.Empty;
                    value = string.Empty;
                }

                if (value is not (string or uint or int or ulong or long or byte or sbyte))
                {
                    throw new InvalidOleVariantTypeException($"invalid codec parameter [key:{key}, value:{value}]");
                }

                // Specific parameters validation.
                if (key == "apt")
                {
                    if (value is not (uint or int or ulong or long or byte or sbyte))
                    {
                        throw new InvalidOleVariantTypeException("invalid codec apt parameter");
                    }
                }
            }

            // rtcpFeedback is optional. If unset, set it to an empty array.
            codec.rtcpFeedback ??= new();

            foreach (var fb in codec.rtcpFeedback)
            {
                ValidateRtcpFeedback(fb);
            }


        }

        public static void ValidateRtcpFeedback(RtcpFeedback fb) 
        {
            if (fb == null) 
            {
                throw new ArgumentNullException(nameof(fb));
            }

            // type is mandatory.
            if (string.IsNullOrEmpty(fb.type)) 
            {
                throw new InvalidOleVariantTypeException("missing fb.type");
            }

            // parameter is optional. If unset set it to an empty string.
            if (string.IsNullOrEmpty(fb.parameters))
            {
                fb.parameters = string.Empty;
            }

        }

        public static void ValidateRtpHeaderExtension(RtpHeaderExtension ext) 
        {
            if (ext == null)
            {
                throw new ArgumentNullException(nameof(ext));
            }

            // uri is mandatory.
            if (string.IsNullOrEmpty(ext.uri))
            {
                throw new InvalidOleVariantTypeException($"missing ext.uri");
            }

            // preferredEncrypt is optional. If unset set it to false.
            ext.preferredEncrypt ??= false;

            // direction is optional. If unset set it to sendrecv.
            ext.direction ??= RtpHeaderExtensionDirection.sendrecv;


        }

        public static void ValidateRtpParameters(RtpParameters param)
        {
            if (param == null)
            {
                throw new InvalidOleVariantTypeException("params is not an object");
            }
            // mid is optional.

            // codecs is mandatory.
            if (param.codecs == null)
            {
                throw new InvalidOleVariantTypeException("missing params.codecs");
            }

            foreach (var codec in param.codecs)
            {
                ValidateRtpCodecParameters(codec);
            }

            // headerExtensions is optional. If unset, fill with an empty array.
            param.headerExtensions ??= new();

            foreach (var ext in param.headerExtensions)
            {
                ValidateRtpHeaderExtensionParameters(ext);
            }

            // encodings is optional. If unset, fill with an empty array.
            param.encodings ??= new();

            foreach (var encoding in param.encodings)
            {
                ValidateRtpEncodingParameters(encoding);
            }

            // rtcp is optional. If unset, fill with an empty object.
            param.RtcpParameters ??= new RtcpParameters();
            ValidateRtcpParameters(param.RtcpParameters);
        }

        public static void ValidateRtpCodecParameters(RtpCodecParameters codec)
        {
            if (codec == null)
            {
                throw new InvalidOleVariantTypeException("codec is not an object");
            }

            // mimeType is mandatory.
            if (string.IsNullOrEmpty(codec.mimeType))
            {
                throw new InvalidOleVariantTypeException("missing codec.mimeType");
            }

            Regex mimeTypeRegex = new Regex(@"^(audio|video)/(.+)", RegexOptions.ECMAScript | RegexOptions.IgnoreCase);

            var mimeTypeMatch = mimeTypeRegex.Matches(codec.mimeType);

            if (mimeTypeMatch.Count==0)
            {
                throw new ArgumentException("invalid codec.mimeType");
            }

            var kind = mimeTypeMatch[1].Value.ToLower().StartsWith("audio")
                ? MediaKind.audio
                : MediaKind.video;

            // channels is optional. If unset, set it to 1 (just if audio).

            if (kind is MediaKind.audio)
            {
                codec.channels = 1;
            }
            else
            {
                codec.channels = -1;
            }

            // parameters is optional. If unset, set it to an empty object.
            codec.parameters ??= new ExpandoObject();

            foreach (var (key, _) in codec.parameters)
            {
                object? value = codec.parameters[key];

                if (value == null)
                {
                    codec.parameters[key] = string.Empty;
                    value = string.Empty;
                }

                if (value is not (string or uint or int or ulong or long or byte or sbyte))
                {
                    throw new InvalidOleVariantTypeException($"invalid codec parameter[key:{key}, value:{value}]");
                }

                // Specific parameters validation.
                if (key == "apt")
                {
                    if (value is not (uint or int or ulong or long or byte or sbyte))
                    {
                        throw new InvalidOleVariantTypeException("invalid codec apt parameter");
                    }
                }
            }

            // rtcpFeedback is optional. If unset, set it to an empty array.
            codec.rtcpFeedback ??= new();

            foreach (var fb in codec.rtcpFeedback)
            {
                ValidateRtcpFeedback(fb);
            }
        }

        public static void ValidateRtpHeaderExtensionParameters(RtpHeaderExtensionParameters ext)
        {
            if (ext == null)
            {
                throw new ArgumentNullException(nameof(ext));
            }

            // uri is mandatory.
            if (string.IsNullOrEmpty(ext.uri))
            {
                throw new InvalidOleVariantTypeException($"missing ext.uri");
            }


            // encrypt is optional. If unset set it to false.
            ext.encrypt ??= false;

            // parameters is optional. If unset, set it to an empty object.
            ext.parameters ??= new ExpandoObject();

            foreach (var (key, _) in (ext.parameters as ExpandoObject)!)
            {
                var value = ext.parameters[key];

                if (value == null)
                {
                    ext.parameters[key] = string.Empty;
                    value = string.Empty;
                }

                if (value is not (uint or int or ulong or long or byte or sbyte))
                {
                    throw new InvalidOleVariantTypeException($"invalid codec parameter[key:{key}, value:{value}]");
                }
            }
        }

        public static void ValidateRtpEncodingParameters(RtpEncodingParameters encoding)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            // dtx is optional. If unset set it to false.
            encoding.dtx ??= false;
        }

        public static void ValidateRtcpParameters(RtcpParameters rtcp)
        {
            if (rtcp == null)
            {
                throw new ArgumentNullException(nameof(rtcp));
            }


            // reducedSize is optional. If unset set it to true.
            rtcp.reducedSize ??= true;
        }

        public static void ValidateSctpCapabilities(SctpCapabilities caps)
        {
            if (caps == null)
            {
                throw new ArgumentNullException(nameof(caps));
            }

            // numStreams is mandatory.
            if (caps.numStreams == null)
            {
                throw new InvalidOleVariantTypeException("missing caps.numStreams");
            }

            ValidateNumSctpStreams(caps.numStreams);
        }

        public static void ValidateNumSctpStreams(NumSctpStreams numStreams)
        {
            if (numStreams == null)
            {
                throw new ArgumentNullException(nameof(numStreams));
            }
        }

        public static void ValidateSctpParameters(SctpParameters param)
        {
            if (param == null)
            {
                throw new ArgumentNullException(nameof(param));
            }
        }

        public static void ValidateSctpStreamParameters(SctpStreamParameters param)
        {
            if (param == null)
            {
                throw new ArgumentNullException(nameof(param));
            }

            // ordered is optional.
            var orderedGiven = false;

            if (param.ordered.HasValue)
            {
                orderedGiven = true;
            }
            else
            {
                param.ordered = true;
            }


            if (param is { maxPacketLifeTime: not null, maxRetransmits: not null })
            {
                throw new InvalidOleVariantTypeException("cannot provide both maxPacketLifeTime and maxRetransmits");
            }

            param.ordered = orderedGiven switch
            {
                true when param.ordered.Value &&
                          (param.maxPacketLifeTime.HasValue || param.maxRetransmits.HasValue) =>
                    throw new InvalidOleVariantTypeException("cannot be ordered with maxPacketLifeTime or maxRetransmits"),
                false when param.maxPacketLifeTime.HasValue || param.maxRetransmits.HasValue
                    => false,
                _ => param.ordered
            };
        }

        public static void ValidateIceParameters()
        {
            
        }

        public static void ValidateIceCandidate()
        {

        }

        public static void ValidateIceCandidates()
        {

        }

        public static void ValidateDtlsFingerprint()
        {

        }

        public static void ValidateDtlsParameters()
        {

        }

        public static void ValidateProducerCodecOptions()
        {

        }

        public static string GetExtendedRtpCapabilities()
        {
            return null;
        }

        public static string GetRecvRtpCapabilities()
        {
            return null;
        }

        public static RtpParameters GetSendingRtpParameters(MediaKind kind,RtpCapabilities extendedRtpCapabilities)
        {
            RtpParameters rtp = new RtpParameters
            {
                mid = null,
                codecs = new List<RtpCodecParameters>(),
                encodings = new List<RtpEncodingParameters>(),
                headerExtensions = new List<RtpHeaderExtensionParameters>(),
                RtcpParameters = new RtcpParameters()
            };

            foreach (RtpCodecCapability extendedCodec in extendedRtpCapabilities.codecs)
            {
                
            }

            return null;

        }

        public static List<RtpCodecParameters> ReduceCodecs(List<RtpCodecParameters> codecs, RtpCodecCapability capCodec) 
        {
            return null;
        }

        public static RtpParameters GetSendingRemoteRtpParameters(MediaKind kind, RtpCapabilities extendedRtpCapabilities)
        {
            return null;
        }

        public static string GenerateProbatorRtpParameters()
        {
            return null;
        }

        public static bool CanSend()
        {
            return false;
        }

        public static bool CanReceive()
        {
            return false;
        }

        public static bool isRtxCodec() 
        {
            return false;
        }

        public static bool MatchCodecs()
        {
            return false;
        }
        public static bool MatchHeaderExtensions()
        {
            return false;
        }
        public static string ReduceRtcpFeedback()
        {
            return null;
        }
        public static byte GetH264PacketizationMode()
        {
            return byte.MaxValue;
        }
        public static byte GetH264LevelAssimetryAllowed()
        {
            return byte.MaxValue;
        }
        public static string GetH264ProfileLevelId()
        {
            return null;
        }
        public static string GetVP9ProfileId()
        {
            return null;
        }


    }

    public static class OrtcUtils 
    {
        public static void AddNackSuppportForOpus(RtpCapabilities rtpCapabilities) 
        {
            foreach (var codec in rtpCapabilities.codecs) 
            {
                if ((codec.mimeType.ToLower() == "audio/opus" || codec.mimeType.ToLower() == "audio/multiopus") && 
                    (codec.rtcpFeedback?.Any(fb => fb.type == "nack" && fb.parameters == null) == true)) 
                {
                    if (codec.rtcpFeedback!=null) 
                    {
                        codec.rtcpFeedback = new List<RtcpFeedback>();
                    }


                    RtcpFeedback nackSupp = new RtcpFeedback {type = "nack",parameters =  "" };
                    codec.rtcpFeedback.Add(nackSupp);
                }
            }
        }
    }
}
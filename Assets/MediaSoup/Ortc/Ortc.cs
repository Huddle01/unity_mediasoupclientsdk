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
using UnityEditor.PackageManager;
using h264_profile_level_id;
using System.Drawing.Text;
using System.Security.Cryptography;
using Unity.VisualScripting;

namespace Mediasoup.Ortc
{
    public class Ortc
    {
        static h264_profile_level_id.H264PRofileLevelId h264 = new H264PRofileLevelId();

        const string RTP_PROBATOR_MID = "probator";
        const int RTP_PROBATOR_SSRC = 1234;
        const int RTP_PROBATOR_CODEC_PAYLOAD_TYPE = 127;

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
            if (codec == null)
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

            string mimeType = mimeTypeRegex.Match(codec.mimeType).Value;

            // Just override kind with media component of mimeType.
            codec.kind = mimeType == "video" ? MediaKind.video : MediaKind.audio;

            // preferredPayloadType is optional, no check required

            // clockRate is mandatory.
            if (codec.clockRate == null) {
                throw new ArgumentNullException("Clock rate is missing");
            }

            // channels is optional. If unset, set it to 1 (just if audio).
            if (codec.kind == MediaKind.audio)
            {
                if (codec.channels == null) {
                    codec.channels = 1;
                }
            }
            else { 
                codec.channels = null;
            }

            // parameters is optional. If unset, set it to an empty object.
            if (codec.parameters == null)
                codec.parameters = new Dictionary<string, string>();

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
                    if (value is not string)
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
            param.rtcpParameters ??= new RtcpParameters();
            ValidateRtcpParameters(param.rtcpParameters);
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

            if (mimeTypeMatch.Count == 0)
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
            codec.parameters ??= new Dictionary<string, string>();

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

        /// <summary>
        ///     Validates Rtcp Parameters
        /// </summary>
        /// <param name="rtcp"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void ValidateRtcpParameters(RtcpParameters rtcpParameters)
        {
            if (rtcpParameters == null)
            {
                throw new ArgumentNullException(nameof(rtcpParameters));
            }

            // cname is optional

            // reducedSize is optional. If unset set it to true.
            rtcpParameters.reducedSize ??= true;
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

        public static ExtendedRtpCapabilities GetExtendedRtpCapabilities(RtpCapabilities localCaps, RtpCapabilities remoteCaps)
        {
            ExtendedRtpCapabilities extendedRtpCapabilities = new ExtendedRtpCapabilities();

            foreach (RtpCodecCapability remoteCodec in remoteCaps.codecs)
            {
                if (IsRtxCodec(remoteCodec))
                {
                    continue;
                }

                RtpCodecCapability matchingLocalCodec = null;
                if (localCaps.codecs != null)
                {
                    foreach (RtpCodecCapability localCodec in localCaps.codecs)
                    {
                        if (MatchCodecs(localCodec, remoteCodec, true, true))
                        {
                            matchingLocalCodec = localCodec;
                            break;
                        }
                    }
                }

                if (matchingLocalCodec == null)
                {
                    continue;
                }

                ExtendedRtpCodecCapability extendedCodec = new ExtendedRtpCodecCapability
                {
                    mimeType = matchingLocalCodec.mimeType,
                    kind = matchingLocalCodec.kind.Value,
                    clockRate = matchingLocalCodec.clockRate,
                    channels = matchingLocalCodec.channels,
                    localPayloadType = matchingLocalCodec.preferredPayloadType,
                    remotePayloadType = remoteCodec.preferredPayloadType,
                    localParameters = matchingLocalCodec.parameters,
                    remoteParameters = remoteCodec.parameters,
                    rtcpFeedback = ReduceRtcpFeedback(matchingLocalCodec, remoteCodec),
                };

                extendedRtpCapabilities.codecs.Add(extendedCodec);
            }

            foreach (ExtendedRtpCodecCapability extendedCodec in extendedRtpCapabilities.codecs)
            {
                {

                    RtpCodecCapability matchingLocalRtxCodec = null;
                    foreach (RtpCodecCapability localCodec in localCaps.codecs)
                    {
                        if (IsRtxCodec(localCodec) && Int32.Parse(localCodec.parameters["apt"]) == extendedCodec.localPayloadType)
                        {
                            matchingLocalRtxCodec = localCodec;
                            break;
                        }
                    }

                    RtpCodecCapability matchingRemoteRtxCodec = remoteCaps.codecs.First(codec => IsRtxCodec(codec)
                    && Int32.Parse(codec.parameters["apt"]) == extendedCodec.localPayloadType);

                    if (matchingLocalRtxCodec != null && matchingRemoteRtxCodec != null)
                    {
                        extendedCodec.localRtxPayloadType = matchingLocalRtxCodec.preferredPayloadType;
                        extendedCodec.remoteRtxPayloadType = matchingRemoteRtxCodec.preferredPayloadType;
                    }
                }
            }

            foreach (RtpHeaderExtension remoteExt in remoteCaps.headerExtensions)
            {
                RtpHeaderExtension matchingLocalExt = localCaps.headerExtensions.First(ext => matchHeaderExtensions(ext, remoteExt));

                if (matchingLocalExt == null) continue;

                ExtendedRtpHeaderExtension extendedExt = new ExtendedRtpHeaderExtension
                {
                    kind = remoteExt.kind,
                    uri = remoteExt.uri,
                    sendId = matchingLocalExt.preferredId,
                    recvId = remoteExt.preferredId,
                    preferredEncrypt = matchingLocalExt.preferredEncrypt,
                    direction = RtpHeaderExtensionDirection.sendrecv
                };

                switch (remoteExt.direction)
                {

                    case (RtpHeaderExtensionDirection.sendrecv):
                        {
                            extendedExt.direction = RtpHeaderExtensionDirection.sendrecv;
                            break;
                        }
                    case (RtpHeaderExtensionDirection.recvonly):
                        {
                            extendedExt.direction = RtpHeaderExtensionDirection.sendonly;
                            break;
                        }
                    case (RtpHeaderExtensionDirection.sendonly):
                        {
                            extendedExt.direction = RtpHeaderExtensionDirection.recvonly;
                            break;
                        }
                    case (RtpHeaderExtensionDirection.inactive):
                        {
                            extendedExt.direction = RtpHeaderExtensionDirection.inactive;
                            break;
                        }
                }

                extendedRtpCapabilities.headerExtensions.Add(extendedExt);

            }


            return extendedRtpCapabilities;
        }

        private static bool matchHeaderExtensions(RtpHeaderExtension a, RtpHeaderExtension b)
        {
            if (a.kind != null && b.kind != null && a.kind != b.kind)
            {
                return false;
            }

            if (a.uri != b.uri) return false;

            return true;

        }

        public static RtpCapabilities GetRecvRtpCapabilities(ExtendedRtpCapabilities extendedRtpCapabilities)
        {
            RtpCapabilities rtpCapabilities = new RtpCapabilities();

            foreach (ExtendedRtpCodecCapability extendedCodec in extendedRtpCapabilities.codecs)
            {
                RtpCodecCapability codec = new RtpCodecCapability
                {
                    mimeType = extendedCodec.mimeType,
                    kind = extendedCodec.kind,
                    preferredPayloadType = extendedCodec.remotePayloadType,
                    clockRate = extendedCodec.clockRate,
                    channels = extendedCodec.channels,
                    parameters = extendedCodec.localParameters,
                    rtcpFeedback = extendedCodec.rtcpFeedback
                };

                rtpCapabilities.codecs.Add(codec);

                if (extendedCodec.remoteRtxPayloadType == null)
                {
                    continue;
                }

                RtpCodecCapability rtxCodec = new RtpCodecCapability
                {
                    mimeType = $"{extendedCodec.kind}/rtx",
                    kind = extendedCodec.kind,
                    preferredPayloadType = extendedCodec.remoteRtxPayloadType.Value,
                    clockRate = extendedCodec.clockRate,
                    parameters = new Dictionary<string, string> { { "apt", extendedCodec.remotePayloadType.ToString() } },
                    rtcpFeedback = new List<RtcpFeedback>()
                };

                rtpCapabilities.codecs.Add(rtxCodec);
            }

            foreach (var extendedExt in extendedRtpCapabilities.headerExtensions)
            {

                if (extendedExt.direction != RtpHeaderExtensionDirection.sendrecv &&
                      extendedExt.direction != RtpHeaderExtensionDirection.recvonly)
                {
                    continue;
                }

                RtpHeaderExtension ext = new RtpHeaderExtension
                {
                    kind = extendedExt.kind,
                    uri = extendedExt.uri,
                    preferredId = extendedExt.recvId,
                    preferredEncrypt = extendedExt.preferredEncrypt,
                    direction = extendedExt.direction
                };

                rtpCapabilities.headerExtensions.Add(ext);
            }

            return rtpCapabilities;
        }

        public static RtpParameters GetSendingRtpParameters(MediaKind kind, ExtendedRtpCapabilities extendedRtpCapabilities)
        {
            RtpParameters RtpParameters = new RtpParameters
            {
                mid = null,
                codecs = new List<RtpCodecParameters>(),
                headerExtensions = new List<RtpHeaderExtensionParameters>(),
                encodings = new List<RtpEncodingParameters>(),
                rtcpParameters = new RtcpParameters()
            };

            foreach (ExtendedRtpCodecCapability extendedCodec in extendedRtpCapabilities.codecs)
            {
                if (extendedCodec.kind != kind)
                {
                    continue;
                }

                RtpCodecParameters codec = new RtpCodecParameters
                {
                    mimeType = extendedCodec.mimeType,
                    payloadType = extendedCodec.localPayloadType,
                    clockRate = extendedCodec.clockRate,
                    channels = extendedCodec.channels,
                    parameters = extendedCodec.localParameters,
                    rtcpFeedback = extendedCodec.rtcpFeedback
                };

                RtpParameters.codecs.Add(codec);

                if (extendedCodec.localRtxPayloadType != null)
                {
                    RtpCodecParameters rtcCodec = new RtpCodecParameters
                    {
                        mimeType = $"{extendedCodec.kind}/rtx",
                        payloadType = extendedCodec.localRtxPayloadType.Value,
                        clockRate = extendedCodec.clockRate,
                        parameters = new Dictionary<string, string> { { "apt", extendedCodec.localPayloadType.ToString() } },
                        rtcpFeedback = new List<RtcpFeedback>()
                    };

                    RtpParameters.codecs.Add(rtcCodec);
                }
            }

            foreach (ExtendedRtpHeaderExtension extendedExt in extendedRtpCapabilities.headerExtensions)
            {
                if ((extendedExt.kind != null && extendedExt.kind != kind) ||
                    (extendedExt.direction != RtpHeaderExtensionDirection.sendonly
                    && extendedExt.direction != RtpHeaderExtensionDirection.sendrecv))
                {
                    continue;
                }

                RtpHeaderExtensionParameters ext = new RtpHeaderExtensionParameters
                {
                    uri = extendedExt.uri,
                    id = extendedExt.sendId,
                    encrypt = extendedExt.preferredEncrypt,
                    parameters = new ExpandoObject()
                };

                RtpParameters.headerExtensions.Add(ext);
            }

            return RtpParameters;
        }

        public static RtpParameters GetSendingRemoteRtpParameters(MediaKind kind, ExtendedRtpCapabilities extendedRtpCapabilities)
        {
            RtpParameters rtpParameters = new RtpParameters
            {
                mid = null,
                codecs = new List<RtpCodecParameters>(),
                headerExtensions = new List<RtpHeaderExtensionParameters>(),
                encodings = new List<RtpEncodingParameters>(),
                rtcpParameters = new RtcpParameters()
            };

            foreach (ExtendedRtpCodecCapability extendedCodec in extendedRtpCapabilities.codecs)
            {
                if (extendedCodec.kind != kind)
                {
                    continue;
                }

                RtpCodecParameters codec = new RtpCodecParameters
                {
                    mimeType = extendedCodec.mimeType,
                    preferredPayloadType = extendedCodec.localPayloadType,
                    clockRate = extendedCodec.clockRate,
                    channels = extendedCodec.channels,
                    parameters = extendedCodec.remoteParameters,
                    rtcpFeedback = extendedCodec.rtcpFeedback
                };

                rtpParameters.codecs.Add(codec);

                if (extendedCodec.remoteRtxPayloadType != null)
                {
                    RtpCodecParameters rtxCodec = new RtpCodecParameters
                    {
                        mimeType = $"{extendedCodec.kind}/rtx",
                        preferredPayloadType = extendedCodec.remoteRtxPayloadType.Value,
                        clockRate = extendedCodec.clockRate,
                        parameters = new Dictionary<string, string> { { "apt", extendedCodec.remotePayloadType.ToString() } },
                        rtcpFeedback = new List<RtcpFeedback>()
                    };

                    rtpParameters.codecs.Add(rtxCodec);
                }
            }

            foreach (ExtendedRtpHeaderExtension extendedExt in extendedRtpCapabilities.headerExtensions)
            {
                if ((extendedExt.kind != null && extendedExt.kind != kind) ||
                    (extendedExt.direction != RtpHeaderExtensionDirection.sendonly
                    && extendedExt.direction != RtpHeaderExtensionDirection.sendrecv))
                {
                    continue;
                }

                RtpHeaderExtensionParameters ext = new RtpHeaderExtensionParameters
                {
                    uri = extendedExt.uri,
                    id = extendedExt.sendId,
                    encrypt = extendedExt.preferredEncrypt,
                    parameters = new ExpandoObject()
                };

                rtpParameters.headerExtensions.Add(ext);
            }

            if (rtpParameters.headerExtensions.Exists(ext => ext.uri == "http://www.ietf.org/id/draft-holmer-rmcat-transport-wide-cc-extensions-01"))
            {
                foreach (RtpCodecCapability codec in rtpParameters.codecs)
                {
                    codec.rtcpFeedback = codec.rtcpFeedback.FindAll(fb => fb.type == "goog-remb");
                }
            }
            else if (
                rtpParameters.headerExtensions!.Exists(
                    ext =>
                        ext.uri == "http://www.webrtc.org/experiments/rtp-hdrext/abs-send-time"
                )
            )
            {
                foreach (RtpCodecCapability codec in rtpParameters.codecs)
                {
                    codec.rtcpFeedback = codec.rtcpFeedback.FindAll(
                        fb => fb.type != "transport-cc"
                    );
                }
            }
            else
            {
                foreach (RtpCodecCapability codec in rtpParameters.codecs)
                {
                    codec.rtcpFeedback = codec.rtcpFeedback.FindAll(
                        fb =>

                            fb.type != "transport-cc" && fb.type != "goog-remb"
                    );
                }
            }

            return rtpParameters;
        }

        public static RtpParameters GenerateProbatorRtpParameters(RtpParameters videoRtpParameters)
        {
            RtpParameters copyRtpParameters = (RtpParameters)videoRtpParameters.Clone();

            ValidateRtpParameters(copyRtpParameters);

            RtpParameters rtpParameters = new RtpParameters
            {
                mid = RTP_PROBATOR_MID,
                codecs = new List<RtpCodecParameters>(),
                headerExtensions = new List<RtpHeaderExtensionParameters>(),
                encodings = new List<RtpEncodingParameters>(),
                rtcpParameters = new RtcpParameters
                {
                    cname = "probator"
                }
            };

            rtpParameters.codecs.Add(copyRtpParameters.codecs[0]);
            rtpParameters.codecs[0].payloadType = RTP_PROBATOR_CODEC_PAYLOAD_TYPE;
            rtpParameters.headerExtensions = copyRtpParameters.headerExtensions;

            return rtpParameters;
        }

        public static bool CanSend(MediaKind kind, ExtendedRtpCapabilities rtpCapabilities)
        {
            return rtpCapabilities.codecs.Exists(codec => codec.kind == kind);
        }

        public static bool CanReceive(RtpParameters rtpParameters, ExtendedRtpCapabilities extendedRtpCapabilities)
        {
            ValidateRtpParameters(rtpParameters);

            if (rtpParameters.codecs.Count == 0)
            {
                return false;
            }

            RtpCodecParameters rtpCodecCapability = rtpParameters.codecs[0];

            return extendedRtpCapabilities.codecs.Exists(codec => codec.remotePayloadType == rtpCodecCapability.preferredPayloadType);
        }

        public static bool IsRtxCodec(RtpCodecCapability codec)
        {
            if (codec == null)
            {
                return false;
            }

            return Regex.IsMatch(codec.mimeType ?? "", @".+\/rtx$", RegexOptions.IgnoreCase);
        }

        public static bool MatchCodecs(RtpCodecCapability aCodec, RtpCodecCapability bCodec, bool strict = false, bool modify = false)
        {
            string aMimeType = aCodec.mimeType.ToLower();
            string bMimeType = bCodec.mimeType.ToLower();

            if (aMimeType != bMimeType)
            {
                return false;
            }

            if (aCodec.clockRate != bCodec.clockRate)
            {
                return false;
            }

            if (aCodec.channels != bCodec.channels)
            {
                return false;
            }

            switch (aMimeType)
            {
                case "video/h264":
                    {
                        if (strict)
                        {
                            object aPacketizationMode = aCodec.parameters["packetization-mode"];
                            object bPacketizationMode = bCodec.parameters["packetization-mode"];

                            if (aPacketizationMode != bPacketizationMode)
                            {
                                return false;
                            }



                            if (!h264.isSameProfile(aCodec.parameters, bCodec.parameters))
                            {
                                return false;
                            }

                            string selectedProfileLevelId;

                            try
                            {
                                selectedProfileLevelId = h264.generateProfileLevelIdStringForAnswer(
                                   aCodec.parameters,
                                   bCodec.parameters
                               );
                            }
                            catch (Exception err)
                            {
                                Console.WriteLine("Error generating profile-level-id string for answer: " + err.Message);
                                return false;
                            }

                            if (modify)
                            {
                                if (selectedProfileLevelId != null)
                                {
                                    aCodec.parameters["profile-level-id"] = selectedProfileLevelId;
                                    bCodec.parameters["profile-level-id"] = selectedProfileLevelId;
                                }
                                else
                                {
                                    if (aCodec.parameters.ContainsKey("profile-level-id"))
                                    {
                                        aCodec.parameters.Remove("profile-level-id");
                                    }

                                    if (bCodec.parameters.ContainsKey("profile-level-id"))
                                    {
                                        bCodec.parameters.Remove("profile-level-id");
                                    }
                                }
                            }
                        }

                        break;
                    }

                case "video/vp9":
                    {
                        if (strict)
                        {
                            string aProfileId = aCodec.parameters["profile-id"];
                            string bProfileId = bCodec.parameters["profile-id"];

                            if (aProfileId != bProfileId)
                            {
                                return false;
                            }
                        }

                        break;
                    }
            }

            return true;
        }

        public static bool MatchHeaderExtensions()
        {
            return false;
        }
        public static List<RtcpFeedback> ReduceRtcpFeedback(RtpCodecCapability codecA, RtpCodecCapability codecB)
        {
            List<RtcpFeedback> reducedRtcpFeedback = new List<RtcpFeedback>();

            foreach (RtcpFeedback aFb in codecA.rtcpFeedback)
            {
                foreach (RtcpFeedback bFb in codecB.rtcpFeedback)
                {
                    if (aFb.type == bFb.type)
                    {
                        if (aFb.parameters == bFb.parameters || (aFb.parameters == null && bFb.parameters == null))
                        {
                            reducedRtcpFeedback.Add(aFb);
                            break;
                        }
                    }
                }
            }

            return reducedRtcpFeedback;
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
                    if (codec.rtcpFeedback != null)
                    {
                        codec.rtcpFeedback = new List<RtcpFeedback>();
                    }


                    RtcpFeedback nackSupp = new RtcpFeedback { type = "nack", parameters = "" };
                    codec.rtcpFeedback.Add(nackSupp);
                }
            }
        }
    }
}
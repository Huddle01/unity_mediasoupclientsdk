using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text.RegularExpressions;
using Mediasoup.RtpParameter;
using Mediasoup.SctpParameter;
using Mediasoup.Transports;
using Unity.WebRTC;
using Utilme.SdpTransform;
using UnityEngine;

namespace Mediasoup.Ortc
{
    public static class ORTC
    {
        private static readonly Regex MimeTypeRegex = new("^(audio|video)/(.+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RtxMimeTypeRegex = new("^.+/rtx$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ProtocolRegex = new Regex("(udp|tcp)", RegexOptions.ECMAScript |  RegexOptions.IgnoreCase);
        private static readonly Regex TypeRegex = new Regex("(host|srflx|prflx|relay)", RegexOptions.ECMAScript |  RegexOptions.IgnoreCase);
        private static readonly Regex RoleRegex = new Regex("(auto|client|server)", RegexOptions.ECMAScript | RegexOptions.IgnoreCase);


        public static readonly byte[] DynamicPayloadTypes = new byte[] {
            100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110,
            111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121,
            122, 123, 124, 125, 126, 127, 96, 97, 98, 99 };

        /// <summary>
        /// Validates RtpCapabilities. It may modify given data by adding missing
        /// fields with default values.
        /// It throws if invalid.
        /// </summary>
        public static void ValidateRtpCapabilities(RtpCapabilities caps)
        {
            if (caps == null)
            {
                throw new ArgumentNullException(nameof(caps));
            }

            caps.Codecs ??= new List<RtpCodecCapability>();

            foreach (var codec in caps.Codecs)
            {
                ValidateRtpCodecCapability(codec);
            }

            // headerExtensions is optional. If unset, fill with an empty array.
            caps.HeaderExtensions ??= new List<RtpHeaderExtension>();

            foreach (var ext in caps.HeaderExtensions)
            {
                ValidateRtpHeaderExtension(ext);
            }
        }

        /// <summary>
        /// Validates RtpCodecCapability. It may modify given data by adding missing
        /// fields with default values.
        /// It throws if invalid.
        /// </summary>
        public static void ValidateRtpCodecCapability(RtpCodecCapability codec)
        {
            // mimeType is mandatory.
            if (codec.MimeType.IsNullOrWhiteSpace())
            {
                throw new ArgumentException($"{nameof(codec.MimeType)} can't be null or white space.");
            }

            var mimeType = codec.MimeType.ToLower();

            if (!MimeTypeRegex.IsMatch(mimeType))
            {
                throw new ArgumentException($"{nameof(codec.MimeType)} is not matched.");
            }

            // Just override kind with media component in mimeType.
            codec.Kind = mimeType.StartsWith("video") ? MediaKind.VIDEO : MediaKind.AUDIO;

            // preferredPayloadType is optional.

            // clockRate is mandatory.

            // channels is optional. If unset, set it to 1 (just if audio).
            if (codec.Kind == MediaKind.AUDIO && (!codec.Channels.HasValue || codec.Channels < 1))
            {
                codec.Channels = 1;
            }

            // parameters is optional. If unset, set it to an empty object.
            codec.Parameters ??= new Dictionary<string, object>();

            foreach (var (key, val) in codec.Parameters)
            {
                var value = val;

                if (value == null)
                {
                    codec.Parameters[key] = string.Empty;
                    value = string.Empty;
                }

                if (value is not (string or uint or int or ulong or long or byte or sbyte))
                {
                    throw new ArgumentNullException($"invalid codec parameter [key:{key}, value:{value}]");
                }

                // Specific parameters validation.
                if (key == "apt")
                {
                    UnityEngine.Debug.Log($"Key in codec params {key}");
                    UnityEngine.Debug.Log($"Key in codec params {val}");
                    UnityEngine.Debug.Log($"Type of value is {value.GetType()}");

                    ExtractApt(value);
                }
            }

            // rtcpFeedback is optional. If unset, set it to an empty array.
            codec.RtcpFeedback ??= new List<RtcpFeedback>(0);

            foreach (var fb in codec.RtcpFeedback)
            {
                ValidateRtcpFeedback(fb);
            }
        }

        public static bool CanSend(MediaKind kind, ExtendedRtpCapabilities rtpCapabilities)
        {
            return rtpCapabilities.codecs.Exists(codec => codec.kind == kind);
        }

        /// <summary>
        /// Validates RtcpFeedback. It may modify given data by adding missing
        /// fields with default values.
        /// It throws if invalid.
        /// </summary>
        public static void ValidateRtcpFeedback(RtcpFeedback fb)
        {
            if (fb == null)
            {
                throw new ArgumentNullException(nameof(fb));
            }

            // type is mandatory.
            if (fb.Type.IsNullOrWhiteSpace())
            {
                throw new ArgumentException(nameof(fb.Type));
            }

            // parameter is optional. If unset set it to an empty string.
            if (fb.Parameter.IsNullOrWhiteSpace())
            {
                fb.Parameter = "";
            }
        }

        /// <summary>
        /// Validates RtpHeaderExtension. It may modify given data by adding missing
        /// fields with default values.
        /// It throws if invalid.
        /// </summary>
        public static void ValidateRtpHeaderExtension(RtpHeaderExtension ext)
        {
            if (ext == null)
            {
                throw new ArgumentNullException(nameof(ext));
            }


            // uri is mandatory.
            // if(ext.Uri.IsNullOrWhiteSpace())
            // {
            //     throw new ArgumentException($"{nameof(ext.Uri)} can't be null or white space.");
            // }

            // preferredId is mandatory.

            // preferredEncrypt is optional. If unset set it to false.
            // if(!ext.PreferredEncrypt.HasValue)
            // {
            //     ext.PreferredEncrypt = false;
            // }

            // direction is optional. If unset set it to SendReceive.
            if (!ext.Direction.HasValue)
            {
                ext.Direction = RtpHeaderExtensionDirection.SendReceive;
            }
        }

        /// <summary>
        /// Validates RtpParameters. It may modify given data by adding missing
        /// fields with default values.
        /// It throws if invalid.
        /// </summary>
        public static void ValidateRtpParameters(RtpParameters parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }
            // mid is optional.

            // codecs is mandatory.
            if (parameters.Codecs == null)
            {
                throw new ArgumentNullException($"{nameof(parameters)}.Codecs");
            }

            foreach (var codec in parameters.Codecs)
            {
                ValidateRtpCodecParameters(codec);
            }

            // headerExtensions is optional. If unset, fill with an empty array.
            parameters.HeaderExtensions ??= new List<RtpHeaderExtensionParameters>();

            foreach (var ext in parameters.HeaderExtensions)
            {
                ValidateRtpHeaderExtensionParameters(ext);
            }

            // encodings is optional. If unset, fill with an empty array.
            // parameters.Encodings ??= new List<RtpEncodingParameters>();

            foreach (var encoding in parameters.Encodings)
            {
                ValidateRtpEncodingParameters(encoding);
            }

            // rtcp is optional. If unset, fill with an empty object.
            // parameters.Rtcp ??= new RtcpParameters();
            ValidateRtcpParameters(parameters.Rtcp);
        }

        /// <summary>
        /// Validates RtpCodecParameters. It may modify given data by adding missing
        /// fields with default values.
        /// It throws if invalid.
        /// </summary>
        public static void ValidateRtpCodecParameters(RtpCodecParameters codec)
        {
            if (codec == null)
            {
                throw new ArgumentNullException(nameof(codec));
            }

            // mimeType is mandatory.
            if (codec.MimeType.IsNullOrWhiteSpace())
            {
                throw new ArgumentException($"{nameof(codec.MimeType)} can't be null or white space.");
            }

            var mimeType = codec.MimeType.ToLower();
            if (!MimeTypeRegex.IsMatch(mimeType))
            {
                throw new ArgumentException($"{nameof(codec.MimeType)} is not matched.");
            }

            // payloadType is mandatory.

            // clockRate is mandatory.

            // channels is optional. If unset, set it to 1 (just if audio).
            if (mimeType.StartsWith("audio") && (!codec.Channels.HasValue || codec.Channels < 1))
            {
                codec.Channels = 1;
            }

            // parameters is optional. If unset, set it to an empty object.
            codec.Parameters ??= new Dictionary<string, object>();

            foreach (var item in codec.Parameters)
            {
                var key = item.Key;
                var value = item.Value;
                if (value == null)
                {
                    codec.Parameters[item.Key] = "";
                    value = "";
                }

                if (!value.IsStringType() && !value.IsNumericType())
                {
                    throw new ArgumentOutOfRangeException($"invalid codec parameter [key:{key}, value:{value}]");
                }

                if (key == "apt" && !value.IsNumericType())
                {
                    throw new ArgumentOutOfRangeException($"invalid codec apt parameter [key:{key}]");
                }
            }

            // rtcpFeedback is optional. If unset, set it to an empty array.
            // codec.RtcpFeedback ??= new List<RtcpFeedback>(0);

            foreach (var fb in codec.RtcpFeedback)
            {
                ValidateRtcpFeedback(fb);
            }
        }

        /// <summary>
        /// Validates RtpHeaderExtensionParameteters. It may modify given data by adding
        /// missing fields with default values. It throws if invalid.
        /// </summary>
        public static void ValidateRtpHeaderExtensionParameters(RtpHeaderExtensionParameters ext)
        {
            if (ext == null)
            {
                throw new ArgumentNullException(nameof(ext));
            }

            // uri is mandatory.
            // if(ext.Uri.IsNullOrWhiteSpace())
            // {
            //     throw new ArgumentException($"{nameof(ext.Uri)} can't be null or white space.");
            // }

            // id is mandatory.

            // encrypt is optional. If unset set it to false.
            // if(!ext.Encrypt.HasValue)
            // {
            //     ext.Encrypt = false;
            // }

            // parameters is optional. If unset, set it to an empty object.
            ext.Parameters ??= new Dictionary<string, object>();

            foreach (var item in ext.Parameters)
            {
                var key = item.Key;
                var value = item.Value;

                if (value == null)
                {
                    ext.Parameters[item.Key] = "";
                    value = "";
                }

                if (!value.IsStringType() && !value.IsNumericType())
                {
                    throw new ArgumentOutOfRangeException($"invalid codec parameter[key:{key}, value:{value}]");
                }
            }
        }

        /// <summary>
        /// Validates RtpEncodingParameters. It may modify given data by adding missing
        /// fields with default values.
        /// It throws if invalid.
        /// </summary>
        public static void ValidateRtpEncodingParameters(RtpEncodingParameters encoding)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            // ssrc is optional.

            // rid is optional.

            // rtx is optional.
            if (encoding.Rtx != null)
            {
                // RTX ssrc is mandatory if rtx is present.
            }

            // dtx is optional. If unset set it to false.
            // if(!encoding.Dtx.HasValue)
            // {
            //     encoding.Dtx = false;
            // }

            // scalabilityMode is optional.
        }

        /// <summary>
        /// Validates RtcpParameters. It may modify given data by adding missing
        /// fields with default values.
        /// It throws if invalid.
        /// </summary>
        public static void ValidateRtcpParameters(RtcpParameters rtcp)
        {
            if (rtcp == null)
            {
                throw new ArgumentNullException(nameof(rtcp));
            }

            // cname is optional.

            // reducedSize is optional. If unset set it to true.
            // if(!rtcp.ReducedSize.HasValue)
            // {
            //     rtcp.ReducedSize = true;
            // }
        }

        /// <summary>
        /// Validates SctpCapabilities. It may modify given data by adding missing
        /// fields with default values.
        /// It throws if invalid.
        /// </summary>
        public static void ValidateSctpCapabilities(SctpCapabilities caps)
        {
            if (caps == null)
            {
                throw new ArgumentNullException(nameof(caps));
            }

            // numStreams is mandatory.
            if (caps.numStreams == null)
            {
                throw new ArgumentNullException($"{nameof(caps)}.NumStreams");
            }

            ValidateNumSctpStreams(caps.numStreams);
        }

        /// <summary>
        /// Validates NumSctpStreams. It may modify given data by adding missing
        /// fields with default values.
        /// It throws if invalid.
        /// </summary>
        public static void ValidateNumSctpStreams(NumSctpStreams _/*numStreams*/)
        {
            // OS is mandatory.

            // MIS is mandatory.
        }

        /// <summary>
        /// Validates SctpParameters. It may modify given data by adding missing
        /// fields with default values.
        /// It throws if invalid.
        /// </summary>
        public static void ValidateSctpParameters(SctpParameters _/*parameters*/)
        {
            // port is mandatory.

            // OS is mandatory.

            // MIS is mandatory.

            // maxMessageSize is mandatory.
        }

        /// <summary>
        /// Validates SctpStreamParameters. It may modify given data by adding missing
        /// fields with default values.
        /// It throws if invalid.
        /// </summary>
        public static void ValidateSctpStreamParameters(SctpStreamParameters parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            // streamId is mandatory.

            // ordered is optional.
            var orderedGiven = true;
            if (!parameters.ordered.HasValue)
            {
                orderedGiven = false;
                parameters.ordered = true;
            }

            // maxPacketLifeTime is optional.

            // maxRetransmits is optional.

            if (parameters.maxPacketLifeTime.HasValue && parameters.maxRetransmits.HasValue)
            {
                throw new ArgumentException("cannot provide both maxPacketLifeTime and maxRetransmits");
            }

            if (orderedGiven &&
                parameters.ordered.HasValue && parameters.ordered.Value &&
                (parameters.maxPacketLifeTime.HasValue || parameters.maxRetransmits.HasValue)
                )
            {
                throw new ArgumentException("cannot be ordered with maxPacketLifeTime or maxRetransmits");
            }
            else if (!orderedGiven && (parameters.maxPacketLifeTime.HasValue || parameters.maxRetransmits.HasValue))
            {
                parameters.ordered = false;
            }
        }

        /// <summary>
        /// Generate RTP capabilities for the Router based on the given media codecs and
        /// mediasoup supported RTP capabilities.
        /// </summary>
        public static RtpCapabilities GenerateRouterRtpCapabilities(RtpCodecCapability[] mediaCodecs)
        {
            if (mediaCodecs == null)
            {
                throw new ArgumentNullException(nameof(mediaCodecs));
            }

            // Normalize supported RTP capabilities.
            ValidateRtpCapabilities(RtpCapabilities.SupportedRtpCapabilities);

            var clonedSupportedRtpCapabilities = Utils.Clone(RtpCapabilities.SupportedRtpCapabilities);
            var dynamicPayloadTypes = Utils.Clone(DynamicPayloadTypes).ToList();
            var caps = new RtpCapabilities
            {
                Codecs = new List<RtpCodecCapability>(),
                HeaderExtensions = clonedSupportedRtpCapabilities.HeaderExtensions
            };

            foreach (var mediaCodec in mediaCodecs)
            {
                // This may throw.
                ValidateRtpCodecCapability(mediaCodec);

                var matchedSupportedCodec = clonedSupportedRtpCapabilities
                    .Codecs!
                    .FirstOrDefault(supportedCodec => MatchCodecs(mediaCodec, supportedCodec, false))
                    ?? throw new Exception($"media codec not supported[mimeType:{mediaCodec.MimeType}]");

                // Clone the supported codec.
                var codec = Utils.Clone(matchedSupportedCodec);

                // If the given media codec has preferredPayloadType, keep it.
                if (mediaCodec.PreferredPayloadType.HasValue)
                {
                    codec.PreferredPayloadType = mediaCodec.PreferredPayloadType;

                    // Also remove the pt from the list in available dynamic values.
                    dynamicPayloadTypes.Remove(codec.PreferredPayloadType.Value);
                }
                // Otherwise if the supported codec has preferredPayloadType, use it.
                else if (codec.PreferredPayloadType.HasValue)
                {
                    // No need to remove it from the list since it's not a dynamic value.
                }
                // Otherwise choose a dynamic one.
                else
                {
                    // Take the first available pt and remove it from the list.
                    var pt = dynamicPayloadTypes.FirstOrDefault();

                    if (pt == 0)
                    {
                        throw new Exception("cannot allocate more dynamic codec payload types");
                    }

                    dynamicPayloadTypes.RemoveAt(0);

                    codec.PreferredPayloadType = pt;
                }

                // Ensure there is not duplicated preferredPayloadType values.
                if (caps.Codecs.Any(c => c.PreferredPayloadType == codec.PreferredPayloadType))
                {
                    throw new Exception("duplicated codec.preferredPayloadType");
                }

                // Merge the media codec parameters.
                codec.Parameters = codec.Parameters!.Merge(mediaCodec.Parameters!);

                // Append to the codec list.
                caps.Codecs.Add(codec);

                // Add a RTX video codec if video.
                if (codec.Kind == MediaKind.VIDEO)
                {
                    // Take the first available pt and remove it from the list.
                    var pt = dynamicPayloadTypes.FirstOrDefault();

                    if (pt == 0)
                    {
                        throw new Exception("cannot allocate more dynamic codec payload types");
                    }

                    dynamicPayloadTypes.RemoveAt(0);

                    var rtxCodec = new RtpCodecCapability
                    {
                        Kind = codec.Kind,
                        MimeType = $"{codec.Kind.GetEnumMemberValue()}/rtx",
                        PreferredPayloadType = pt,
                        ClockRate = codec.ClockRate,
                        Parameters = new Dictionary<string, object>
                        {
                            { "apt", codec.PreferredPayloadType}
                        },
                        RtcpFeedback = new List<RtcpFeedback>(0),
                    };

                    // Append to the codec list.
                    caps.Codecs.Add(rtxCodec);
                }
            }

            return caps;
        }

        public static void ValidateIceParameters(IceParameters iceParameters) {
            string usernameFragmentIt = iceParameters.usernameFragment;
            string passwordIt = iceParameters.password;
            bool iceLiteIt = iceParameters.iceLite;

            // usernameFragment is mandatory
            if (usernameFragmentIt == null) {
                throw new ArgumentException("ORTC: Missing username Fragment in IceParameters");
            }

            // passwordIt is mandatory
            if (passwordIt == null)
            {
                throw new ArgumentException("ORTC: Missing passwordIt Fragment in IceParameters");
            }
        }

        public static void ValidateIceCandidate(IceCandidate candidate) {
            if (candidate == null)
                throw new ArgumentNullException(nameof(candidate));

            if (string.IsNullOrEmpty(candidate.foundation))
                throw new ArgumentException("missing foundation");

            if (candidate.priority <= 0)
                throw new ArgumentException("priority must be a positive integer");

            if (string.IsNullOrEmpty(candidate.ip))
                throw new ArgumentException("missing ip");

            if (string.IsNullOrEmpty(candidate.protocol))
                throw new ArgumentException("missing protocol");

            if (!ProtocolRegex.IsMatch(candidate.protocol))
                throw new ArgumentException("invalid protocol");

            if (candidate.port <= 0)
                throw new ArgumentException("port must be a positive integer");

            if (string.IsNullOrEmpty(candidate.type))
                throw new ArgumentException("missing type");

            if (!TypeRegex.IsMatch(candidate.type))
                throw new ArgumentException("invalid type");
        }

        public static void ValidateIceCandidates(List<IceCandidate> iceCandidates) {
            if (iceCandidates == null || iceCandidates.Count == 0) {
                throw new ArgumentException("Ice Candidates cannot be null or empty");
            }

            foreach (IceCandidate candidate in iceCandidates) {
                ValidateIceCandidate(candidate);
            }
        }
        
        /// <summary>
        /// <para>
        /// Get a mapping in codec payloads and encodings in the given Producer RTP
        /// parameters as values expected by the Router.
        /// </para>
        /// <para>It may throw if invalid or non supported RTP parameters are given.</para>
        /// </summary>
        public static RtpMapping GetProducerRtpParametersMapping(RtpParameters parameters, RtpCapabilities caps)
        {
            var rtpMapping = new RtpMapping
            {
                Codecs = new List<RtpMappingCodec>(),
                Encodings = new List<RtpMappingEncoding>()
            };

            // Match parameters media codecs to capabilities media codecs.
            var codecToCapCodec = new Dictionary<RtpCodecParameters, RtpCodecCapability>();

            foreach (var codec in parameters.Codecs)
            {
                if (IsRtxMimeType(codec.MimeType))
                {
                    continue;
                }

                // Search for the same media codec in capabilities.
                var matchedCapCodec = caps.Codecs!
                    .FirstOrDefault(capCodec => MatchCodecs(codec, capCodec, true, true));
                codecToCapCodec[codec] = matchedCapCodec ?? throw new NotSupportedException($"Unsupported codec[mimeType:{codec.MimeType}, payloadType:{codec.PayloadType}, Channels:{codec.Channels}]");
            }

            // Match parameters RTX codecs to capabilities RTX codecs.
            foreach (var codec in parameters.Codecs)
            {
                if (!IsRtxMimeType(codec.MimeType))
                {
                    continue;
                }

                // Search for the associated media codec.
                var associatedMediaCodec = parameters.Codecs
                    .FirstOrDefault(mediaCodec => MatchCodecsWithPayloadTypeAndApt(mediaCodec.PayloadType, codec.Parameters!))
                    ?? throw new Exception($"missing media codec found for RTX PT {codec.PayloadType}");

                var capMediaCodec = codecToCapCodec[associatedMediaCodec];

                // Ensure that the capabilities media codec has a RTX codec.
                var associatedCapRtxCodec = caps.Codecs!
                    .FirstOrDefault(capCodec => IsRtxMimeType(capCodec.MimeType) && MatchCodecsWithPayloadTypeAndApt(capMediaCodec.PreferredPayloadType, capCodec.Parameters!));
                codecToCapCodec[codec] = associatedCapRtxCodec ?? throw new Exception($"no RTX codec for capability codec PT {capMediaCodec.PreferredPayloadType}");
            }

            // Generate codecs mapping.
            foreach (var item in codecToCapCodec)
            {
                rtpMapping.Codecs.Add(new RtpMappingCodec
                {
                    PayloadType = item.Key.PayloadType,
                    MappedPayloadType = item.Value.PreferredPayloadType!.Value,
                });
            }

            // Generate encodings mapping.
            uint mappedSsrc = Utils.GenerateRandomNumber();

            foreach (var encoding in parameters.Encodings)
            {
                var mappedEncoding = new RtpMappingEncoding
                {
                    MappedSsrc = mappedSsrc++,
                    Rid = encoding.Rid,
                    Ssrc = encoding.Ssrc,
                    ScalabilityMode = encoding.ScalabilityMode,
                };

                rtpMapping.Encodings.Add(mappedEncoding);
            }

            return rtpMapping;
        }

        /// <summary>
        /// Generate RTP parameters to be internally used by Consumers given the RTP
        /// parameters in a Producer and the RTP capabilities in the Router.
        /// </summary>
        public static RtpParameters GetConsumableRtpParameters(MediaKind kind, RtpParameters parameters, RtpCapabilities caps, RtpMapping rtpMapping)
        {
            var consumableParams = new RtpParameters
            {
                Codecs = new List<RtpCodecParameters>(),
                HeaderExtensions = new List<RtpHeaderExtensionParameters>(),
                Encodings = new List<RtpEncodingParameters>(),
                Rtcp = new RtcpParameters(),
            };

            foreach (var codec in parameters.Codecs)
            {
                if (IsRtxMimeType(codec.MimeType))
                {
                    continue;
                }

                var consumableCodecPt = rtpMapping.Codecs
                    .Where(entry => entry.PayloadType == codec.PayloadType)
                    .Select(m => m.MappedPayloadType)
                    .FirstOrDefault();

                var matchedCapCodec = caps.Codecs!
                    .FirstOrDefault(capCodec => capCodec.PreferredPayloadType == consumableCodecPt);

                var consumableCodec = new RtpCodecParameters
                {
                    MimeType = matchedCapCodec!.MimeType,
                    PayloadType = matchedCapCodec.PreferredPayloadType!.Value,
                    ClockRate = matchedCapCodec.ClockRate,
                    Channels = matchedCapCodec.Channels,
                    Parameters = codec.Parameters, // Keep the Producer codec parameters.
                    RtcpFeedback = matchedCapCodec.RtcpFeedback
                };

                consumableParams.Codecs.Add(consumableCodec);

                var consumableCapRtxCodec = caps.Codecs!
                    .FirstOrDefault(capRtxCodec => IsRtxMimeType(capRtxCodec.MimeType) && MatchCodecsWithPayloadTypeAndApt(consumableCodec.PayloadType, capRtxCodec.Parameters!));

                if (consumableCapRtxCodec != null)
                {
                    var consumableRtxCodec = new RtpCodecParameters
                    {
                        MimeType = consumableCapRtxCodec.MimeType,
                        PayloadType = consumableCapRtxCodec.PreferredPayloadType!.Value,
                        ClockRate = consumableCapRtxCodec.ClockRate,
                        Channels = consumableCapRtxCodec.Channels,
                        Parameters = consumableCapRtxCodec.Parameters, // Keep the Producer codec parameters.
                        RtcpFeedback = consumableCapRtxCodec.RtcpFeedback
                    };

                    consumableParams.Codecs.Add(consumableRtxCodec);
                }
            }

            foreach (var capExt in caps.HeaderExtensions!)
            {
                // Just take RTP header extension that can be used in Consumers.
                if (capExt.Kind != kind || (capExt.Direction != RtpHeaderExtensionDirection.SendReceive && capExt.Direction != RtpHeaderExtensionDirection.SendOnly))
                {
                    continue;
                }

                var consumableExt = new RtpHeaderExtensionParameters
                {
                    Uri = capExt.Uri,
                    Id = capExt.PreferredId,
                    Encrypt = capExt.PreferredEncrypt,
                    Parameters = new Dictionary<string, object>(),
                };

                consumableParams.HeaderExtensions.Add(consumableExt);
            }

            // Clone Producer encodings since we'll mangle them.
            var consumableEncodings = Utils.Clone(parameters.Encodings!);

            for (var i = 0; i < consumableEncodings.Count; ++i)
            {
                var consumableEncoding = consumableEncodings[i];
                var mappedSsrc = rtpMapping.Encodings[i].MappedSsrc;

                // Remove useless fields.
                consumableEncoding.Rid = null;
                consumableEncoding.Rtx = null;
                consumableEncoding.CodecPayloadType = null;

                // Set the mapped ssrc.
                consumableEncoding.Ssrc = mappedSsrc;

                consumableParams.Encodings.Add(consumableEncoding);
            }

            consumableParams.Rtcp = new RtcpParameters
            {
                CNAME = parameters.Rtcp!.CNAME,
                ReducedSize = true,
            };

            return consumableParams;
        }

        /// <summary>
        /// Check whether the given RTP capabilities can consume the given Producer.
        /// </summary>
        public static bool CanConsume(RtpParameters consumableParams, RtpCapabilities caps)
        {
            // This may throw.
            ValidateRtpCapabilities(caps);

            var matchingCodecs = new List<RtpCodecParameters>();

            foreach (var codec in consumableParams.Codecs)
            {
                var matchedCapCodec = caps.Codecs!
                    .FirstOrDefault(capCodec => MatchCodecs(capCodec, codec, true));

                if (matchedCapCodec == null)
                {
                    continue;
                }

                matchingCodecs.Add(codec);
            }

            // Ensure there is at least one media codec.
            return matchingCodecs.Count != 0 && !IsRtxMimeType(matchingCodecs[0].MimeType);
        }

        /// <summary>
        /// <para>Generate RTP parameters for a specific Consumer.</para>
        /// <para>
        /// It reduces encodings to just one and takes into account given RTP capabilities
        /// to reduce codecs, codecs' RTCP feedback and header extensions, and also enables
        /// or disabled RTX.
        /// </para>
        /// </summary>
        public static RtpParameters GetConsumerRtpParameters(RtpParameters consumableParams, RtpCapabilities caps, bool pipe)
        {
            var consumerParams = new RtpParameters
            {
                Codecs = new List<RtpCodecParameters>(),
                HeaderExtensions = new List<RtpHeaderExtensionParameters>(),
                Encodings = new List<RtpEncodingParameters>(),
                Rtcp = consumableParams.Rtcp
            };

            foreach (var capCodec in caps.Codecs!)
            {
                ValidateRtpCodecCapability(capCodec);
            }

            var consumableCodecs = Utils.Clone(consumableParams.Codecs);

            var rtxSupported = false;

            foreach (var codec in consumableCodecs)
            {
                var matchedCapCodec = caps.Codecs
                    .FirstOrDefault(capCodec => MatchCodecs(capCodec, codec, true));

                if (matchedCapCodec == null)
                {
                    continue;
                }

                codec.RtcpFeedback = matchedCapCodec.RtcpFeedback;

                consumerParams.Codecs.Add(codec);
            }

            // Must sanitize the list of matched codecs by removing useless RTX codecs.
            var codecsToRemove = new List<RtpCodecParameters>();
            foreach (var codec in consumerParams.Codecs)
            {
                if (IsRtxMimeType(codec.MimeType))
                {
                    if (!codec.Parameters!.TryGetValue("apt", out var apt))
                    {
                        throw new Exception("\"apt\" key is not exists.");
                    }

                    byte apiInteger = 0;
                    apiInteger = ExtractApt(apt);

                    // Search for the associated media codec.
                    var associatedMediaCodec = consumerParams.Codecs.FirstOrDefault(mediaCodec => mediaCodec.PayloadType == apiInteger);
                    if (associatedMediaCodec != null)
                    {
                        rtxSupported = true;
                    }
                    else
                    {
                        codecsToRemove.Add(codec);
                    }
                }
            }

            codecsToRemove.ForEach(m => consumerParams.Codecs.Remove(m));

            // Ensure there is at least one media codec.
            if (consumerParams.Codecs.Count == 0 || IsRtxMimeType(consumerParams.Codecs[0].MimeType))
            {
                throw new Exception("no compatible media codecs");
            }

            consumerParams.HeaderExtensions = consumableParams.HeaderExtensions!
                .Where(ext =>
                    caps.HeaderExtensions!
                        .Any(capExt => capExt.PreferredId == ext.Id && capExt.Uri == ext.Uri)
                ).ToList();

            // Reduce codecs' RTCP feedback. Use Transport-CC if available, REMB otherwise.
            if (consumerParams.HeaderExtensions.Any(ext => ext.Uri == RtpHeaderExtensionUri.TransportWideCcDraft01))
            {
                foreach (var codec in consumerParams.Codecs)
                {
                    codec.RtcpFeedback = codec.RtcpFeedback.Where(fb => fb.Type != "goog-remb").ToList();
                }
            }
            else if (consumerParams.HeaderExtensions.Any(ext => ext.Uri == RtpHeaderExtensionUri.AbsSendTime))
            {
                foreach (var codec in consumerParams.Codecs)
                {
                    codec.RtcpFeedback = codec.RtcpFeedback.Where(fb => fb.Type != "transport-cc").ToList();
                }
            }
            else
            {
                foreach (var codec in consumerParams.Codecs)
                {
                    codec.RtcpFeedback = codec.RtcpFeedback.Where(fb => fb.Type is not "transport-cc" and not "goog-remb").ToList();
                }
            }

            if (!pipe)
            {
                var consumerEncoding = new RtpEncodingParameters
                {
                    Ssrc = Utils.GenerateRandomNumber()
                };

                if (rtxSupported)
                {
                    consumerEncoding.Rtx = new Rtx { Ssrc = consumerEncoding.Ssrc.Value + 1 };
                }

                // If any in the consumableParams.Encodings has scalabilityMode, process it
                // (assume all encodings have the same value).
                var encodingWithScalabilityMode = consumableParams.Encodings.FirstOrDefault(encoding => !encoding.ScalabilityMode.IsNullOrWhiteSpace());

                var scalabilityMode = encodingWithScalabilityMode?.ScalabilityMode;

                // If there is simulast, mangle spatial layers in scalabilityMode.
                if (consumableParams.Encodings.Count > 1)
                {
                    var scalabilityModeObject = ScalabilityMode.Parse(scalabilityMode!);

                    scalabilityMode = $"S{consumableParams.Encodings.Count}T{scalabilityModeObject.TemporalLayers}";
                }

                if (!scalabilityMode.IsNullOrWhiteSpace())
                {
                    consumerEncoding.ScalabilityMode = scalabilityMode;
                }

                // Use the maximum maxBitrate in any encoding and honor it in the Consumer's
                // encoding.
                var maxEncodingMaxBitrate = consumableParams.Encodings.Max(m => m.MaxBitrate);
                if (maxEncodingMaxBitrate > 0)
                {
                    consumerEncoding.MaxBitrate = maxEncodingMaxBitrate;
                }

                // Set a single encoding for the Consumer.
                consumerParams.Encodings.Add(consumerEncoding);
            }
            else
            {
                var consumableEncodings = Utils.Clone(consumableParams.Encodings);
                var baseSsrc = Utils.GenerateRandomNumber();
                var baseRtxSsrc = Utils.GenerateRandomNumber();

                for (var i = 0; i < consumableEncodings!.Count; ++i)
                {
                    var encoding = consumableEncodings[i];
                    encoding.Ssrc = baseSsrc + (uint)i;
                    encoding.Rtx = rtxSupported ? new Rtx { Ssrc = baseRtxSsrc + (uint)i } : null;

                    consumerParams.Encodings.Add(encoding);
                }
            }

            return consumerParams;
        }

        /// <summary>
        /// <para>Generate RTP parameters for a pipe Consumer.</para>
        /// <para>
        /// It keeps all original consumable encodings and removes support for BWE. If
        /// enableRtx is false, it also removes RTX and NACK support.
        /// </para>
        /// </summary>
        public static RtpParameters GetPipeConsumerRtpParameters(RtpParameters consumableParams, bool enableRtx = false)
        {
            var consumerParams = new RtpParameters
            {
                Codecs = new List<RtpCodecParameters>(),
                HeaderExtensions = new List<RtpHeaderExtensionParameters>(),
                Encodings = new List<RtpEncodingParameters>(),
                Rtcp = consumableParams.Rtcp
            };

            var consumableCodecs = Utils.Clone(consumableParams.Codecs);

            foreach (var codec in consumableCodecs)
            {
                if (!enableRtx && IsRtxMimeType(codec.MimeType))
                {
                    continue;
                }

                codec.RtcpFeedback = codec.RtcpFeedback
                    .Where(fb =>
                        (fb.Type == "nack" && fb.Parameter == "pli") ||
                        (fb.Type == "ccm" && fb.Parameter == "fir") ||
                        (enableRtx && fb.Type == "nack" && fb.Parameter.IsNullOrWhiteSpace())
                    ).ToList();

                consumerParams.Codecs.Add(codec);
            }

            // Reduce RTP extensions by disabling transport MID and BWE related ones.
            consumerParams.HeaderExtensions = consumableParams.HeaderExtensions!
                .Where(ext => ext.Uri != RtpHeaderExtensionUri.Mid &&
                ext.Uri != RtpHeaderExtensionUri.AbsSendTime &&
                ext.Uri != RtpHeaderExtensionUri.TransportWideCcDraft01).ToList();

            var consumableEncodings = Utils.Clone(consumableParams.Encodings);

            var baseSsrc = Utils.GenerateRandomNumber();
            var baseRtxSsrc = Utils.GenerateRandomNumber();

            for (var i = 0; i < consumableEncodings.Count; ++i)
            {
                var encoding = consumableEncodings[i];
                encoding.Ssrc = (uint)(baseSsrc + i);

                if (enableRtx)
                {
                    encoding.Rtx = new Rtx { Ssrc = (uint)(baseRtxSsrc + i) };
                }
                else
                {
                    encoding.Rtx = null;
                }

                consumerParams.Encodings.Add(encoding);
            }

            return consumerParams;
        }

        private static bool IsRtxMimeType(string mimeType)
        {
            return RtxMimeTypeRegex.IsMatch(mimeType);
        }

        private static bool CheckDirectoryValueEquals(IDictionary<string, object> a, IDictionary<string, object> b, string key)
        {
            if (a != null && b != null)
            {
                var got1 = a.TryGetValue(key, out var aPacketizationMode);
                var got2 = b.TryGetValue(key, out var bPacketizationMode);
                if (got1 && got2 && !aPacketizationMode!.Equals(bPacketizationMode))
                {
                    return false;
                }
                else if (got1 ^ got2)
                {
                    return false;
                }
            }
            else if (a != null && b == null)
            {
                var got = a.ContainsKey("packetization-mode");
                if (got)
                {
                    return false;
                }
            }
            else if (a == null && b != null)
            {
                var got = b.ContainsKey("packetization-mode");
                if (got)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool MatchCodecs(RtpCodecBase aCodec, RtpCodecBase bCodec, bool strict = false, bool modify = false)
        {
            var aMimeType = aCodec.MimeType.ToLower();
            var bMimeType = bCodec.MimeType.ToLower();

            if (aMimeType != bMimeType || aCodec.ClockRate != bCodec.ClockRate || aCodec.Channels != bCodec.Channels)
            {
                return false;
            }

            // Per codec special checks.
            switch (aMimeType)
            {
                case "audio/multiopus":
                    {
                        var aNumStreams = aCodec.Parameters!["num_streams"];
                        var bNumStreams = bCodec.Parameters!["num_streams"];

                        if (aNumStreams != bNumStreams)
                        {
                            return false;
                        }

                        var aCoupledStreams = aCodec.Parameters["coupled_streams"];
                        var bCoupledStreams = bCodec.Parameters["coupled_streams"];

                        if (aCoupledStreams != bCoupledStreams)
                        {
                            return false;
                        }

                        break;
                    }
                case "video/h264":
                case "video/h264-svc":
                    {
                        // If strict matching check profile-level-id.
                        if (strict)
                        {
                            if (!CheckDirectoryValueEquals(aCodec.Parameters!, aCodec.Parameters!, "packetization-mode"))
                            {
                                return false;
                            }

                            if (!Huddle01.H264ProfileLevelId.Utils.IsSameProfile(aCodec.Parameters!, bCodec.Parameters!))
                            {
                                return false;
                            }
                            string? selectedProfileLevelId;

                            try
                            {
                                selectedProfileLevelId = Huddle01.H264ProfileLevelId.Utils.GenerateProfileLevelIdForAnswer(aCodec.Parameters!, bCodec.Parameters!);
                            }
                            catch (Exception ex)
                            {
                                UnityEngine.Debug.Log($"MatchCodecs() | {ex.Message}");
                                return false;
                            }

                            if (modify)
                            {
                                if (!selectedProfileLevelId.IsNullOrWhiteSpace())
                                {
                                    aCodec.Parameters!["profile-level-id"] = selectedProfileLevelId!;
                                }
                                else
                                {
                                    aCodec.Parameters!.Remove("profile-level-id");
                                }
                            }
                        }

                        break;
                    }
                case "video/vp9":
                    {
                        if (strict)
                        {
                            if (!CheckDirectoryValueEquals(aCodec.Parameters!, aCodec.Parameters!, "profile-id"))
                            {
                                return false;
                            }
                        }

                        break;
                    }

                default:
                    break;
            }

            return true;
        }

        private static bool MatchCodecsWithPayloadTypeAndApt(byte? payloadType, IDictionary<string, object> parameters)
        {
            if (payloadType == null && parameters == null)
            {
                return true;
            }

            if (parameters == null)
            {
                return false;
            }

            if (!parameters.TryGetValue("apt", out var apt))
            {
                return false;
            }

            var aptInteger = ExtractApt(apt);

            return payloadType == aptInteger;
        }

        public static RtpParameters GetSendingRemoteRtpParameters(MediaKind kind, ExtendedRtpCapabilities extendedRtpCapabilities)
        {
            RtpParameters rtpParameters = new RtpParameters
            {
                Mid = null,
                Codecs = new List<RtpCodecParameters>(),
                HeaderExtensions = new List<RtpHeaderExtensionParameters>(),
                Encodings = new List<RtpEncodingParameters>(),
                Rtcp = new RtcpParameters()
            };

            foreach (ExtendedRtpCodecCapability extendedCodec in extendedRtpCapabilities.codecs)
            {
                if (extendedCodec.kind != kind)
                {
                    continue;
                }

                RtpCodecParameters codec = new RtpCodecParameters
                {
                    MimeType = extendedCodec.mimeType,
                    PayloadType = extendedCodec.localPayloadType.Value,
                    ClockRate = extendedCodec.clockRate,
                    Channels = extendedCodec.channels,
                    Parameters = extendedCodec.remoteParameters,
                    RtcpFeedback = extendedCodec.rtcpFeedback
                };

                rtpParameters.Codecs.Add(codec);

                if (extendedCodec.remoteRtxPayloadType.HasValue)
                {
                    RtpCodecParameters rtxCodec = new RtpCodecParameters
                    {
                        MimeType = $"{extendedCodec.kind}/rtx",
                        PayloadType = extendedCodec.remoteRtxPayloadType.Value,
                        ClockRate = extendedCodec.clockRate,
                        Parameters = new Dictionary<string, object> { { "apt", extendedCodec.remotePayloadType } },
                        RtcpFeedback = new List<RtcpFeedback>()
                    };

                    rtpParameters.Codecs.Add(rtxCodec);
                }
            }

            foreach (ExtendedRtpHeaderExtension extendedExt in extendedRtpCapabilities.headerExtensions)
            {
                if ((extendedExt.Kind != null && extendedExt.Kind != kind) ||
                    (extendedExt.Direction != RtpHeaderExtensionDirection.SendOnly
                    && extendedExt.Direction != RtpHeaderExtensionDirection.SendReceive))
                {
                    continue;
                }

                RtpHeaderExtensionParameters ext = new RtpHeaderExtensionParameters
                {
                    Uri = extendedExt.Uri,
                    Id = extendedExt.sendId,
                    Encrypt = extendedExt.PreferredEncrypt,
                    Parameters = new ExpandoObject()
                };

                rtpParameters.HeaderExtensions.Add(ext);
            }

            if (rtpParameters.HeaderExtensions.Exists(ext => EnumExtensions.GetEnumMemberValue(ext.Uri) == "http://www.ietf.org/id/draft-holmer-rmcat-transport-wide-cc-extensions-01"))
            {
                foreach (RtpCodecParameters codec in rtpParameters.Codecs)
                {
                    codec.RtcpFeedback = codec.RtcpFeedback.FindAll(fb => fb.Type == "goog-remb");
                }
            }
            else if (
                rtpParameters.HeaderExtensions!.Exists(
                    ext =>
                        EnumExtensions.GetEnumMemberValue(ext.Uri) == "http://www.webrtc.org/experiments/rtp-hdrext/abs-send-time"
                )
            )
            {
                foreach (RtpCodecParameters codec in rtpParameters.Codecs)
                {
                    codec.RtcpFeedback = codec.RtcpFeedback.FindAll(
                        fb => fb.Type != "transport-cc"
                    );
                }
            }
            else
            {
                foreach (RtpCodecParameters codec in rtpParameters.Codecs)
                {
                    codec.RtcpFeedback = codec.RtcpFeedback.FindAll(
                        fb =>

                            fb.Type != "transport-cc" && fb.Type != "goog-remb"
                    );
                }
            }

            return rtpParameters;
        }

        public static RtpCapabilities GetRecvRtpCapabilities(ExtendedRtpCapabilities extendedRtpCapabilities)
        {
            RtpCapabilities rtpCapabilities = new RtpCapabilities();

            foreach (ExtendedRtpCodecCapability extendedCodec in extendedRtpCapabilities.codecs)
            {
                RtpCodecCapability codec = new RtpCodecCapability
                {
                    MimeType = extendedCodec.mimeType,
                    Kind = extendedCodec.kind,
                    PreferredPayloadType = extendedCodec.remotePayloadType,
                    ClockRate = extendedCodec.clockRate,
                    Channels = extendedCodec.channels,
                    Parameters = extendedCodec.localParameters,
                    RtcpFeedback = extendedCodec.rtcpFeedback
                };

                rtpCapabilities.Codecs.Add(codec);

                if (!extendedCodec.remoteRtxPayloadType.HasValue)
                {
                    continue;
                }

                RtpCodecCapability rtxCodec = new RtpCodecCapability
                {
                    MimeType = $"{extendedCodec.kind}/rtx",
                    Kind = extendedCodec.kind,
                    PreferredPayloadType = extendedCodec.remoteRtxPayloadType.Value,
                    ClockRate = extendedCodec.clockRate,
                    Parameters = new Dictionary<string, object> { { "apt", extendedCodec.remotePayloadType } },
                    RtcpFeedback = new List<RtcpFeedback>()
                };

                rtpCapabilities.Codecs.Add(rtxCodec);
            }

            foreach (var extendedExt in extendedRtpCapabilities.headerExtensions)
            {

                if (extendedExt.Direction != RtpHeaderExtensionDirection.SendReceive &&
                      extendedExt.Direction != RtpHeaderExtensionDirection.ReceiveOnly)
                {
                    continue;
                }

                RtpHeaderExtension ext = new RtpHeaderExtension
                {
                    Kind = extendedExt.Kind,
                    Uri = extendedExt.Uri,
                    PreferredId = extendedExt.recvId,
                    PreferredEncrypt = extendedExt.PreferredEncrypt,
                    Direction = extendedExt.Direction
                };

                rtpCapabilities.HeaderExtensions.Append(ext);
            }

            return rtpCapabilities;
        }


        public static RtpParameters GetSendingRtpParameters(MediaKind kind, ExtendedRtpCapabilities extendedRtpCapabilities)
        {
            RtpParameters RtpParameters = new RtpParameters
            {
                Mid = null,
                Codecs = new List<RtpCodecParameters>(),
                HeaderExtensions = new List<RtpHeaderExtensionParameters>(),
                Encodings = new List<RtpEncodingParameters>(),
                Rtcp = new RtcpParameters()
            };

            foreach (ExtendedRtpCodecCapability extendedCodec in extendedRtpCapabilities.codecs)
            {
                if (extendedCodec.kind != kind)
                {
                    continue;
                }

                RtpCodecParameters codec = new RtpCodecParameters
                {
                    MimeType = extendedCodec.mimeType,
                    PayloadType = extendedCodec.localPayloadType.Value,
                    ClockRate = extendedCodec.clockRate,
                    Channels = extendedCodec.channels,
                    Parameters = extendedCodec.localParameters,
                    RtcpFeedback = extendedCodec.rtcpFeedback
                };

                RtpParameters.Codecs.Add(codec);

                if (extendedCodec.localRtxPayloadType.HasValue)
                {
                    RtpCodecParameters rtcCodec = new RtpCodecParameters
                    {
                        MimeType = $"{extendedCodec.kind}/rtx",
                        PayloadType = extendedCodec.localRtxPayloadType.Value,
                        ClockRate = extendedCodec.clockRate,
                        Parameters = new Dictionary<string, object> { { "apt", extendedCodec.localPayloadType } },
                        RtcpFeedback = new List<RtcpFeedback>()
                    };

                    RtpParameters.Codecs.Add(rtcCodec);
                }
            }

            foreach (ExtendedRtpHeaderExtension extendedExt in extendedRtpCapabilities.headerExtensions)
            {
                if ((extendedExt.Kind != kind) ||
                    (extendedExt.Direction != RtpHeaderExtensionDirection.SendOnly
                    && extendedExt.Direction != RtpHeaderExtensionDirection.SendReceive))
                {
                    continue;
                }

                RtpHeaderExtensionParameters ext = new RtpHeaderExtensionParameters
                {
                    Uri = extendedExt.Uri,
                    Id = extendedExt.sendId,
                    Encrypt = extendedExt.PreferredEncrypt,
                    Parameters = new ExpandoObject()
                };

                RtpParameters.HeaderExtensions.Add(ext);
            }

            return RtpParameters;
        }

        public static List<RtpCodecParameters> ReduceCodecs(List<RtpCodecParameters> codecs, RtpCodecCapability? capCodec) { 
            
            List<RtpCodecParameters> filteredCodecs = new List<RtpCodecParameters>();

            if (capCodec == null)
            {
                filteredCodecs.Add(codecs[0]);

                if (codecs[1] != null && IsRtxMimeType(codecs[1].MimeType))
                {
                    filteredCodecs.Add(codecs[1]);
                }
            }
            else {
                for (var idx = 0; idx < codecs.Count; ++idx)
                {
                    if (MatchCodecs(codecs[idx], capCodec))
                    {
                        filteredCodecs.Add(codecs[idx]);

                        if (codecs != null && IsRtxMimeType(codecs[idx + 1].MimeType))
                        {
                            filteredCodecs.Add(codecs[idx + 1]);
                        }

                        break;
                    }
                }

                if (filteredCodecs.Count == 0)
                {
                    throw new Exception("no matching codec");
                }

            }
            
            return filteredCodecs;
        }

        private static bool IsRtxCodec(RtpCodecCapability codec) {
            if (codec == null) return false;
            return IsRtxMimeType(codec.MimeType);
        }

        public static ExtendedRtpCapabilities GetExtendedRtpCapabilities(RtpCapabilities localCaps, RtpCapabilities remoteCaps)
        {
            ExtendedRtpCapabilities extendedRtpCapabilities = new ExtendedRtpCapabilities();

            foreach (RtpCodecCapability remoteCodec in remoteCaps.Codecs)
            {
                if (remoteCodec != null && IsRtxMimeType(remoteCodec.MimeType))
                {
                    continue;
                }

                RtpCodecCapability matchingLocalCodec = null;
                if (localCaps.Codecs != null)
                {
                    foreach (RtpCodecCapability localCodec in localCaps.Codecs)
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
                    mimeType = matchingLocalCodec.MimeType,
                    kind = matchingLocalCodec.Kind,
                    clockRate = matchingLocalCodec.ClockRate,
                    channels = matchingLocalCodec.Channels,
                    localPayloadType = matchingLocalCodec.PreferredPayloadType,
                    remotePayloadType = remoteCodec.PreferredPayloadType,
                    localParameters = matchingLocalCodec.Parameters,
                    remoteParameters = remoteCodec.Parameters,
                    rtcpFeedback = ReduceRtcpFeedback(matchingLocalCodec, remoteCodec),
                };

                extendedRtpCapabilities.codecs.Add(extendedCodec);
            }

            foreach (ExtendedRtpCodecCapability extendedCodec in extendedRtpCapabilities.codecs)
            {
                {

                    RtpCodecCapability matchingLocalRtxCodec = null;
                    foreach (RtpCodecCapability localCodec in localCaps.Codecs)
                    {
                        if (localCodec != null && IsRtxMimeType(localCodec.MimeType)) {
                            byte localAptValue = ExtractApt(localCodec.Parameters["apt"]);
                            
                            if (localAptValue == extendedCodec.localPayloadType)
                            {
                                matchingLocalRtxCodec = localCodec;
                                break;
                            }
                        }
                    }


                    RtpCodecCapability matchingRemoteRtxCodec = remoteCaps.Codecs.FirstOrDefault(codec => IsRtxCodec(codec)
                    && ExtractApt(codec.Parameters["apt"]) == extendedCodec.localPayloadType);

                    if (matchingLocalRtxCodec != null && matchingRemoteRtxCodec != null)
                    {
                        extendedCodec.localRtxPayloadType = matchingLocalRtxCodec.PreferredPayloadType;
                        extendedCodec.remoteRtxPayloadType = matchingRemoteRtxCodec.PreferredPayloadType;
                    }
                }
            }

            foreach (RtpHeaderExtension remoteExt in remoteCaps.HeaderExtensions)
            {
                RtpHeaderExtension matchingLocalExt = localCaps.HeaderExtensions.FirstOrDefault(ext => MatchHeaderExtensions(ext, remoteExt));

                if (matchingLocalExt == null) continue;

                ExtendedRtpHeaderExtension extendedExt = new ExtendedRtpHeaderExtension
                {
                    Kind = remoteExt.Kind,
                    Uri = remoteExt.Uri,
                    sendId = matchingLocalExt.PreferredId,
                    recvId = remoteExt.PreferredId,
                    PreferredEncrypt = matchingLocalExt.PreferredEncrypt,
                    Direction = RtpHeaderExtensionDirection.SendReceive
                };

                switch (remoteExt.Direction)
                {

                    case (RtpHeaderExtensionDirection.SendReceive):
                        {
                            extendedExt.Direction = RtpHeaderExtensionDirection.SendReceive;
                            break;
                        }
                    case (RtpHeaderExtensionDirection.ReceiveOnly):
                        {
                            extendedExt.Direction = RtpHeaderExtensionDirection.SendOnly;
                            break;
                        }
                    case (RtpHeaderExtensionDirection.SendOnly):
                        {
                            extendedExt.Direction = RtpHeaderExtensionDirection.ReceiveOnly;
                            break;
                        }
                    case (RtpHeaderExtensionDirection.Inactive):
                        {
                            extendedExt.Direction = RtpHeaderExtensionDirection.Inactive;
                            break;
                        }
                }

                extendedRtpCapabilities.headerExtensions.Add(extendedExt);

            }


            return extendedRtpCapabilities;
        }

        private static bool MatchHeaderExtensions(RtpHeaderExtension a, RtpHeaderExtension b)
        {
            if (a.Kind != null && b.Kind != null && a.Kind != b.Kind)
            {
                return false;
            }

            if (a.Uri != b.Uri) return false;

            return true;

        }

        public static List<RtcpFeedback> ReduceRtcpFeedback(RtpCodecCapability codecA, RtpCodecCapability codecB)
        {
            List<RtcpFeedback> reducedRtcpFeedback = new List<RtcpFeedback>();

            foreach (RtcpFeedback aFb in codecA.RtcpFeedback)
            {
                foreach (RtcpFeedback bFb in codecB.RtcpFeedback)
                {
                    if (aFb.Type == bFb.Type)
                    {
                        if (aFb.Parameter == bFb.Parameter || (aFb.Parameter == null && bFb.Parameter == null))
                        {
                            reducedRtcpFeedback.Add(aFb);
                            break;
                        }
                    }
                }
            }

            return reducedRtcpFeedback;
        }

        public static bool CanReceive(RtpParameters rtpParam,ExtendedRtpCapabilities extendedRtpCapabilities) 
        {
            ValidateRtpParameters(rtpParam);

            if (rtpParam.Codecs.Count==0) 
            {
                return false;
            }

            var firstMediaCodec = rtpParam.Codecs[0];

            return extendedRtpCapabilities.codecs.Any(codec => codec.remotePayloadType == firstMediaCodec.PayloadType);

        }

        public static RtpParameters GenerateProbatorRtpParameters(RtpParameters videoRtpParameters) 
        {
            RtpParameters result = Utils.Clone(videoRtpParameters);

            ValidateRtpParameters(result);

            RtpParameters rtpParameters = new RtpParameters 
            {
                Mid = "probator",
                Codecs = new List<RtpCodecParameters>(),
                HeaderExtensions = new List<RtpHeaderExtensionParameters>(),
                Encodings = new List<RtpEncodingParameters> { new RtpEncodingParameters {Ssrc = 1234 } },
                Rtcp = new RtcpParameters {CNAME = "probator" }
            };

            rtpParameters.Codecs.Add(videoRtpParameters.Codecs[0]);
            rtpParameters.Codecs[0].PayloadType = 127;
            rtpParameters.HeaderExtensions = videoRtpParameters.HeaderExtensions;

            return rtpParameters;
        }

        private static byte ExtractApt(object apt) {
            byte localAptValue;
            if (apt.IsNumericType())
            {
                localAptValue = byte.Parse(apt.ToString());
            }
            else
            {
                if (!byte.TryParse((string)apt , out localAptValue))
                {
                    throw new InvalidCastException("GetExtendedRtpCapabilities(): Cannot parse apt");
                }
            }

            return localAptValue;
        }

    }

    public static class OrtcUtils
    {
        public static void AddNackSuppportForOpus(RtpCapabilities rtpCapabilities)
        {
            foreach (var codec in rtpCapabilities.Codecs)
            {
                if ((codec.MimeType.ToLower() == "audio/opus" || codec.MimeType.ToLower() == "audio/multiopus") &&
                    (codec.RtcpFeedback?.Any(fb => fb.Type == "nack" && fb.Parameter == null) == true))
                {
                    if (codec.RtcpFeedback != null)
                    {
                        codec.RtcpFeedback = new List<RtcpFeedback>();
                    }


                    RtcpFeedback nackSupp = new RtcpFeedback { Type = "nack", Parameter = "" };
                    codec.RtcpFeedback.Add(nackSupp);
                }
            }
        }
    }
}

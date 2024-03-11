using Mediasoup.DataConsumers;
using Mediasoup.RtpParameter;
using Mediasoup.SctpParameter;
using Mediasoup.Transports;
using System;
using System.Collections.Generic;

namespace Mediasoup
{

    public class FakeParameters
    {
        public static System.Random rnd = new System.Random();

        private static string GenerateFakeUuid()
        {
            return new string(rnd.Next(10000000).ToString());
        }

        public static RtpCapabilities GenerateRouterRtpCapabilies()
        {
            return new RtpCapabilities
            {
                Codecs = new List<RtpCodecCapability>
                {
                    new RtpCodecCapability
                    {
                        Kind = MediaKind.AUDIO,
                        MimeType = "audio/opus",
                        ClockRate = 48000,
                        Channels = 2,
                        Parameters = new Dictionary<string, object>
                        {
                            { "foo", "bar" },
                            { "useinbandfec", 1 }
                        },
                        RtcpFeedback = new List<RtcpFeedback>{
                            new RtcpFeedback { Type = "transport-cc" },
                        },
                        PreferredPayloadType = 100
                    },
                    new RtpCodecCapability
                    {
                        Kind = MediaKind.VIDEO,
                        MimeType = "video/VP8",
                        ClockRate = 90000,
                        PreferredPayloadType = 101,
                        Parameters = new Dictionary<string, object>
                        {
                            {"x-google-start-bitrate" , 1500 },

                        },
                        RtcpFeedback = new List<RtcpFeedback>{
                            new RtcpFeedback { Type = "nack" },
                            new RtcpFeedback { Type = "nack", Parameter = "pli" },
                            new RtcpFeedback { Type = "ccm", Parameter = "fir" },
                            new RtcpFeedback { Type = "goog-remb" },
                            new RtcpFeedback { Type = "transport-cc" },
                        }
                    },
                    new RtpCodecCapability
                    {
                        Kind = MediaKind.VIDEO,
                        MimeType = "video/rtx",
                        ClockRate = 90000,

                        PreferredPayloadType = 102,
                        Parameters = new Dictionary<string, object>
                        {
                            { "apt", 101 },
                        },
                        RtcpFeedback = new List<RtcpFeedback>{
                        }
                    }
                    ,new RtpCodecCapability
                    {
                        Kind = MediaKind.VIDEO,
                        MimeType = "video/H264",
                        ClockRate = 90000,

                        PreferredPayloadType = 103,
                        Parameters = new Dictionary<string, object>
                        {
                            { "level-asymmetry-allowed", 1 },
                            { "packetization-mode", 1 },
                            { "profile-level-id" , "42e01f"}
                        },
                        RtcpFeedback = new List<RtcpFeedback>{
                            new RtcpFeedback { Type = "nack" },
                            new RtcpFeedback { Type = "nack", Parameter = "pli" },
                            new RtcpFeedback { Type = "ccm", Parameter = "fir" },
                            new RtcpFeedback { Type = "goog-remb" },
                            new RtcpFeedback { Type = "transport-cc" },
                        }
                    }
                    ,new RtpCodecCapability
                    {
                        Kind = MediaKind.VIDEO,
                        MimeType = "video/rtx",
                        ClockRate = 90000,
                        PreferredPayloadType = 104,
                        Parameters = new Dictionary<string, object>
                        {
                            { "apt", 103 },
                        },
                        RtcpFeedback = new List<RtcpFeedback>{
                        }
                    }
                },
                HeaderExtensions = new List<RtpHeaderExtension> {
                    new RtpHeaderExtension
                    {
                        Kind = MediaKind.AUDIO,
                        Uri = EnumExtensions.GetEnumValueFromEnumMemberValue<RtpHeaderExtensionUri>("urn:ietf:params:rtp-hdrext:sdes:mid"),
                        PreferredId = 1,
                        PreferredEncrypt = false,
                        Direction = RtpHeaderExtensionDirection.SendReceive
                    },
                    new RtpHeaderExtension
                    {
                        Kind = MediaKind.VIDEO,
                        Uri = EnumExtensions.GetEnumValueFromEnumMemberValue<RtpHeaderExtensionUri>("urn:ietf:params:rtp-hdrext:sdes:mid"),
                        PreferredId = 1,
                        PreferredEncrypt = false,
                        Direction = RtpHeaderExtensionDirection.SendReceive
                    },
                    new RtpHeaderExtension
                    {
                        Kind = MediaKind.VIDEO,
                        Uri = EnumExtensions.GetEnumValueFromEnumMemberValue<RtpHeaderExtensionUri>("urn:ietf:params:rtp-hdrext:sdes:mid"),
                        PreferredId = 2,
                        PreferredEncrypt = false,
                        Direction = RtpHeaderExtensionDirection.ReceiveOnly
                    },
                    new RtpHeaderExtension
                    {
                        Kind = MediaKind.VIDEO,
                        Uri = EnumExtensions.GetEnumValueFromEnumMemberValue<RtpHeaderExtensionUri>("urn:ietf:params:rtp-hdrext:sdes:repaired-rtp-stream-id"),
                        PreferredId = 3,
                        PreferredEncrypt = false,
                        Direction = RtpHeaderExtensionDirection.ReceiveOnly
                    },

                    new RtpHeaderExtension
                    {
                        Kind = MediaKind.AUDIO,
                        Uri = EnumExtensions.GetEnumValueFromEnumMemberValue<RtpHeaderExtensionUri>("http://www.webrtc.org/experiments/rtp-hdrext/abs-send-time"),
                        PreferredId = 4,
                        PreferredEncrypt = false,
                        Direction = RtpHeaderExtensionDirection.SendReceive
                    },new RtpHeaderExtension
                    {
                        Kind = MediaKind.VIDEO,
                        Uri = EnumExtensions.GetEnumValueFromEnumMemberValue<RtpHeaderExtensionUri>("http://www.webrtc.org/experiments/rtp-hdrext/abs-send-time"),
                        PreferredId = 4,
                        PreferredEncrypt = false,
                        Direction = RtpHeaderExtensionDirection.SendReceive
                    },
                    new RtpHeaderExtension
                    {
                        Kind = MediaKind.AUDIO,
                        Uri = EnumExtensions.GetEnumValueFromEnumMemberValue<RtpHeaderExtensionUri>("http://www.ietf.org/id/draft-holmer-rmcat-transport-wide-cc-extensions-01"),
                        PreferredId = 5,
                        PreferredEncrypt = false,
                        Direction = RtpHeaderExtensionDirection.ReceiveOnly
                    },

                    new RtpHeaderExtension
                    {
                        Kind = MediaKind.VIDEO,
                        Uri = EnumExtensions.GetEnumValueFromEnumMemberValue<RtpHeaderExtensionUri>("http://www.ietf.org/id/draft-holmer-rmcat-transport-wide-cc-extensions-01"),
                        PreferredId = 5,
                        PreferredEncrypt = false,
                        Direction = RtpHeaderExtensionDirection.SendReceive
                    },
                    new RtpHeaderExtension
                    {
                        Kind = MediaKind.VIDEO,
                        Uri = EnumExtensions.GetEnumValueFromEnumMemberValue<RtpHeaderExtensionUri>("http://tools.ietf.org/html/draft-ietf-avtext-framemarking-07"),
                        PreferredId = 6,
                        PreferredEncrypt = false,
                        Direction = RtpHeaderExtensionDirection.SendReceive
                    },
                    new RtpHeaderExtension
                    {
                        Kind = MediaKind.VIDEO,
                        Uri = EnumExtensions.GetEnumValueFromEnumMemberValue<RtpHeaderExtensionUri>("urn:ietf:params:rtp-hdrext:framemarking"),
                        PreferredId = 7,
                        PreferredEncrypt = false,
                        Direction = RtpHeaderExtensionDirection.SendReceive
                    },
                    new RtpHeaderExtension
                    {
                        Kind = MediaKind.AUDIO,
                        Uri = EnumExtensions.GetEnumValueFromEnumMemberValue<RtpHeaderExtensionUri>("urn:ietf:params:rtp-hdrext:ssrc-audio-level"),
                        PreferredId = 10,
                        PreferredEncrypt = false,
                        Direction = RtpHeaderExtensionDirection.SendReceive
                    },
                    new RtpHeaderExtension
                    {
                        Kind = MediaKind.VIDEO,
                        Uri = EnumExtensions.GetEnumValueFromEnumMemberValue<RtpHeaderExtensionUri>("urn:3gpp:video-orientation"),
                        PreferredId = 11,
                        PreferredEncrypt = false,
                        Direction = RtpHeaderExtensionDirection.SendReceive
                    },
                    new RtpHeaderExtension
                    {
                        Kind = MediaKind.VIDEO,
                        Uri = EnumExtensions.GetEnumValueFromEnumMemberValue<RtpHeaderExtensionUri>("urn:ietf:params:rtp-hdrext:toffset"),
                        PreferredId = 12,
                        PreferredEncrypt = false,
                        Direction = RtpHeaderExtensionDirection.SendReceive
                    },
                }.ToArray(),
            };
        }

        public static RtpCapabilities GenerateNativeRtpCapabilities()
        {
            return new RtpCapabilities
            {
                Codecs = new List<RtpCodecCapability> {
                    new RtpCodecCapability{
                        MimeType = "audio/opus",
                        Kind = MediaKind.AUDIO,
                        PreferredPayloadType = 111,
                        ClockRate = 48000,
                        Channels = 2,
                        RtcpFeedback = new List<RtcpFeedback>{
                            new RtcpFeedback{
                                Type = "transport-cc"
                            }
                        },
                        Parameters = new Dictionary<string, object> {
                            { "minptime", 10 },
                            {"useinbandfec", 1 }
                        }
                    },
                    new RtpCodecCapability{
                        MimeType = "audio/ISAC",
                        Kind = MediaKind.AUDIO,
                        PreferredPayloadType = 103,
                        ClockRate = 16000,
                        Channels = 1,
                        RtcpFeedback = new List<RtcpFeedback>{
                            new RtcpFeedback{
                                Type = "transport-cc"
                            }
                        },
                        Parameters = new Dictionary<string, object> {}
                    },
                    new RtpCodecCapability{
                        MimeType = "audio/CN",
                        Kind = MediaKind.AUDIO,
                        PreferredPayloadType = 106,
                        ClockRate = 32000,
                        Channels = 1,
                        RtcpFeedback = new List<RtcpFeedback>{
                            new RtcpFeedback{
                                Type = "transport-cc"
                            }
                        },
                        Parameters = new Dictionary<string, object> {}
                    },
                    new RtpCodecCapability{
                        MimeType = "video/VP8",
                        Kind = MediaKind.VIDEO,
                        PreferredPayloadType = 96,
                        ClockRate = 90000,
                        Channels = 2,
                        RtcpFeedback = new List<RtcpFeedback>{
                            new RtcpFeedback{
                                Type = "transport-cc"
                            },
                            new RtcpFeedback{
                                Type = "goog-remb"
                            },
                            new RtcpFeedback{
                                Type = "ccm", Parameter = "fir"
                            },
                            new RtcpFeedback{
                                Type = "nack"
                            },
                            new RtcpFeedback{
                                Type = "nack", Parameter = "pli"
                            }
                        },
                        Parameters = new Dictionary<string, object> {
                            { "baz", "1234abcd" },
                        }
                    },
                    new RtpCodecCapability{
                        MimeType = "audio/rtx",
                        Kind = MediaKind.VIDEO,
                        PreferredPayloadType = 97,
                        ClockRate = 90000,
                        Channels = 2,
                        RtcpFeedback = new List<RtcpFeedback>{

                        },
                        Parameters = new Dictionary<string, object> {
                            { "apt", 96 },
                        }
                    },
                },
                HeaderExtensions = new List<RtpHeaderExtension> {

                    new RtpHeaderExtension
                    {
                        Kind = MediaKind.AUDIO,
                        Uri = EnumExtensions.GetEnumValueFromEnumMemberValue<RtpHeaderExtensionUri>("urn:ietf:params:rtp-hdrext:sdes:mid"),
                        PreferredId = 1,
                    },
                    new RtpHeaderExtension
                    {
                        Kind = MediaKind.VIDEO,
                        Uri = EnumExtensions.GetEnumValueFromEnumMemberValue<RtpHeaderExtensionUri>("urn:ietf:params:rtp-hdrext:sdes:mid"),
                        PreferredId = 1,
                    },
                    new RtpHeaderExtension
                    {
                        Kind = MediaKind.VIDEO,
                        Uri = EnumExtensions.GetEnumValueFromEnumMemberValue<RtpHeaderExtensionUri>("urn:ietf:params:rtp-hdrext:toffset"),
                        PreferredId = 1,
                    },
                    new RtpHeaderExtension
                    {
                        Kind = MediaKind.VIDEO,
                        Uri = EnumExtensions.GetEnumValueFromEnumMemberValue<RtpHeaderExtensionUri>("http://www.webrtc.org/experiments/rtp-hdrext/abs-send-time"),
                        PreferredId = 1,
                    },
                    new RtpHeaderExtension
                    {
                        Kind = MediaKind.VIDEO,
                        Uri = EnumExtensions.GetEnumValueFromEnumMemberValue<RtpHeaderExtensionUri>("urn:3gpp:video-orientation"),
                        PreferredId = 1,
                    },
                    new RtpHeaderExtension
                    {
                        Kind = MediaKind.VIDEO,
                        Uri = EnumExtensions.GetEnumValueFromEnumMemberValue<RtpHeaderExtensionUri>("http://www.ietf.org/id/draft-holmer-rmcat-transport-wide-cc-extensions-01"),
                        PreferredId = 1,
                    },
                    new RtpHeaderExtension
                    {
                        Kind = MediaKind.VIDEO,
                        Uri = EnumExtensions.GetEnumValueFromEnumMemberValue<RtpHeaderExtensionUri>("http://www.webrtc.org/experiments/rtp-hdrext/playout-delay"),
                        PreferredId = 1,
                    },
                    new RtpHeaderExtension
                    {
                        Kind = MediaKind.VIDEO,
                        Uri = EnumExtensions.GetEnumValueFromEnumMemberValue<RtpHeaderExtensionUri>("http://www.webrtc.org/experiments/rtp-hdrext/video-content-type"),
                        PreferredId = 1,
                    },

                    new RtpHeaderExtension
                    {
                        Kind = MediaKind.VIDEO,
                        Uri = EnumExtensions.GetEnumValueFromEnumMemberValue<RtpHeaderExtensionUri>("http://www.webrtc.org/experiments/rtp-hdrext/video-timing"),
                        PreferredId = 1,
                    },

                    new RtpHeaderExtension
                    {
                        Kind = MediaKind.VIDEO,
                        Uri = EnumExtensions.GetEnumValueFromEnumMemberValue<RtpHeaderExtensionUri>("urn:ietf:params:rtp-hdrext:ssrc-audio-level"),
                        PreferredId = 1,
                    },
                }.ToArray()
            };
        }

        public static SctpCapabilities GenerateNativeSctpCapabilities()
        {

            return new SctpCapabilities
            {
                numStreams = new NumSctpStreams
                {
                    OS = 2048,
                    MIS = 2048
                }
            };
        }

        public static DtlsParameters GenerateLocalDtlsParameters()
        {
            return new DtlsParameters
            {
                fingerprints = new List<DtlsFingerprint> {
                    new DtlsFingerprint{
                        algorithm = FingerPrintAlgorithm.sha256,
                        value = "82:5A:68:3D:36:C3:0A:DE:AF:E7:32:43:D2:88:83:57:AC:2D:65:E5:80:C4:B6:FB:AF:1A:A0:21:9F:6D:0C:AD"
                    }
                },
                role = DtlsRole.auto
            };
        }

        public static ConsumerOptions GenerateConsumerRemoteParameters(string id, string codecMimeType)
        {
            switch (codecMimeType)
            {

                case "audio/opus":
                    {
                        return new ConsumerOptions
                        {
                            id = id != null ? id : GenerateFakeUuid(),
                            producerId = GenerateFakeUuid(),
                            kind = "audio",
                            rtpParameters = new RtpParameters
                            {
                                Codecs = new List<RtpCodecParameters> {
                                    new RtpCodecParameters {
                                        MimeType = "audio/opus",
                                        PayloadType = 100,
                                        ClockRate = 48000,
                                        Channels = 2,
                                        RtcpFeedback = new List<RtcpFeedback> {
                                            new RtcpFeedback {
                                                Type = "transport-cc"
                                            }
                                        },
                                        Parameters = new Dictionary<string, object> {
                                            { "useinbandfec", 1 },
                                            { "foo", "bar" }
                                        }
                                    },
                                },
                                Encodings = new List<RtpEncodingParameters> {
                                    new RtpEncodingParameters{

                                        Ssrc = 46687003
                                    }
                                },
                                HeaderExtensions = new List<RtpHeaderExtensionParameters> {
                                    new RtpHeaderExtensionParameters{
                                        Uri = RtpHeaderExtensionUri.Mid,
                                        Id = 1
                                    },
                                    new RtpHeaderExtensionParameters{
                                        Uri = RtpHeaderExtensionUri.TransportWideCcDraft01,
                                        Id = 5
                                    },
                                    new RtpHeaderExtensionParameters{
                                        Uri = RtpHeaderExtensionUri.AudioLevel,
                                        Id = 10
                                    }
                                },
                                Rtcp = new RtcpParameters
                                {
                                    CNAME = "wB4Ql4lrsxYLjzuN",
                                    ReducedSize = true,
                                    mux = true,
                                }
                            },
                        };
                    }

                case "audio/ISAC":
                    {
                        return new ConsumerOptions
                        {
                            id = id != null ? id : GenerateFakeUuid(),
                            producerId = GenerateFakeUuid(),
                            kind = "audio",
                            rtpParameters = new RtpParameters
                            {
                                Codecs = new List<RtpCodecParameters> {
                                    new RtpCodecParameters {
                                        MimeType = "audio/ISAC",
                                        PayloadType = 111,
                                        ClockRate = 16000,
                                        Channels = 1,
                                        RtcpFeedback = new List<RtcpFeedback> {
                                            new RtcpFeedback {
                                                Type = "transport-cc"
                                            }
                                        },
                                        Parameters = new Dictionary<string, object> {
                                        }
                                    },
                                },
                                Encodings = new List<RtpEncodingParameters> {
                                    new RtpEncodingParameters{

                                        Ssrc = 46687004
                                    }
                                },
                                HeaderExtensions = new List<RtpHeaderExtensionParameters> {
                                    new RtpHeaderExtensionParameters{
                                        Uri = RtpHeaderExtensionUri.Mid,
                                        Id = 1
                                    },
                                    new RtpHeaderExtensionParameters{
                                        Uri = RtpHeaderExtensionUri.TransportWideCcDraft01,
                                        Id = 5
                                    },
                                },
                                Rtcp = new RtcpParameters
                                {
                                    CNAME = "wB4Ql4lrsxYLjzuN",
                                    ReducedSize = true,
                                    mux = true,
                                }
                            },
                        };
                    }

                case "video/VP8":
                    {
                        return new ConsumerOptions
                        {
                            id = id != null ? id : GenerateFakeUuid(),
                            producerId = GenerateFakeUuid(),
                            kind = "video",
                            rtpParameters = new RtpParameters
                            {
                                Codecs = new List<RtpCodecParameters> {
                                    new RtpCodecParameters {
                                        MimeType = "video/VP8",
                                        PayloadType = 101,
                                        ClockRate = 90000,
                                        RtcpFeedback = new List<RtcpFeedback> {
                                            new RtcpFeedback{
                                                Type = "transport-cc"
                                            },
                                            new RtcpFeedback{
                                                Type = "goog-remb"
                                            },
                                            new RtcpFeedback{
                                                Type = "ccm", Parameter = "fir"
                                            },
                                            new RtcpFeedback{
                                                Type = "nack"
                                            },
                                            new RtcpFeedback{
                                                Type = "nack", Parameter = "pli"
                                            }
                                        },
                                        Parameters = new Dictionary<string, object> {
                                            { "x-google-start-bitrate", 1500 },
                                            //{ "foo", "bar" }
                                        }
                                    },
                                    new RtpCodecParameters{
                                        MimeType = "video/rtx",
                                        PayloadType = 102,
                                        ClockRate = 90000,
                                        RtcpFeedback = new List<RtcpFeedback>{
                                        },
                                        Parameters = new Dictionary<string, object>{
                                            { "apt", 101 }
                                        }
                                    }
                                },
                                Encodings = new List<RtpEncodingParameters> {
                                    new RtpEncodingParameters{

                                        Ssrc = 99991111,
                                        Rtx = new Rtx{
                                            Ssrc = 99991112
                                        }
                                    }
                                },
                                HeaderExtensions = new List<RtpHeaderExtensionParameters> {
                                    new RtpHeaderExtensionParameters{
                                        Uri = RtpHeaderExtensionUri.Mid,
                                        Id = 1
                                    },
                                    new RtpHeaderExtensionParameters{
                                        Uri = RtpHeaderExtensionUri.AbsSendTime,
                                        Id = 4
                                    },
                                    new RtpHeaderExtensionParameters{
                                        Uri = RtpHeaderExtensionUri.TransportWideCcDraft01,
                                        Id = 5
                                    },
                                    new RtpHeaderExtensionParameters{
                                        Uri = RtpHeaderExtensionUri.VideoOrientation,
                                        Id = 11,
                                    },
                                    new RtpHeaderExtensionParameters{
                                        Uri = RtpHeaderExtensionUri.TimeOffset,
                                        Id = 12
                                    }
                                },
                                Rtcp = new RtcpParameters
                                {
                                    CNAME = "wB4Ql4lrsxYLjzuN",
                                    ReducedSize = true,
                                    mux = true,
                                }
                            },
                        };
                    }

                case "video/H264":
                    {
                        return new ConsumerOptions
                        {
                            id = id != null ? id : GenerateFakeUuid(),
                            producerId = GenerateFakeUuid(),
                            kind = "video",
                            rtpParameters = new RtpParameters
                            {
                                Codecs = new List<RtpCodecParameters> {
                                    new RtpCodecParameters {
                                        MimeType = "video/H264",
                                        PayloadType = 103,
                                        ClockRate = 90000,
                                        RtcpFeedback = new List<RtcpFeedback> {
                                            new RtcpFeedback{
                                                Type = "transport-cc"
                                            },
                                            new RtcpFeedback{
                                                Type = "goog-remb"
                                            },
                                            new RtcpFeedback{
                                                Type = "ccm", Parameter = "fir"
                                            },
                                            new RtcpFeedback{
                                                Type = "nack"
                                            },
                                            new RtcpFeedback{
                                                Type = "nack", Parameter = "pli"
                                            }
                                        },
                                        Parameters = new Dictionary<string, object> {

                                            { "level-asymmetry-allowed",  1 },
                                            {  "packetization-mode" , 1 },
                                            {  "profile-level-id", "42e01f" },
                                            //{ "foo", "bar" }
                                        }
                                    },
                                    new RtpCodecParameters{
                                        MimeType = "video/rtx",
                                        PayloadType = 104,
                                        ClockRate = 90000,
                                        RtcpFeedback = new List<RtcpFeedback>{
                                        },
                                        Parameters = new Dictionary<string, object>{
                                            { "apt", 103 }
                                        }
                                    }
                                },
                                Encodings = new List<RtpEncodingParameters> {
                                    new RtpEncodingParameters{

                                        Ssrc = 99991113,
                                        Rtx = new Rtx{
                                            Ssrc = 99991114
                                        }
                                    }
                                },
                                HeaderExtensions = new List<RtpHeaderExtensionParameters> {
                                    new RtpHeaderExtensionParameters{
                                        Uri = RtpHeaderExtensionUri.Mid,
                                        Id = 1
                                    },
                                    new RtpHeaderExtensionParameters{
                                        Uri = RtpHeaderExtensionUri.AbsSendTime,
                                        Id = 4
                                    },
                                    new RtpHeaderExtensionParameters{
                                        Uri = RtpHeaderExtensionUri.TransportWideCcDraft01,
                                        Id = 5
                                    },
                                    new RtpHeaderExtensionParameters{
                                        Uri = RtpHeaderExtensionUri.VideoOrientation,
                                        Id = 11,
                                    },
                                    new RtpHeaderExtensionParameters{
                                        Uri = RtpHeaderExtensionUri.TimeOffset,
                                        Id = 12
                                    }
                                },
                                Rtcp = new RtcpParameters
                                {
                                    CNAME = "wB4Ql4lrsxYLjzuN",
                                    ReducedSize = true,
                                    mux = true,
                                }
                            },
                        };
                    }

                default:
                    {
                        throw new Exception("unknown codemimetype " + codecMimeType);
                    }
            }

        }

        public static Dictionary<string, object> GenerateDataProducerRemoteParameters() {
            return new Dictionary<string, object> { { "id" , GenerateFakeUuid()} };
        }

        public static DataConsumerOptions GenerateDataConsumerRemoteParameters(string id) {
            return new DataConsumerOptions
            {
                id = id == null ? GenerateFakeUuid() : id,
                datProducerId = GenerateFakeUuid(),
                sctpStreamParameters = new SctpStreamParameters {
                    streamId = 666,
                    maxPacketLifeTime = 5000,
                    maxRetransmits = null
                }
            };
        }


    }

}

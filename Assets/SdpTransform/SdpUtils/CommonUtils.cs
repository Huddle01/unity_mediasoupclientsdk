using Mediasoup.RtpParameter;
using System.Collections.Generic;
using Utilme.SdpTransform;
using System.Linq;
using Mediasoup.Transports;
using System;
using System.Text;

public class CommonUtils
{
    public static RtpCapabilities ExtractRtpCapabilities(Sdp sdp)
    {
        Dictionary<byte, RtpCodecCapability> codecsMap = new Dictionary<byte, RtpCodecCapability>();
        List<RtpHeaderExtension> headerExtensions = new List<RtpHeaderExtension>();

        bool gotAudio = false;
        bool gotVideo = false;

        foreach (var mediaDes in sdp.MediaDescriptions)
        {
            MediaType mediaType = mediaDes.Media;

            MediaKind mediaKind;


            switch (mediaDes.Media)
            {
                case MediaType.Audio:
                    if (gotAudio) continue;
                    gotAudio = true;
                    mediaKind = MediaKind.AUDIO;
                    break;

                case MediaType.Video:
                    if (gotVideo) continue;
                    gotVideo = true;
                    mediaKind = MediaKind.VIDEO;
                    break;

                default:
                    continue;
            }

            //Get Codecs
            foreach (var rtp in mediaDes.Attributes.Rtpmaps)
            {
                RtpCodecCapability codecCap = new RtpCodecCapability
                {
                    Kind = mediaKind,
                    MimeType = rtp.EncodingName,
                    ClockRate = rtp.ClockRate,
                    PreferredPayloadType = rtp.PayloadType
                };

                if (rtp.Channels != null) codecCap.Channels = rtp.Channels.Value;

                codecsMap.Add(codecCap.PreferredPayloadType.Value, codecCap);
            }

            //Get codec Parameters
            foreach (var fmtp in mediaDes.Attributes.Fmtps)
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>();

                string[] configPayload = fmtp.Value.Split(" ");
                string[] paramList = configPayload[1].Split(';');

                foreach (string pair in paramList)
                {
                    var keyValue = pair.Split('=');

                    if (keyValue.Length == 2)
                    {
                        // Add to the dictionary
                        parameters.Add(keyValue[0].Trim(), keyValue[1].Trim());
                    }
                }

                RtpCodecCapability codec;

                if (codecsMap.TryGetValue(fmtp.PayloadType, out codec))
                {
                    codecsMap[fmtp.PayloadType].Parameters = parameters;
                }
            }

            // Get RTCP feedback for each codec.

            foreach (var fb in mediaDes.Attributes.RtcpFbs)
            {
                RtcpFeedback feedback = new RtcpFeedback
                {
                    Parameter = fb.SubType,
                    Type = fb.Type
                };

                if (codecsMap.ContainsKey(fb.PayloadType))
                {
                    if (codecsMap[fb.PayloadType] != null)
                    {
                        continue;
                    }

                    codecsMap[fb.PayloadType].RtcpFeedback.Add(feedback);
                }
            }

            // Get RTP header extensions.
            foreach (var ext in mediaDes.Attributes.Extmaps)
            {
                try
                {
                    RtpHeaderExtension headerExt = new RtpHeaderExtension
                    {
                        Kind = mediaKind,
                        Uri = EnumExtensions.GetEnumValueFromEnumMemberValue<RtpHeaderExtensionUri>(ext.Uri.AbsoluteUri),
                        PreferredId = ext.Value
                    };


                    headerExtensions.Add(headerExt);

                }
                catch (ArgumentException err) { 
                    Console.WriteLine("CommonUtils: Failed to get enum from value", err.Message);
                }
            }
        }

        RtpCapabilities rtpCapabilities = new RtpCapabilities
        {
            Codecs = codecsMap.Values.ToList(),
            HeaderExtensions = headerExtensions.ToArray()
        };

        return rtpCapabilities;
    }

    public static DtlsParameters ExtractDtlsParameters(Sdp sdp)
    {
        Setup setup = sdp.Attributes.Setup;
        Fingerprint fingerPrint = sdp.Attributes.Fingerprint;

        if (setup == null || fingerPrint == null)
        {
            MediaDescription mediaObject = null;

            if (sdp.MediaDescriptions != null && sdp.MediaDescriptions.Count > 0)
            {
                mediaObject = sdp.MediaDescriptions.FirstOrDefault<MediaDescription>(x => x.Port != 0);
            }

            if (mediaObject != null)
            {
                setup ??= mediaObject.Attributes.Setup;
                fingerPrint ??= mediaObject.Attributes.Fingerprint;
            }
        }

        if (setup == null)
        {
            throw new Exception("no a=setup found at SDP session or media level");
        }
        else if (fingerPrint == null)
        {
            throw new Exception("no a=fingerprint found at SDP session or media level");
        }

        DtlsRole role = DtlsRole.auto;

        switch (setup.Role.GetStringValue())
        {
            case "active":
                role = DtlsRole.client;
                break;


            case "passive":
                role = DtlsRole.server;
                break;

            case "actpass":
                role = DtlsRole.auto;
                break;
        }

        List<DtlsFingerprint> fingerPrintLIst = new List<DtlsFingerprint>();
        DtlsFingerprint finger = new DtlsFingerprint();

        switch (fingerPrint.HashFunction)
        {
            case HashFunction.Sha1:
                finger.algorithm = FingerPrintAlgorithm.sha1;
                break;

            case HashFunction.Sha224:
                finger.algorithm = FingerPrintAlgorithm.sha224;
                break;

            case HashFunction.Sha256:
                finger.algorithm = FingerPrintAlgorithm.sha256;
                break;

            case HashFunction.Sha384:
                finger.algorithm = FingerPrintAlgorithm.sha384;
                break;

            case HashFunction.Sha512:
                finger.algorithm = FingerPrintAlgorithm.sha512;
                break;
        }

        finger.value = Encoding.UTF8.GetString(fingerPrint.HashValue);

        fingerPrintLIst.Add(finger);

        DtlsParameters dtlsParams = new DtlsParameters
        {
            role = role,
            fingerprints = fingerPrintLIst
        };

        return dtlsParams;

    }

    public static string GetCName(MediaDescription offerMediaObject)
    {
        Ssrc ssrc = null;

        if (offerMediaObject != null && offerMediaObject.Attributes.Ssrcs.Count > 0)
        {
            ssrc = offerMediaObject.Attributes.Ssrcs.FirstOrDefault<Ssrc>(x => x.Attribute.ToLower() == "cname");
        }

        if (ssrc == null)
        {
            return "";
        }

        return ssrc.Value;
    }

    public static void ApplyCodecParameters(RtpParameters offerRtpParameters, MediaDescription answerMediaObject)
    {
        foreach (var codec in offerRtpParameters.Codecs)
        {
            string mimeType = codec.MimeType.ToLower();

            if (mimeType != "audio/opus")
            {
                continue;
            }

            Rtpmap rtp = null;
            if (answerMediaObject.Attributes.Rtpmaps != null && answerMediaObject.Attributes.Rtpmaps.Count > 0)
            {
                rtp = answerMediaObject.Attributes.Rtpmaps.FirstOrDefault<Rtpmap>(f => f.PayloadType == codec.PayloadType);
            }

            if (rtp == null) continue;

            Fmtp fmtp = null;

            if (answerMediaObject.Attributes.Fmtps == null)
            {
                answerMediaObject.Attributes.Fmtps = new List<Fmtp>();
            }

            fmtp = answerMediaObject.Attributes.Fmtps.FirstOrDefault<Fmtp>(f => f.PayloadType == codec.PayloadType);

            answerMediaObject.Attributes.Fmtps.Add(fmtp);

            Dictionary<string, object> fmtpParameters = new Dictionary<string, object>();

            string[] configPayload = fmtp.Value.Split(" ");
            string[] paramList = configPayload[1].Split(';');

            foreach (string pair in paramList)
            {
                var keyValue = pair.Split('=');

                if (keyValue.Length == 2)
                {
                    // Add to the dictionary
                    fmtpParameters.Add(keyValue[0].Trim(), keyValue[1].Trim());
                }
            }

            switch (mimeType)
            {
                case "audio/opus":

                    object spropStereo = codec.Parameters["sprop-stereo"];

                    if (spropStereo != null)
                    {
                        fmtpParameters["stereo"] = spropStereo;
                    }
                    break;

            }

            string fmtpConfig = configPayload[0] + " ";

            foreach (var par in fmtpParameters)
            {
                fmtpConfig += par.Key + "=" + par.Value + ";";
            }

            fmtp.Value = fmtpConfig;
        }
    }
}

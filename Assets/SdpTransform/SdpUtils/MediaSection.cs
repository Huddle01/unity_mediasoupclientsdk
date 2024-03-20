using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mediasoup.Transports;
using Mediasoup.RtpParameter;
using Mediasoup.SctpParameter;
using Utilme.SdpTransform;
using System.Text;
using System;
using Mediasoup;
using Unity.WebRTC;
using System.Linq;
using System.Text.RegularExpressions;
using UtilmeSdpTransform;
using Newtonsoft.Json;

public class MediaSection
{

    public bool planB;

    public MediaDescription _mediaObject;

    public bool isClosed { get { return _mediaObject.Port == 0; } }
    public string mid { get { return _mediaObject.Attributes.Mid.Id; } }

    public MediaSection(IceParameters _iceParameters, List<IceCandidate> _iceCandidates, DtlsParameters _dtlsParameters, bool _planB)
    {
        _mediaObject = new MediaDescription();
        _mediaObject.Attributes = new Attributes();
        _mediaObject.Fmts = new List<string>();
        planB = _planB;

        if (_iceParameters != null) SetIceParameters(_iceParameters);

        if (_iceCandidates != null && _iceCandidates.Count > 0)
        {
            _mediaObject.Attributes.Candidates = new List<Candidate>();

            foreach (IceCandidate candi in _iceCandidates)
            {
                Candidate candidateObject = new Candidate();
                candidateObject.ComponentId = 1;
                candidateObject.Foundation = candi.foundation;
                // Be ready for new candidate.address field in mediasoup server side
                // field and keep backward compatibility with deprecated candidate.ip.
                candidateObject.ConnectionAddress = candi.address ?? candi.ip;
                candidateObject.Port = candi.port;
                candidateObject.Priority = candi.priority;
                if (candi.protocol.ToLower().Contains("u"))
                {
                    candidateObject.Transport = CandidateTransport.Udp;
                }
                else if (candi.protocol.ToLower().Contains("t"))
                {
                    candidateObject.Transport = CandidateTransport.Tcp;
                }

                candidateObject.Type = IceCandidateToCandidateType(candi.type);

                if (!string.IsNullOrEmpty(candi.tcpType))
                {
                    candidateObject.TCPType = candi.tcpType;
                }

                _mediaObject.Attributes.Candidates.Add(candidateObject);

            }

            _mediaObject.Attributes.IceOptions = new IceOptions { Tags = Enumerable.Repeat("renomination", 1).ToArray() };

            if (_dtlsParameters != null) { SetDtlsRole(_dtlsParameters.role); }

        }

    }

    public virtual void SetDtlsRole(DtlsRole _dtlsRole)
    {

    }

    public MediaDescription GetObject()
    {
        return _mediaObject;
    }

    public void SetIceParameters(IceParameters _iceParameters)
    {
        Debug.Log(_mediaObject is null);
        Debug.Log(_mediaObject.Attributes is null);
        _mediaObject.Attributes.IceUfrag = new IceUfrag();
        _mediaObject.Attributes.IcePwd = new IcePwd();
        _mediaObject.Attributes.IceUfrag.Ufrag = _iceParameters.usernameFragment;
        _mediaObject.Attributes.IcePwd.Password = _iceParameters.password;
    }

    public void Pause()
    {
        _mediaObject.Direction = "inactive";
    }

    public virtual void Resume()
    {

    }

    public void PlanBReceive(RtpParameters offerRtp, string streamId, string trackId)
    {
        throw new NotImplementedException();
    }


    public void Disable()
    {
        Pause();
        _mediaObject.Attributes.Extmaps = null;
        _mediaObject.Attributes.Ssrcs = null;
        _mediaObject.Attributes.SsrcGroups = null;
        _mediaObject.Attributes.Simulcast = null;
        _mediaObject.Attributes.Simulcast = null;
        _mediaObject.Attributes.Rids = null;
        _mediaObject.Attributes.ExtmapAllowMixed = false;
    }

    public void Closed()
    {
        Disable();
        _mediaObject.Port = 0;
    }

    public string GetCodecName(RtpCodecParameters codec)
    {
        const string MimeTypePattern = @"^(audio|video)/(.+)";
        Regex MimeTypeRegex = new Regex(MimeTypePattern, RegexOptions.IgnoreCase);

        Match mimeTypeMatch = MimeTypeRegex.Match(codec.MimeType);

        if (!mimeTypeMatch.Success)
        {
            throw new InvalidOperationException("Invalid codec.MimeType");
        }

        return mimeTypeMatch.Groups[2].Value;
    }


    private CandidateType IceCandidateToCandidateType(string value)
    {
        value = value.ToLower();
        if (value.Contains("host"))
        {
            return CandidateType.Host;
        }
        else if (value.Contains("prflx"))
        {
            return CandidateType.Prflx;
        }
        else if (value.Contains("relay"))
        {
            return CandidateType.Relay;
        }
        else if (value.Contains("srflx"))
        {
            return CandidateType.Srflx;
        }

        return CandidateType.Relay;
    }
}

public class AnswerMediaSection : MediaSection
{
    public AnswerMediaSection(IceParameters _iceParameters, List<IceCandidate> _iceCandidates, DtlsParameters _dtlsParameters,
                              SctpParameters _sctpParameters, PlainRtpParameters _plainRtpParameters, bool _planB, object _offerMediaObject,
                              RtpParameters _offerRtp, RtpParameters _answerRtp, ProducerCodecOptions _codecOptions, bool _extmapAllowMixed) :
                                base(_iceParameters, _iceCandidates, _dtlsParameters, _planB)
    {
        if (_offerMediaObject is MediaDescription)
        {
            MediaDescription tempMediaDes = _offerMediaObject as MediaDescription;
            _mediaObject.Attributes.Mid = tempMediaDes.Attributes.Mid;
            _mediaObject.Media = tempMediaDes.Media;
            _mediaObject.Attributes.Mid = tempMediaDes.Attributes.Mid;

            //this._mediaObject.type = offerMediaObject.type;
            //this._mediaObject.protocol = offerMediaObject.protocol;

            if (_plainRtpParameters == null)
            {
                _mediaObject.ConnectionData = new ConnectionData { ConnectionAddress = "127.0.0.0" };
                _mediaObject.Port = 7;
                _mediaObject.Proto = tempMediaDes.Proto;
            }
            else
            {
                AddrType tempAdr = _plainRtpParameters.ipVersion.Contains("4") ? AddrType.Ip4 : AddrType.Ip6;
                _mediaObject.ConnectionData = new ConnectionData { ConnectionAddress = _plainRtpParameters.ip, AddrType = tempAdr };
                _mediaObject.Port = _plainRtpParameters.port;
            }

            switch (tempMediaDes.Media)
            {
                case MediaType.Audio:
                case MediaType.Video:
                    _mediaObject.Direction = "recvonly";
                    _mediaObject.Attributes.RecvOnly = true;
                    _mediaObject.Attributes.Rtpmaps = new List<Rtpmap>();
                    _mediaObject.Attributes.RtcpFbs = new List<RtcpFb>();
                    _mediaObject.Attributes.Fmtps = new List<Fmtp>();
                    _mediaObject.ExtraParam = new List<string>();

                    if (_answerRtp != null)
                    {
                        foreach (var codec in _answerRtp.Codecs)
                        {
                            Rtpmap rtp = new Rtpmap
                            {
                                PayloadType = codec.PayloadType,
                                EncodingName = GetCodecName(codec),
                                ClockRate = codec.ClockRate
                            };

                            if (codec.Channels > 1)
                            {
                                rtp.Channels = codec.Channels;
                            }

                            _mediaObject.Attributes.Rtpmaps.Add(rtp);


                            Dictionary<string, object> codecParameters;
                            if (codec.Parameters != null)
                            {
                                codecParameters = new Dictionary<string, object>(codec.Parameters);
                            }
                            else
                            {
                                codecParameters = new Dictionary<string, object>();
                            }

                            List<RtcpFeedback> codecRtcpFeedback;
                            if (codec.RtcpFeedback != null)
                            {
                                codecRtcpFeedback = codec.RtcpFeedback;
                            }
                            else
                            {
                                codecRtcpFeedback = new List<RtcpFeedback>();
                            }

                            if (_codecOptions != null)
                            {
                                RtpCodecParameters offerCodec = _offerRtp!.Codecs.Find(x => x.PayloadType == codec.PayloadType);

                                Debug.Log("MediaSection | offerCodec : " + JsonConvert.SerializeObject(offerCodec));

                                switch (codec.MimeType.ToLower())
                                {
                                    case "audio/opus":
                                    case "audio/multiopus":

                                        //
                                        if (_codecOptions.opusStereo != null)
                                        {
                                            if (!offerCodec.Parameters.ContainsKey("sprop-stereo"))
                                            {
                                                offerCodec.Parameters.Add("sprop-stereo", _codecOptions.opusStereo.Value ? 1 : 0);
                                            }
                                            else
                                            {
                                                offerCodec.Parameters["sprop-stereo"] = _codecOptions.opusStereo.Value ? 1 : 0;
                                            }

                                            if (!codecParameters.ContainsKey("stereo"))
                                            {
                                                codecParameters.Add("stereo", _codecOptions.opusStereo.Value ? 1 : 0);
                                            }
                                            else
                                            {
                                                codecParameters["stereo"] = _codecOptions.opusStereo.Value ? 1 : 0;
                                            }
                                        }


                                        //////

                                        if (_codecOptions.opusFec != null)
                                        {
                                            if (!offerCodec.Parameters.ContainsKey("useinbandfec"))
                                            {
                                                offerCodec.Parameters.Add("useinbandfec", _codecOptions.opusFec.Value ? 1 : 0);
                                            }
                                            else
                                            {
                                                offerCodec.Parameters["useinbandfec"] = _codecOptions.opusFec.Value ? 1 : 0;
                                            }

                                            if (!codecParameters.ContainsKey("useinbandfec"))
                                            {
                                                codecParameters.Add("useinbandfec", _codecOptions.opusFec.Value ? 1 : 0);
                                            }
                                            else
                                            {
                                                codecParameters["useinbandfec"] = _codecOptions.opusFec.Value ? 1 : 0;
                                            }
                                        }


                                        ////////
                                        if (_codecOptions.opusDtx != null)
                                        {
                                            if (!offerCodec.Parameters.ContainsKey("usedtx"))
                                            {
                                                offerCodec.Parameters.Add("usedtx", _codecOptions.opusDtx.Value ? 1 : 0);
                                            }
                                            else
                                            {
                                                offerCodec.Parameters["usedtx"] = _codecOptions.opusDtx.Value ? 1 : 0;
                                            }

                                            if (!codecParameters.ContainsKey("usedtx"))
                                            {
                                                codecParameters.Add("usedtx", _codecOptions.opusDtx.Value ? 1 : 0);
                                            }
                                            else
                                            {
                                                codecParameters["usedtx"] = _codecOptions.opusDtx.Value ? 1 : 0;
                                            }
                                        }

                                        //////

                                        if (_codecOptions.opusMaxPlaybackRate != null)
                                        {
                                            if (!codecParameters.ContainsKey("maxplaybackrate"))
                                            {
                                                codecParameters.Add("maxplaybackrate", _codecOptions.opusMaxPlaybackRate.Value);
                                            }
                                            else
                                            {
                                                codecParameters["maxplaybackrate"] = _codecOptions.opusMaxPlaybackRate.Value;
                                            }
                                        }
                                        //
                                        if (_codecOptions.opusMaxAverageBitrate != null)
                                        {
                                            if (!codecParameters.ContainsKey("maxaveragebitrate"))
                                            {
                                                codecParameters.Add("maxaveragebitrate", _codecOptions.opusMaxAverageBitrate.Value);
                                            }
                                            else
                                            {
                                                codecParameters["maxaveragebitrate"] = _codecOptions.opusMaxAverageBitrate.Value;
                                            }
                                        }
                                        //
                                        if (_codecOptions.opusPtime != null)
                                        {
                                            if (!offerCodec.Parameters.ContainsKey("ptime"))
                                            {
                                                offerCodec.Parameters.Add("ptime", _codecOptions.opusPtime.Value);
                                            }
                                            else
                                            {
                                                offerCodec.Parameters["ptime"] = _codecOptions.opusPtime.Value;
                                            }

                                            if (!codecParameters.ContainsKey("ptime"))
                                            {
                                                codecParameters.Add("ptime", _codecOptions.opusPtime.Value);
                                            }
                                            else
                                            {
                                                codecParameters["ptime"] = _codecOptions.opusPtime.Value;
                                            }
                                        }

                                        // If opusNack is not set, we must remove NACK support for OPUS.
                                        // Otherwise it would be enabled for those handlers that artificially
                                        // announce it in their RTP capabilities.

                                        if (_codecOptions.opusNack != null)
                                        {
                                            if (offerCodec != null && offerCodec.RtcpFeedback != null)
                                            {
                                                offerCodec.RtcpFeedback = offerCodec.RtcpFeedback
                                                    .Where(fb => fb.Type != "nack" || fb.Parameter != null)
                                                    .ToList();
                                            }

                                            codecRtcpFeedback = codecRtcpFeedback.
                                                Where(fb => fb.Type != "nack" || fb.Parameter != null).
                                                ToList();
                                        }

                                        break;

                                    case "video/vp8":
                                    case "video/vp9":
                                    case "video/h264":
                                    case "video/h265":

                                        if (_codecOptions.videoGoogleStartBitrate != null)
                                        {
                                            if (!codecParameters.ContainsKey("x-google-start-bitrate"))
                                            {
                                                codecParameters.Add("x-google-start-bitrate", _codecOptions.videoGoogleStartBitrate.Value);
                                            }
                                            else
                                            {
                                                codecParameters["x-google-start-bitrate"] = _codecOptions.videoGoogleStartBitrate.Value;
                                            }
                                        }

                                        if (_codecOptions.videoGoogleMaxBitrate != null)
                                        {
                                            if (!codecParameters.ContainsKey("x-google-max-bitrate"))
                                            {
                                                codecParameters.Add("x-google-max-bitrate", _codecOptions.videoGoogleMaxBitrate.Value);
                                            }
                                            else
                                            {
                                                codecParameters["x-google-max-bitrate"] = _codecOptions.videoGoogleMaxBitrate.Value;
                                            }
                                        }

                                        if (_codecOptions.videoGoogleMinBitrate != null)
                                        {
                                            if (!codecParameters.ContainsKey("x-google-min-bitrate"))
                                            {
                                                codecParameters.Add("x-google-min-bitrate", _codecOptions.videoGoogleMinBitrate.Value);
                                            }
                                            else
                                            {
                                                codecParameters["x-google-min-bitrate"] = _codecOptions.videoGoogleMinBitrate.Value;
                                            }
                                        }

                                        break;
                                }

                                Fmtp fmtp = new Fmtp { PayloadType = codec.PayloadType, Value = "" };

                                foreach (var key in codecParameters.Keys)
                                {
                                    Debug.Log("MediaSection CodecParameter Key: " + key + $" value {codecParameters[key]}");
                                    if (!string.IsNullOrEmpty(fmtp.Value))
                                    {
                                        fmtp.Value += ";";
                                    }


                                    fmtp.Value += $"{key}={codecParameters[key]}";
                                }

                                Debug.Log($"fmtp Value: {fmtp.Value}");

                                if (!string.IsNullOrEmpty(fmtp.Value))
                                {
                                    _mediaObject.Attributes.Fmtps.Add(fmtp);
                                    _mediaObject.Fmts.Add(fmtp.PayloadType.ToString());
                                    _mediaObject.ExtraParam.Add(fmtp.PayloadType.ToString());
                                }

                                foreach (var fb in codecRtcpFeedback)
                                {
                                    _mediaObject.Attributes.RtcpFbs.Add(
                                        new RtcpFb { PayloadType = codec.PayloadType, Type = fb.Type, SubType = fb.Parameter });

                                }

                            }

                        }

                        //_mediaObject.payloads = answerRtpParameters!.codecs
                        //.map((codec: RtpCodecParameters) => codec.PayloadType)
                        //.join(' ');

                        _mediaObject.Attributes.Extmaps = new List<Extmap>();

                        if (_answerRtp != null && _answerRtp.HeaderExtensions != null)
                        {
                            foreach (var ext in _answerRtp.HeaderExtensions)
                            {
                                // Don't add a header extension if not present in the offer.
                                bool found = (tempMediaDes.Attributes.Extmaps.Any(localExt => localExt.Uri.AbsoluteUri == EnumExtensions.GetEnumMemberValue(ext.Uri)));

                                if (!found)
                                {
                                    continue;
                                }

                                _mediaObject.Attributes.Extmaps.Add(new Extmap
                                {
                                    Uri = new Uri(EnumExtensions.GetEnumMemberValue(ext.Uri)),
                                    Value = ext.Id
                                });
                            }
                        }

                        // Simulcast.
                        if (tempMediaDes.Attributes.Simulcast != null)
                        {
                            _mediaObject.Attributes.Simulcast = new Simulcast
                            {
                                Direction = RidDirection.Recv,
                                IdList = tempMediaDes.Attributes.Simulcast.IdList
                            };

                            _mediaObject.Attributes.Rids = new List<Rid>();

                            foreach (Rid rid in tempMediaDes.Attributes.Rids)
                            {
                                if (rid.Direction != RidDirection.Send)
                                    continue;

                                _mediaObject.Attributes.Rids.Add(new Rid
                                {
                                    Id = rid.Id,
                                    Direction = RidDirection.Recv,
                                });


                            }

                        }
                        else
                        {
                            Debug.Log("Implement Simulcast03"); // currently dont know what it is
                        }

                        _mediaObject.Attributes.RtcpRsize = true;
                        _mediaObject.Attributes.RtcpMux = true;

                        if (_planB && _mediaObject.Media == MediaType.Video)
                        {
                            //_mediaObject.xGoogleFlag = 'conference';
                        }


                    }

                    break;

                case MediaType.Application:
                    if (tempMediaDes.Attributes.SctpPort != null)
                    {
                        //_mediaObject.payloads = 'webrtc-datachannel';
                        if (_sctpParameters != null)
                        {
                            _mediaObject.Attributes.SctpPort.Port = _sctpParameters.port;
                            _mediaObject.Attributes.MaxMessageSize.Size = _sctpParameters.maxMessageSize;
                        }

                    }

                    break;
            }


        }
    }

    public AnswerMediaSection(IceParameters _iceParameters, List<IceCandidate> _iceCandidates, DtlsParameters _dtlsParameters,
                              SctpParameters _sctpParameters, PlainRtpParameters _plainRtpParameters, bool _planB, object _offerMediaObject) :
                                base(_iceParameters, _iceCandidates, _dtlsParameters, _planB)
    {

    }

    public void MuxSimulcastStreams(List<RTCRtpEncodingParameters> encodings)
    {
        if (_mediaObject.Attributes.Simulcast == null || _mediaObject.Attributes.Simulcast.IdList == null) return;

        Dictionary<string, RTCRtpEncodingParameters> layers = new Dictionary<string, RTCRtpEncodingParameters>();

        foreach (var encoding in encodings)
        {
            if (!string.IsNullOrEmpty(encoding.rid))
            {
                layers[encoding.rid] = encoding;
            }
        }

        string raw = string.Join(";", _mediaObject.Attributes.Simulcast.IdList);
        List<List<SimulcastFormat>> simulcastStreams = UtilityExtensions.ParseSimulcastStreamList(raw);

        foreach (var simulcastStream in simulcastStreams)
        {
            foreach (var simulcastFormat in simulcastStream)
            {
                simulcastFormat.Paused = !layers.TryGetValue(simulcastFormat.Scid.ToString(), out var layer) || !layer.active;
            }
        }

        _mediaObject.Attributes.Simulcast.IdList = simulcastStreams.Select(simulcastFormats =>
                string.Join(",", simulcastFormats.Select(f => $"{(f.Paused ? "~" : "")}{f.Scid}")))
            .ToArray();

    }

    public override void Resume()
    {
        _mediaObject.Direction = "recvonly";
        _mediaObject.Attributes.RecvOnly = true;
    }

    public override void SetDtlsRole(DtlsRole _dtlsRole)
    {
        switch (_dtlsRole)
        {
            case DtlsRole.client:
                _mediaObject.Attributes.Setup = new Setup { Role = SetupRole.Active };
                break;

            case DtlsRole.server:
                _mediaObject.Attributes.Setup = new Setup { Role = SetupRole.Active };
                break;

            case DtlsRole.auto:
                _mediaObject.Attributes.Setup = new Setup { Role = SetupRole.Active };
                break;
        }
    }




}

public class OfferMediaSection : MediaSection
{
    public OfferMediaSection(IceParameters _iceParameters, List<IceCandidate> _iceCandidates, DtlsParameters _dtlsParameters,
                              SctpParameters _sctpParameters, PlainRtpParameters _plainRtpParameters, bool _planB, string _mid,
                              MediaKind _mediaKind, RtpParameters _offerRtp, string _streamId, string _trackId, bool _oldDataChannelSpec = false) :
                                base(_iceParameters, _iceCandidates, _dtlsParameters, _planB)
    {
        Debug.Log("OfferMediaSection | cons() | ");
        if (_mediaObject == null)
        {
            Debug.Log("OfferMediaSection | cons() | _mediaObject null");
            _mediaObject = new MediaDescription();
        }
        if (_mediaObject.Attributes == null)
        {
            Debug.Log("OfferMediaSection | cons() | Attributes is null");
            _mediaObject.Attributes = new Attributes();
        }
        if (_mediaObject.Attributes.Mid == null )
        {
            Debug.Log("OfferMediaSection | cons() | Mid is null");
            _mediaObject.Attributes.Mid = new Mid();
        }
        _mediaObject.Attributes.Mid.Id = _mid;

        switch (_mediaKind)
        {
            case MediaKind.AUDIO:
                _mediaObject.Media = MediaType.Audio;
                break;

            case MediaKind.VIDEO:
                _mediaObject.Media = MediaType.Video;
                break;

            case MediaKind.APPLICATION:
                _mediaObject.Media = MediaType.Application;
                break;
        }

        Debug.Log("OfferMediaSection | cons() | media kind setup complete ");

        if (_plainRtpParameters == null)
        {
            Debug.Log("OfferMediaSection | cons() | _plainRtpParameters is null ");
            _mediaObject.ConnectionData = new ConnectionData
            {
                ConnectionAddress = "127.0.0.1",
                AddrType = AddrType.Ip4
            };

            if (_sctpParameters == null)
            {
                _mediaObject.Proto = "UDP/TLS/RTP/SAVPF";
            }
            else
            {
                _mediaObject.Proto = "UDP/DTLS/SCTP";
            }

            _mediaObject.Port = 7;
        }
        else
        {
            Debug.Log("OfferMediaSection | cons() | _plainRtpParameters is present ");
            AddrType tempAdr = _plainRtpParameters.ipVersion.Contains("4") ? AddrType.Ip4 : AddrType.Ip6;
            _mediaObject.ConnectionData = new ConnectionData
            {
                ConnectionAddress = _plainRtpParameters.ip,
                AddrType = tempAdr
            };

            _mediaObject.Proto = "RTP/AVP";
            _mediaObject.Port = _plainRtpParameters.port;
        }

        switch (_mediaKind)
        {
            case MediaKind.AUDIO:
            case MediaKind.VIDEO:
                _mediaObject.Direction = "sendonly";
                _mediaObject.Attributes.Rtpmaps = new List<Rtpmap>();
                _mediaObject.Attributes.RtcpFbs = new List<RtcpFb>();
                _mediaObject.Attributes.Fmtps = new List<Fmtp>();
                _mediaObject.Attributes.SendOnly = true;

                if (!_planB)
                {
                    _mediaObject.Attributes.Msid = new Msid
                    {
                        AppData = _streamId,
                        Id = _trackId
                    };
                }

                Debug.Log($"OfferMediaSection | cons() | _offerRtp is null? : {_offerRtp == null}");

                if (_offerRtp != null)
                {
                    foreach (var codec in _offerRtp.Codecs)
                    {
                        Rtpmap rtp = new Rtpmap
                        {
                            PayloadType = codec.PayloadType,
                            EncodingName = GetCodecName(codec),
                            ClockRate = codec.ClockRate
                        };

                        if (codec.Channels > 1)
                        {
                            rtp.Channels = codec.Channels;
                        }

                        _mediaObject.Attributes.Rtpmaps.Add(rtp);

                        Fmtp fmtp = new Fmtp { PayloadType = codec.PayloadType, Value = "" };

                        //Debug.Log($"OfferMediaSection | cons() | codec.payloadType: {codec.PayloadType}");

                        foreach (var key in codec.Parameters.Keys)
                        {
                            if (fmtp.Value.Length > 0)
                            {
                                fmtp.Value.Append(';');
                            }

                            fmtp.Value += $"{key}={codec.Parameters[key]}";
                        }

                        //Debug.Log($"OfferMediaSection | cons() | fmtp.Value is null: {string.IsNullOrEmpty(fmtp.Value)}");


                        if (!string.IsNullOrEmpty(fmtp.Value))
                        {
                            _mediaObject.Attributes.Fmtps.Add(fmtp);
                            if (_mediaObject.Fmts == null) _mediaObject.Fmts = new List<string>();
                            _mediaObject.Fmts.Add(fmtp.PayloadType.ToString());
                        }

                        //Debug.Log($"OfferMediaSection | cons() | fmtp.Value: {fmtp.Value}");

                        foreach (var fb in codec.RtcpFeedback)
                        {
                            _mediaObject.Attributes.RtcpFbs.Add(new RtcpFb
                            {
                                PayloadType = codec.PayloadType,
                                Type = fb.Type,
                                SubType = fb.Parameter
                            });
                        }
                    }
                }

                Debug.Log("OfferMediaSection | cons() | offerRtp genereated");

                //this._mediaObject.payloads = offerRtpParameters!.codecs
                //  .map((codec: RtpCodecParameters) => codec.PayloadType)
                //.join(' ');

                _mediaObject.Attributes.Extmaps = new List<Extmap>();

                if (_offerRtp != null && _offerRtp.HeaderExtensions != null)
                {
                    foreach (var ext in _offerRtp.HeaderExtensions)
                    {
                        _mediaObject.Attributes.Extmaps.Add(new Extmap
                        {
                            Uri = new Uri(EnumExtensions.GetEnumMemberValue(ext.Uri)),
                            Value = ext.Id
                        });
                    }
                }


                //Done by default
                _mediaObject.Attributes.RtcpMux = true;
                _mediaObject.Attributes.RtcpRsize = true;

                if (_offerRtp != null && _offerRtp.Encodings != null && _offerRtp.Encodings.Count > 0)
                {
                    RtpEncodingParameters encoding = _offerRtp.Encodings[0];
                    uint ssrc = encoding.Ssrc.Value;
                    uint rtsxSSrc = uint.MaxValue;
                    if (encoding.Rtx != null && encoding.Rtx.Ssrc != null)
                    {
                        rtsxSSrc = encoding.Rtx.Ssrc;
                    }

                    _mediaObject.Attributes.Ssrcs = new List<Ssrc>();
                    _mediaObject.Attributes.SsrcGroups = new List<SsrcGroup>();

                    if (_offerRtp.Rtcp != null && !string.IsNullOrEmpty(_offerRtp.Rtcp.CNAME))
                    {
                        _mediaObject.Attributes.Ssrcs.Add(new Ssrc
                        {
                            Id = ssrc,
                            Attribute = "cname",
                            Value = _offerRtp.Rtcp.CNAME
                        });

                    }


                    if (_planB)
                    {
                        _mediaObject.Attributes.Ssrcs.Add(new Ssrc
                        {
                            Id = ssrc,
                            Attribute = "msid",
                            Value = $"{_streamId} {_trackId}"
                        });
                    }

                    if (rtsxSSrc != uint.MaxValue)
                    {
                        if (_offerRtp.Rtcp != null && !string.IsNullOrEmpty(_offerRtp.Rtcp.CNAME))
                        {
                            _mediaObject.Attributes.Ssrcs.Add(new Ssrc
                            {
                                Id = rtsxSSrc,
                                Attribute = "cname",
                                Value = _offerRtp.Rtcp.CNAME
                            });

                        }

                        if (_planB)
                        {
                            _mediaObject.Attributes.Ssrcs.Add(new Ssrc
                            {
                                Id = rtsxSSrc,
                                Attribute = "msid",
                                Value = $"{_streamId} {_trackId}"
                            });
                        }

                        // Associate original and retransmission SSRCs.
                        _mediaObject.Attributes.SsrcGroups.Add(new SsrcGroup
                        {
                            Semantics = "FID",
                            SsrcIds = new string[] { ssrc.ToString(), rtsxSSrc.ToString() },
                        });

                    }

                }

                break;

            case MediaKind.APPLICATION:

                if (!_oldDataChannelSpec)
                {

                }

                break;

        }

        Debug.Log("OfferMediaSection | cons() | end of constructor " + string.Join(",", this._mediaObject.Fmts.ToArray()) + " Size: " + _mediaObject.Fmts.Count);



    }

    internal void PlanBStopReceiving(RtpParameters offerRtp)
    {
        throw new NotImplementedException();
    }

    public override void Resume()
    {
        _mediaObject.Direction = "sendonly";
        _mediaObject.Attributes.SendOnly = true;
    }

    public override void SetDtlsRole(DtlsRole _dtlsRole)
    {
        // Always 'actpass'.
        _mediaObject.Attributes.Setup = new Setup { Role = SetupRole.ActPass };

    }
}

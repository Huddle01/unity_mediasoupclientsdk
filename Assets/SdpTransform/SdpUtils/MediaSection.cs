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

public class MediaSection
{

    public bool planB;

    public MediaDescription _mediaObject;

    public bool isClosed { get { return _mediaObject.Port == 0; } }
    public string mid { get { return _mediaObject.Attributes.Mid.Id; } }

    public MediaSection(IceParameters _iceParameters,List<IceCandidate> _iceCandidates,DtlsParameters _dtlsParameters,bool _planB) 
    {
        _mediaObject = new MediaDescription();
        planB = _planB;

        if (_iceParameters!=null) SetIceParameters(_iceParameters);

        if (_iceCandidates!=null && _iceCandidates.Count>0) 
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
                } else if (candi.protocol.ToLower().Contains("t")) 
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

            _mediaObject.Attributes.IceOptions = new IceOptions { Tags = Enumerable.Repeat("renomination", 1).ToArray()};

            if (_dtlsParameters!=null) { SetDtlsRole(_dtlsParameters.role); }

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

        Match mimeTypeMatch = MimeTypeRegex.Match(codec.mimeType);

        if (!mimeTypeMatch.Success)
        {
            throw new InvalidOperationException("Invalid codec.mimeType");
        }

        return mimeTypeMatch.Groups[2].Value;
    }


    private CandidateType IceCandidateToCandidateType(string value) 
    {
        value = value.ToLower();
        if (value.Contains("host"))
        {
            return CandidateType.Host;
        } else if (value.Contains("prflx")) 
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

public class AnswerMediaSection  : MediaSection
{
    public AnswerMediaSection(IceParameters _iceParameters, List<IceCandidate> _iceCandidates, DtlsParameters _dtlsParameters, 
                              SctpParameters _sctpParameters ,PlainRtpParameters _plainRtpParameters, bool _planB, object _offerMediaObject,
                              RtpParameters _offerRtp, RtpParameters _answerRtp,ProducerCodecOptions _codecOptions,bool _extmapAllowMixed): 
                                base(_iceParameters, _iceCandidates,_dtlsParameters, _planB) 
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
            }
            else 
            {
                AddrType tempAdr = _plainRtpParameters.ipVersion.Contains("4")? AddrType.Ip4 : AddrType.Ip6;
                _mediaObject.ConnectionData = new ConnectionData { ConnectionAddress = _plainRtpParameters.ip,AddrType = tempAdr };
                _mediaObject.Port = _plainRtpParameters.port;
            }

            switch (tempMediaDes.Media) 
            {
                case MediaType.Audio:
                case MediaType.Video:
                    _mediaObject.Direction = "recvonly";
                    _mediaObject.Attributes.Rtpmaps = new List<Rtpmap>();
                    _mediaObject.Attributes.RtcpFbs = new List<RtcpFb>();
                    _mediaObject.Attributes.Fmtps = new List<Fmtp>();

                    if (_answerRtp!=null) 
                    {
                        foreach (var codec in _answerRtp.codecs)
                        {
                            Rtpmap rtp = new Rtpmap 
                            {
                                PayloadType = codec.payloadType,
                                EncodingName = GetCodecName(codec),
                                ClockRate = codec.clockRate
                            };

                            if (codec.channels>1) 
                            {
                                rtp.Channels = codec.channels;
                            }

                            _mediaObject.Attributes.Rtpmaps.Add(rtp);


                            Dictionary<string, object> codecParameters;
                            if (codec.parameters != null)
                            {
                                codecParameters = new Dictionary<string, object>(codec.parameters);
                            }
                            else 
                            {
                                codecParameters = new Dictionary<string, object>();
                            }

                            List<RtcpFeedback> codecRtcpFeedback;
                            if (codec.rtcpFeedback!=null)
                            {
                                codecRtcpFeedback = codec.rtcpFeedback;
                            }
                            else 
                            {
                                codecRtcpFeedback = new List<RtcpFeedback>();
                            }

                            if (_codecOptions != null) 
                            {
                                RtpCodecParameters offerCodec = _offerRtp!.codecs.Find(x => x.payloadType == codec.payloadType);

                                switch (codec.mimeType.ToLower()) 
                                {
                                    case "audio/opus":
                                    case "audio/multiopus":

                                        //
                                        if (_codecOptions.opusStereo!=null) 
                                        {
                                            if (!offerCodec.parameters.ContainsKey("sprop-stereo"))
                                            {
                                                offerCodec.parameters.Add("sprop-stereo", _codecOptions.opusStereo.Value ? 1 : 0);
                                            }
                                            else
                                            {
                                                offerCodec.parameters["sprop-stereo"] = _codecOptions.opusStereo.Value ? 1 : 0;
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
                                            if (!offerCodec.parameters.ContainsKey("useinbandfec"))
                                            {
                                                offerCodec.parameters.Add("useinbandfec", _codecOptions.opusFec.Value ? 1 : 0);
                                            }
                                            else
                                            {
                                                offerCodec.parameters["useinbandfec"] = _codecOptions.opusFec.Value ? 1 : 0;
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
                                            if (!offerCodec.parameters.ContainsKey("usedtx"))
                                            {
                                                offerCodec.parameters.Add("usedtx", _codecOptions.opusDtx.Value ? 1 : 0);
                                            }
                                            else
                                            {
                                                offerCodec.parameters["usedtx"] = _codecOptions.opusDtx.Value ? 1 : 0;
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

                                        if (_codecOptions.opusMaxPlaybackRate!=null) 
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
                                            if (!offerCodec.parameters.ContainsKey("ptime"))
                                            {
                                                offerCodec.parameters.Add("ptime", _codecOptions.opusPtime.Value);
                                            }
                                            else
                                            {
                                                offerCodec.parameters["ptime"] = _codecOptions.opusPtime.Value;
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

                                        if (_codecOptions.opusNack!=null) 
                                        {
                                            if (offerCodec != null && offerCodec.rtcpFeedback != null)
                                            {
                                                offerCodec.rtcpFeedback = offerCodec.rtcpFeedback
                                                    .Where(fb => fb.type != "nack" || fb.parameters != null)
                                                    .ToList();
                                            }

                                            codecRtcpFeedback = codecRtcpFeedback.
                                                Where(fb => fb.type != "nack" || fb.parameters != null).
                                                ToList();
                                        }

                                        break;

                                    case "video/vp8" :
                                    case "video/vp9":
                                    case "video/h264":
                                    case "video/h265":

                                        if (_codecOptions.videoGoogleStartBitrate!=null) 
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

                                Fmtp fmtp = new Fmtp {PayloadType=codec.payloadType,Value ="" };

                                foreach (var key in codecParameters.Keys)
                                {
                                    if (fmtp.Value != null)
                                    {
                                        fmtp.Value += ";";
                                    }

                                    fmtp.Value += $"{key}={codecParameters[key]}";
                                }

                                if (!string.IsNullOrEmpty(fmtp.Value)) 
                                {
                                    _mediaObject.Attributes.Fmtps.Add(fmtp);
                                }

                                foreach (var fb in codecRtcpFeedback)
                                {
                                    _mediaObject.Attributes.RtcpFbs.Add(
                                        new RtcpFb {PayloadType = codec.payloadType,Type = fb.type,SubType = fb.parameters});

                                }

                            }

                        }

                        //_mediaObject.payloads = answerRtpParameters!.codecs
                        //.map((codec: RtpCodecParameters) => codec.payloadType)
                        //.join(' ');

                        _mediaObject.Attributes.Extmaps = new List<Extmap>();

                        if (_answerRtp!=null && _answerRtp.headerExtensions!=null) 
                        {
                            foreach (var ext in _answerRtp.headerExtensions)
                            {
                                // Don't add a header extension if not present in the offer.
                                bool found = (tempMediaDes.Attributes.Extmaps.Any(localExt => localExt.Uri.AbsoluteUri == ext.uri));

                                if (!found)
                                {
                                    continue;
                                }

                                _mediaObject.Attributes.Extmaps.Add(new Extmap
                                {
                                    Uri = new Uri(ext.uri) ,
                                    Value = ext.id
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

                        if (_planB && _mediaObject.Media == MediaType.Video) 
                        {
                            //_mediaObject.xGoogleFlag = 'conference';
                        }


                    }

                    break;

                case MediaType.Application:
                    if (tempMediaDes.Attributes.SctpPort!=null)
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

        string raw =  string.Join(";",_mediaObject.Attributes.Simulcast.IdList);
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
    }

    public override void SetDtlsRole(DtlsRole _dtlsRole)
    {
        switch (_dtlsRole) 
        {
            case DtlsRole.client:
                _mediaObject.Attributes.Setup = new Setup {Role = SetupRole.Active };
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
                              SctpParameters _sctpParameters, PlainRtpParameters _plainRtpParameters, bool _planB,string _mid,
                              MediaKind _mediaKind, RtpParameters _offerRtp,string _streamId, string _trackId,bool _oldDataChannelSpec = false) :
                                base(_iceParameters, _iceCandidates, _dtlsParameters, _planB)
    {

    }

    internal void PlanBStopReceiving(RtpParameters offerRtp)
    {
        throw new NotImplementedException();
    }
}

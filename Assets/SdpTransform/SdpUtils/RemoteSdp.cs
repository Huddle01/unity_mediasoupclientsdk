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
using System.Linq;
using Unity.WebRTC;

public class RemoteSdp
{
    private IceParameters? iceParameters;
    private List<IceCandidate>? iceCandidates;
    private DtlsParameters? dtlsParameters;
    private SctpParameters? sctpParameters;
    private PlainRtpParameters? plainRtpParameters;
    private bool planB;
    private List<MediaSection> mediaSections = new List<MediaSection>();
    private Dictionary<string, int> midToIndex = new Dictionary<string, int>();
    private string firstMid;
    private Sdp sdpObject;

    public RemoteSdp(IceParameters _iceParameters, List<IceCandidate> _iceCandidates, DtlsParameters _dtlsParameters,
                    SctpParameters _sctpParameters, PlainRtpParameters _plainRtpParameters, bool _planB)
    {
        iceParameters = _iceParameters;
        iceCandidates = _iceCandidates;
        dtlsParameters = _dtlsParameters;
        sctpParameters = _sctpParameters;
        plainRtpParameters = _plainRtpParameters;
        planB = _planB;

        sdpObject = new Sdp();
        sdpObject.ProtocolVersion = 0;
        Origin origin = new Origin
        {
            UnicastAddress = "0.0.0.0",
            AddrType = AddrType.Ip4,
            NetType = NetType.Internet,
            SessionId = 10000,
            SessionVersion = 0,
            UserName = "mediasoup-client"

        };

        sdpObject.Origin = origin;
        sdpObject.SessionName = "-";

        sdpObject.Timings = new List<Timing>
        {
            new Timing{StartTime=DateTimeOffset.FromUnixTimeSeconds(0).UtcDateTime,StopTime=DateTimeOffset.FromUnixTimeSeconds(0).UtcDateTime }
        };


        sdpObject.MediaDescriptions = new List<MediaDescription>();
        sdpObject.Attributes = new Attributes();

        // If ICE parameters are given, add ICE-Lite indicator.
        if (_iceParameters != null && _iceParameters.iceLite)
        {
            //sdpObject.Attributes.IceLiteLabel = "ice-lite";
            sdpObject.Attributes.IceLite = true;
        }

        // If DTLS parameters are given, assume WebRTC and BUNDLE.
        if (_dtlsParameters != null)
        {
            sdpObject.Attributes.MsidSemantic = new MsidSemantic { WebRtcMediaStreamToken = "WMS", Token = "*" };

            int numFingerPrints = _dtlsParameters.fingerprints.Count;
            DtlsFingerprint stldFingerPrint = _dtlsParameters.fingerprints[numFingerPrints - 1];
            Debug.Log($"DTLS Fingerprints: {JsonUtility.ToJson(_dtlsParameters.fingerprints)}");
            Fingerprint fingerPrint = new Fingerprint();

            switch (stldFingerPrint.algorithm)
            {
                case FingerPrintAlgorithm.sha1:
                    fingerPrint.HashFunction = HashFunction.Sha1;
                    break;

                case FingerPrintAlgorithm.sha224:
                    fingerPrint.HashFunction = HashFunction.Sha224;
                    break;

                case FingerPrintAlgorithm.sha256:
                    fingerPrint.HashFunction = HashFunction.Sha256;
                    break;

                case FingerPrintAlgorithm.sha384:
                    fingerPrint.HashFunction = HashFunction.Sha384;
                    break;

                case FingerPrintAlgorithm.sha512:
                    fingerPrint.HashFunction = HashFunction.Sha512;
                    break;
            }

            fingerPrint.HashValue = Encoding.UTF8.GetBytes(stldFingerPrint.value);
            //fingerPrint.HashValue = Encoding.UTF8.GetBytes(stldFingerPrint.value);

            sdpObject.Attributes.Fingerprint = fingerPrint;

            Debug.Log($"Fingerprint Algorithm: ${fingerPrint.HashFunction.ToString()}, Hash Value: {fingerPrint.HashValue}");

            sdpObject.Attributes.Group = new Group 
            {
                Semantics = GroupSemantics.Bundle,
                // TODO: Check this parameter
                SemanticsExtensions = new string[] { "0" },
            };

        }

        if (_plainRtpParameters != null)
        {
            sdpObject.Origin.UnicastAddress = _plainRtpParameters.ip;

            if (_plainRtpParameters.ipVersion.Contains("4"))
            {
                sdpObject.Origin.AddrType = AddrType.Ip4;
            }
            else if (_plainRtpParameters.ipVersion.Contains("6"))
            {
                sdpObject.Origin.AddrType = AddrType.Ip6;
            }
        }

        Debug.Log($"Final Remote SDP object: { sdpObject.ToText()}");

    }

    public void UpdateIceParameters(IceParameters _iceParameters)
    {
        iceParameters = _iceParameters;
        //sdpObject.Attributes.IceLiteLabel = _iceParameters.iceLite ? "ice-lite" : null;

        foreach (var item in mediaSections)
        {
            item.SetIceParameters(_iceParameters);
        }

    }


    public void UpdateDtlsRole(DtlsRole _dtlsRole)
    {
        if (dtlsParameters != null) dtlsParameters.role = _dtlsRole;
        
        foreach (var item in mediaSections)
        {
            item.SetDtlsRole(_dtlsRole);
        }
    }

    public Tuple<int, string> GetNextMediaSectionIdx() 
    {
        for (int idx = 0; idx < mediaSections.Count; idx++)
        {
            MediaSection tempMediaSection = mediaSections[idx];

            if (tempMediaSection.isClosed) 
            {
                return Tuple.Create(idx, tempMediaSection.mid);
            }

        }

        return Tuple.Create(mediaSections.Count, "");
    }

    public void Send(object _offerMediaObject,string _resuedMid,RtpParameters _offerRtp, RtpParameters _answerRtp,
                        ProducerCodecOptions _codecOptions,bool _extmapAllowMixed) 
    {
        AnswerMediaSection asnwerMediaSection = new AnswerMediaSection(iceParameters,iceCandidates,dtlsParameters,null,plainRtpParameters,
                                                    planB,_offerMediaObject,_offerRtp,_answerRtp,_codecOptions,_extmapAllowMixed);
        Debug.Log(_resuedMid);
        if (!string.IsNullOrEmpty(_resuedMid))
        {
            ReplaceMediaSection(asnwerMediaSection, _resuedMid);
        } else if (!midToIndex.ContainsKey(asnwerMediaSection.mid))
        {
            AddMediaSection(asnwerMediaSection);
        }
        else 
        {
            ReplaceMediaSection(asnwerMediaSection);
        }
    }

    public void Receive(string _mid,MediaKind _mediaKind, RtpParameters _offerRtp,string _streamId,string _trackId) 
    {
        OfferMediaSection offerMediaSection = null;

        int idx = -1;
        if (midToIndex.TryGetValue(_mid, out idx)) 
        {
            offerMediaSection = mediaSections[idx] as OfferMediaSection;
        }

        if (offerMediaSection == null)
        {
            offerMediaSection = new OfferMediaSection(iceParameters, iceCandidates, dtlsParameters, null, plainRtpParameters, planB,
                                                        _mid, _mediaKind, _offerRtp, _streamId, _trackId);

            // Let's try to recycle a closed media section (if any).
            // NOTE: Yes, we can recycle a closed m=audio section with a new m=video.
            var oldMediaSection = mediaSections.FirstOrDefault(x => x.isClosed);

            if (oldMediaSection != null)
            {
                ReplaceMediaSection(offerMediaSection, oldMediaSection.mid);
            }
            else
            {
                AddMediaSection(offerMediaSection);
            }

        }
        else // Plan-B.
        {
            offerMediaSection.PlanBReceive(_offerRtp,_streamId,_trackId);
            ReplaceMediaSection(offerMediaSection);
        }

    }

    public void PauseMediaSection(string _mid) 
    {
        MediaSection mediaSection = FindMediaSection(_mid);
        mediaSection.Pause();
    }

    public void ResumeSendingMediaSection(string _mid)
    {
        MediaSection mediaSection = FindMediaSection(_mid);
        mediaSection.Resume();
    }

    public void ResumeReceivingMediaSection(string _mid)
    {
        MediaSection mediaSection = FindMediaSection(_mid);
        mediaSection.Resume();
    }

    public void DisableMediaSection(string _mid)
    {
        MediaSection mediaSection = FindMediaSection(_mid);
        mediaSection.Disable();
    }

    public bool CloseMediaSection(string _mid)
    {
        MediaSection mediaSection = FindMediaSection(_mid);

        if (_mid == firstMid) 
        {
            Debug.Log($"closeMediaSection() | cannot close first media section, disabling it instead {_mid}");
            DisableMediaSection(_mid);
            return false;
        }
        mediaSection.Closed();

        RegenerateBundleMids();

        return true;

    }

    public void MuxMediaSectionSimulcast(string _mid,List<RTCRtpEncodingParameters> _encodings) 
    {
        AnswerMediaSection mediaSection = FindMediaSection(_mid) as AnswerMediaSection;
        mediaSection.MuxSimulcastStreams(_encodings);
        ReplaceMediaSection(mediaSection);
    }

    public void PlanBStopReceiving(string _mid,RtpParameters _offerRtp) 
    {
        OfferMediaSection mediaSection = FindMediaSection(_mid) as OfferMediaSection;
        mediaSection.PlanBStopReceiving(_offerRtp);
        ReplaceMediaSection(mediaSection);
    }

    public void SendSctpAssociation(object _offerMediaObject) 
    {
        AnswerMediaSection mediaSection = new AnswerMediaSection(iceParameters, iceCandidates, dtlsParameters, sctpParameters, plainRtpParameters,
                                                    false, _offerMediaObject);

        AddMediaSection(mediaSection);
    }

    public void ReceiveSctpAssociation(bool _oldDataChannelSpec) 
    {
        OfferMediaSection mediaSection = new OfferMediaSection(iceParameters, iceCandidates, dtlsParameters, sctpParameters, plainRtpParameters,
                                                        false, "datachannel",MediaKind.APPLICATION,null,null,null, _oldDataChannelSpec);

        AddMediaSection(mediaSection);
    }

    public string GetSdp() 
    {
        sdpObject.Origin.SessionVersion++;
        return sdpObject.ToText();
    }

    public void AddMediaSection(MediaSection _newMediaSection)
    {
        if (string.IsNullOrEmpty(firstMid))
        {
            firstMid = _newMediaSection.mid;
        }

        mediaSections.Add(_newMediaSection);
        midToIndex.Add(_newMediaSection.mid, mediaSections.Count - 1);
        sdpObject.MediaDescriptions.Add(_newMediaSection.GetObject());

        RegenerateBundleMids();
    }

    private void ReplaceMediaSection(MediaSection _newMediaSection, string _resuedMid = null)
    {
        if (!string.IsNullOrEmpty(_resuedMid))
        {
            int idx = -1;

            if (!midToIndex.TryGetValue(_resuedMid, out idx))
            {
                throw new Exception($"no media section found with mid '${_resuedMid}'");
            }

            MediaSection oldMediaSection = mediaSections[idx];
            // Replace the index in the vector with the new media section.
            mediaSections[idx] = _newMediaSection;

            // Update the map.
            midToIndex.Remove(oldMediaSection.mid);
            midToIndex.Add(_newMediaSection.mid,idx);

            // Update the SDP object.
            sdpObject.MediaDescriptions[idx] = _newMediaSection.GetObject();

            // Regenerate BUNDLE mids.
            RegenerateBundleMids();

        }
        else 
        {
            int idx = -1;

            if (!midToIndex.TryGetValue(_newMediaSection.mid, out idx))
            {
                throw new Exception($"no media section found with mid '${_resuedMid}'");
            }

            mediaSections[idx] = _newMediaSection;
            sdpObject.MediaDescriptions[idx] = _newMediaSection.GetObject();
        }
    }

    public MediaSection FindMediaSection(string _mid)
    {
        int idx = -1;

        if (!midToIndex.TryGetValue(_mid, out idx))
        {
            throw new Exception($"no media section found with mid '${_mid}'");
        }

        return mediaSections[idx];

    }

    public void RegenerateBundleMids()
    {
        if (dtlsParameters == null) return;

        if (sdpObject.Attributes.Group.SemanticsExtensions!=null && sdpObject.Attributes.Group.SemanticsExtensions.Length > 0)
        {
            sdpObject.Attributes.Group.SemanticsExtensions[0] = string.Join(' ', mediaSections
                .Where(mediaSection => !mediaSection.isClosed)
                .Select(mediaSection => mediaSection.mid));

        }
    }
}

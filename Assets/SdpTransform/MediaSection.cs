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

public class MediaSection
{
    public bool isClosed;
    public string mid;

    public MediaSection(IceParameters _iceParameters,List<IceCandidate> _iceCandidates,DtlsParameters _dtlsParameters,bool _planB) 
    {
        
    }

    public void SetIceParameters(IceParameters _iceParameters) 
    {
        
    }

    public void SetDtlsRole(DtlsRole _dtlsRole)
    {

    }

    public void PlanBReceive(RtpParameters offerRtp, string streamId, string trackId)
    {
        throw new NotImplementedException();
    }

    public void Pause()
    {
        throw new NotImplementedException();
    }

    public void Resume()
    {
        throw new NotImplementedException();
    }

    public void Disable()
    {
        throw new NotImplementedException();
    }

    internal void Closed()
    {
        throw new NotImplementedException();
    }

    public MediaDescription GetObject()
    {
        throw new NotImplementedException();
    }
}

public class AnswerMediaSection  : MediaSection
{
    public AnswerMediaSection(IceParameters _iceParameters, List<IceCandidate> _iceCandidates, DtlsParameters _dtlsParameters, 
                              SctpParameters _sctpParameters ,PlainRtpParameters _plainRtpParameters, bool _planB, object _offerMediaObject,
                              RtpParameters _offerRtp, RtpParameters _answerRtp,ProducerCodecOptions _codecOptions,bool _extmapAllowMixed): 
                                base(_iceParameters, _iceCandidates,_dtlsParameters, _planB) 
    {
        
    }

    public AnswerMediaSection(IceParameters _iceParameters, List<IceCandidate> _iceCandidates, DtlsParameters _dtlsParameters,
                              SctpParameters _sctpParameters, PlainRtpParameters _plainRtpParameters, bool _planB, object _offerMediaObject) :
                                base(_iceParameters, _iceCandidates, _dtlsParameters, _planB)
    {

    }

    internal void MuxSimulcastStreams(List<RTCRtpEncodingParameters> encodings)
    {
        throw new NotImplementedException();
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

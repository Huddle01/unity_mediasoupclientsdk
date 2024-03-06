using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;
using System;
using Mediasoup.RtpParameter;
using Mediasoup.Internal;
using Mediasoup.Types;

namespace Mediasoup
{
    public interface IProducer
    {
        string id { get;}
        string localId { get;}
        bool isClosed { get; }

        RTCRtpSender rtpSender {get;}
        MediaStreamTrack track { get; }
        MediaKind kind { get; }
        RtpParameters rtpParameters { get; }

        bool isPaused { get; }
        int maxSpatialLayer { get; }
        bool stopTracks { get; }
        bool disableTrackOnPause { get; }
        bool zeroRtpOnPause { get; }

        object appData { get; }

        EnhancedEventEmitter<ProducerObserverEvents> observer { get;}

        void Close();
        void TransportClosed();
        RTCStatsReport GetStats();
        void Pause();
        void Resume();
        void ReplaceTrack(MediaStreamTrack track);
        void SetMaxSpatialLayer(int layer);
        void SetRtpEncodingParameters(RtpEncodingParameters parameters);
        

    }

    public class Producer<TProducerAppData> : EnhancedEventEmitter<ProducerEvents>, IProducer where TProducerAppData:AppData
    {
        public string id { get; private set; }

        public string localId { get; private set; }

        public bool isClosed { get; private set; }

        public RTCRtpSender rtpSender { get; private set; }

        public MediaStreamTrack track { get; private set; }

        public MediaKind kind { get; private set; }

        public RtpParameters rtpParameters { get; private set; }

        public bool isPaused { get; private set; }

        public int maxSpatialLayer { get; private set; }

        public bool stopTracks { get; private set; }

        public bool disableTrackOnPause { get; private set; }

        public bool zeroRtpOnPause { get; private set; }

        public object appData { get; private set; }

        public EnhancedEventEmitter<ProducerObserverEvents> observer { get; set; }

        public Producer(string _id,string _localId, RTCRtpSender _rtpSender,MediaStreamTrack _track,RtpParameters _rtpParameters,
                   bool _stopTracks, bool _disableTrackOnPause, bool _zeroRtpOnPause, TProducerAppData? _appData)
        {

            id = _id;
            localId = _localId;
            rtpSender = _rtpSender;
            track = _track;
            rtpParameters = _rtpParameters;
            isPaused = !track.Enabled ? _disableTrackOnPause : false;
            maxSpatialLayer = -1;
            stopTracks = _stopTracks;
            disableTrackOnPause = _disableTrackOnPause;
            zeroRtpOnPause = _zeroRtpOnPause;


            if (_appData != null) appData = _appData ?? typeof(TProducerAppData).New<TProducerAppData>()!;
            observer = new EnhancedEventEmitter<ProducerObserverEvents>();
        }

        private void OnTrackEnded() 
        {
            _ = Emit("trackended");
            _ = observer.SafeEmit("trackended");
        }

        private void HandleTrack()
        {
            
        }

        private void DestroyTrack()
        {
            if (track == null) return;

            if (stopTracks) 
            {
                track.Stop();
                OnTrackEnded();
            } 
        }

        public void Close()
        {
            if (isClosed) return;

            isClosed = false;
            DestroyTrack();
            _ = Emit("@close");
            _ = observer.SafeEmit("close");
        }

        public void TransportClosed()
        {
            if (isClosed) return;
            isClosed = true;
            DestroyTrack();
            _ = Emit("transportclose");
            _ = observer.SafeEmit("close");
        }

        public RTCStatsReport GetStats()
        {
            if (isClosed)
            {
                throw new InvalidOperationException("closed");
            }

            RTCStatsReportAsyncOperation a = rtpSender.GetStats();
            return a.Value;
        }

        public void Pause()
        {
            if (isClosed) return;
            isPaused = true;

            if (track != null && disableTrackOnPause)
            {
                track.Enabled = false;
            }

            if (zeroRtpOnPause)
            {
                _ = SafeEmit("@pause");
            }

            _ = observer.SafeEmit("pause");

        }

        public void Resume()
        {
            if (isClosed) return;

            isPaused = false;

            if (track!=null && disableTrackOnPause ) 
            {
                track.Enabled = true;
            }

            if (zeroRtpOnPause) 
            {
                _ = SafeEmit("resume");
            }

            _ = observer.SafeEmit("resume");

        }

        public void ReplaceTrack(MediaStreamTrack _track)
        {
            if (isClosed)
            {
                if (_track != null && stopTracks)
                {
                    try
                    {
                        _track.Stop();
                    } catch (Exception error) { }
                }

                throw new InvalidOperationException("Closed");
            } else if (_track != null && track.ReadyState == TrackState.Ended) 
            {
                throw new InvalidOperationException("Ended");
            }

            if (track == _track) 
            {
                return;
            }

            rtpSender.ReplaceTrack(_track);

            DestroyTrack();

            track = _track;

            if (track!=null && disableTrackOnPause)
            {
                if (!isPaused)
                {
                    track.Enabled = true;
                }
                else if (isPaused)
                {
                    track.Enabled = false;
                }
            }
        }

        public void SetMaxSpatialLayer(int layer)
        {
            if (isClosed)
            {
                throw new InvalidOperationException("Closed");
            }
            else if (kind != MediaKind.video)
            {
                throw new InvalidProgramException("not a video producer");
            }

            if (layer == maxSpatialLayer) 
            {
                return;
            }

            //todo set layer

            maxSpatialLayer = layer;
        }

        public void SetRtpEncodingParameters(RtpEncodingParameters parameters)
        {
            if (isClosed)
            {
                throw new InvalidOperationException("Closed");
            } else if (parameters==null) 
            {
                throw new InvalidCastException("Invalid params");
            }

            RTCRtpEncodingParameters tempParam = new RTCRtpEncodingParameters
            {
                active = true,
                maxBitrate = (ulong)parameters.maxBitrate,
                maxFramerate = (uint)parameters.maxFramerate,
                rid = parameters.rid,
                scaleResolutionDownBy = parameters.scaleResolutionDownBy
            };

            RTCRtpSendParameters sendParam = rtpSender.GetParameters();
            for (int i=0;i<sendParam.encodings.Length;i++) 
            {
                sendParam.encodings[i] = tempParam;
            }

            _ = rtpSender.SetParameters(sendParam);
        }
    }

    public class ProducerEvents
    {
        public List<Action> transportclose { get; set; } = new List<Action>();
        public List<Action> trackended { get; set; } = new List<Action>();


        public Tuple<Action, Action<string>> OnPause;
        public Tuple<Action, Action<string>> OnResume;
        public Tuple<Action<MediaStreamTrack>,Action, Action<string>> OnReplaceTrack;
        public Tuple<Action<int>,Action, Action<string>> OnSetmaxspatiallayer;
        public Tuple<Action<RtpEncodingParameters>, Action, Action<string>> OnSetrtpencodingparameters;
        public Tuple<Action<RTCStatsReport>, Action<string>> Getstats;
        public Action OnClose;

    }

    public class ProducerOptions<TProducerAppData>
    {
        public MediaStreamTrack track;
        public List<RtpEncodingParameters> encodings { get; set; } = new List<RtpEncodingParameters>();

        public ProducerCodecOptions codecOptions;
        public RtpCodecCapability codec;
        public bool stopTracks;
        public bool disableTrackOnPause;
        public bool zeroRtpOnPause;
        public TProducerAppData appData;

    }

    public class ProducerObserverEvents
    {
        public Action OnClose { get; set; }
        public Action OnPause { get; set; }
        public Action OnResume { get; set; }
        public Action OnTrackEnded { get; set; }
    }

    public class ProducerCodecOptions 
    {
        public bool? opusStereo;
        public bool? opusFec;
        public bool? opusDtx;
        public int? opusMaxPlaybackRate;
        public int? opusMaxAverageBitrate;
        public int? opusPtime;
        public bool? opusNack;
        public int? videoGoogleStartBitrate;
        public int? videoGoogleMaxBitrate;
        public int? videoGoogleMinBitrate;
    }
}

using System.Collections;
using System.Collections.Generic;
using Mediasoup.RtpParameter;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Mediasoup.Internal;
using System;
using System.Threading.Tasks;
using Unity.WebRTC;

namespace Mediasoup
{
    public interface IConsumer
    {
        string id { get; set; }
        string localId { get; set; }
        string producerId { get; set; }
        bool isClosed { get; set; }
        MediaKind kind { get; set; }
        RTCRtpReceiver rtpReceiver { get; set; }
        MediaStreamTrack track { get; set; }
        RtpParameters rtpParameters { get; set; }
        bool isPaused { get; set; }

        object appData { get; set; }

        void Close();
        RTCStatsReport GetStats();
        void Pause();
        void Resume();
        void TransportClosed();
        void OnTrackEnded();
        void HandleTrack();
        void DestroyTrack();

        EnhancedEventEmitter observer { get; set; }

    }

    public class Consumer<TConsumerAppData> : EnhancedEventEmitter<ConsumerEvents>, IConsumer
    {
        public object appData { get; set; }
        public string id { get; set; }
        public string localId { get; set; }
        public string producerId { get; set; }
        public bool isClosed { get; set; }
        public MediaKind kind { get; set; }
        public RTCRtpReceiver rtpReceiver { get; set; }
        public MediaStreamTrack track { get; set; }
        public RtpParameters rtpParameters { get; set; }
        public bool isPaused { get; set; }


        public EnhancedEventEmitter observer { get; set; }

        public Consumer(string _id,string _localId, string _producerId, RTCRtpReceiver _rtpReceiver,
                            MediaStreamTrack _track, RtpParameters _rtpParameters, TConsumerAppData? _appData) : base()
        {
            id = _id;
            localId = _localId;
            producerId = _producerId;
            rtpReceiver = _rtpReceiver;
            track = _track;
            rtpParameters = _rtpParameters;

            if (_appData != null) appData = _appData ?? typeof(TConsumerAppData).New<TConsumerAppData>()!;
        }


        public delegate void OnGetStatsHandler(RTCStatsReport report);
        public event OnGetStatsHandler OnGetStats;

        public RTCStatsReport GetStats()
        {
            if (isClosed) 
            {
                throw new InvalidOperationException("closed");
            }

            RTCStatsReportAsyncOperation a =  rtpReceiver.GetStats();
            return a.Value;
        }

        

        public void Pause()
        {
            if (isClosed) return;
            if (isPaused) return;

            isPaused = true;
            track.Enabled = false;

            Emit("@pause");
            observer.SafeEmit("pause");
        }

        public void Resume()
        {
            throw new NotImplementedException();
        }

        public void TransportClosed()
        {
            if (isClosed) return;
            isClosed = true; 
            DestroyTrack();
            Emit("transportclose");
            observer.SafeEmit("close");
        }

        public void HandleTrack()
        {
            throw new NotImplementedException();
        }

        public void DestroyTrack()
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            if (isClosed) return;

            isClosed = true;
            DestroyTrack();
            Emit("@close");
            observer.SafeEmit("close");
        }

        public void OnTrackEnded()
        {
            throw new NotImplementedException();
        }
        
    }



    public class ConsumerEvents
    {
        public List<Action> transportclose { get; set; } = new List<Action>();
        public List<Action> trackended { get; set; } = new List<Action>();

        public List<Action<RTCStatsReport>> OnGetStats { get; set; } = new List<Action<RTCStatsReport>>();
        public List<Action> OnClose { get; set; } = new List<Action>();
        public List<Action> OnPause { get; set; } = new List<Action>();
        public List<Action> OnResume { get; set; } = new List<Action>();
    }

    public class ConsumerOptions
    {
       
    }


    [JsonConverter(typeof(StringEnumConverter))]
    public enum ConsumerTraceEventType
    {
        rtp,
        keyframe,
        nack,
        pli,
        fir
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ConsumerType
    {
        simple = 1,
        simulcast = 2,
        svc = 3,
        pipe
    }

}



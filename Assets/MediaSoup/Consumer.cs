using System.Collections.Generic;
using Mediasoup.RtpParameter;
using Mediasoup.Internal;
using System;
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

        void Close();
        RTCStatsReport GetStats();
        void Pause();
        void Resume();
        void TransportClosed();

        EnhancedEventEmitter<ConsumerObserverEvents> observer { get; set; }

    }

    public class Consumer<TConsumerAppData> : EnhancedEventEmitter<ConsumerEvents>, IConsumer
    {
        public object appData { get; private set; }
        public string id { get; set; }
        public string localId { get; set; }
        public string producerId { get; set; }
        public bool isClosed { get; set; }
        public MediaKind kind { get; set; }
        public RTCRtpReceiver rtpReceiver { get; set; }
        public MediaStreamTrack track { get; set; }
        public RtpParameters rtpParameters { get; set; }
        public bool isPaused { get; set; }


        public EnhancedEventEmitter<ConsumerObserverEvents> observer { get; set; }

        public Consumer(string _id, string _localId, string _producerId, RTCRtpReceiver _rtpReceiver,
                            MediaStreamTrack _track, RtpParameters _rtpParameters, TConsumerAppData? _appData) : base()
        {
            id = _id;
            localId = _localId;
            producerId = _producerId;
            rtpReceiver = _rtpReceiver;
            track = _track;
            rtpParameters = _rtpParameters;

            if (_appData != null) appData = _appData ?? typeof(TConsumerAppData).New<TConsumerAppData>()!;

            observer = new EnhancedEventEmitter<ConsumerObserverEvents>();
        }

        ~Consumer()
        {

        }

        public void Close()
        {
            if (isClosed) return;

            isClosed = true;
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

            RTCStatsReportAsyncOperation a = rtpReceiver.GetStats();
            return a.Value;
        }

        public void Pause()
        {
            if (isClosed) return;
            if (isPaused) return;

            isPaused = true;
            track.Enabled = false;

            _ = Emit("@pause");
            _ = observer.SafeEmit("pause");
        }

        public void Resume()
        {
            if (isClosed) return;
            if (!isPaused) return;//already playing

            isPaused = false;
            track.Enabled = true;

            _ = Emit("@resume");
            _ = observer.SafeEmit("resume");

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
            track.Stop();
            OnTrackEnded();
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

        public string Close = "close";
    }

    public class ConsumerOptions<TConsumerAppData>
    {
        public string id;
        public string producerId;
        public string kind;  //'audio' | 'video'
        public RtpParameters rtpParameters;
        public string streamId;
        public TConsumerAppData appData;
    }

    public class ConsumerObserverEvents
    {
        public List<Action> OnClose { get; set; } = new List<Action>();
        public List<Action> OnPause { get; set; } = new List<Action>();
        public List<Action> OnResume { get; set; } = new List<Action>();
        public List<Action> OnTrackEnded { get; set; } = new List<Action>();
    }

}

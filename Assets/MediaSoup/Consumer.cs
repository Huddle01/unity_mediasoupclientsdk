using System.Collections;
using System.Collections.Generic;
using Mediasoup.RtpParameter;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Mediasoup.Internal;
using System;

namespace Mediasoup 
{
    public interface IConsumer
    {
        string Id { get; }
        bool Closed { get; }

        bool Paused { get; }

        bool ProducerPaused { get; }

        MediaKind Kind { get; }

        RtpParameter.RtpParameters RtpParameters { get; }

        ConsumerType Type { get; }

        void Close();

        void TransportClosed();

        internal EnhancedEventEmitter Observer { get; }
    }

    public class Consumer<TConsumerAppData> : EnhancedEventEmitter<ConsumerEvents>, IConsumer
    {

        public object AppData { get; protected set; }

        public Consumer(TConsumerAppData? appData) : base()
        {
            if(appData!=null) AppData = appData;
        }

        public string Id => throw new NotImplementedException();

        public bool Closed => throw new NotImplementedException();

        public bool Paused => throw new NotImplementedException();

        public bool ProducerPaused => throw new NotImplementedException();

        public MediaKind Kind => throw new NotImplementedException();

        public RtpParameters RtpParameters => throw new NotImplementedException();

        public ConsumerType Type => throw new NotImplementedException();

        string IConsumer.Id => throw new NotImplementedException();

        bool IConsumer.Closed => throw new NotImplementedException();

        bool IConsumer.Paused => throw new NotImplementedException();

        bool IConsumer.ProducerPaused => throw new NotImplementedException();

        MediaKind IConsumer.Kind => throw new NotImplementedException();

        RtpParameters IConsumer.RtpParameters => throw new NotImplementedException();

        ConsumerType IConsumer.Type => throw new NotImplementedException();

        EnhancedEventEmitter IConsumer.Observer => throw new NotImplementedException();

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void TransportClosed()
        {
            throw new NotImplementedException();
        }

        void IConsumer.Close()
        {
            throw new NotImplementedException();
        }

        void IConsumer.TransportClosed()
        {
            throw new NotImplementedException();
        }
    }



    public record ConsumerEvents
    {
        public List<object> Transportclose { get; set; } = new();
        public List<object> Producerclose { get; set; } = new();
        public List<object> Producerpause { get; set; } = new();
        public List<object> Producerresume { get; set; } = new();
        public Tuple<ConsumerScore> Score { get; set; }
        public Tuple<ConsumerLayers?> Layerschange { get; set; }
        public Tuple<ConsumerTraceEventData> Trace { get; set; }
        public Tuple<byte[]> Rtp { get; set; }

        private List<object> close = new();
        private List<object> producerclose = new();

    }

    public record ConsumerScore
    {
       
        public int Score { get; set; }

        
        public int ProducerScore { get; set; }

        
        public List<object> ProducerScores { get; set; } = new();
    }

    public record ConsumerLayers
    {
        
        public int SpatialLayer { get; set; }

        
        public int? TemporalLayer { get; set; }
    }

    public record ConsumerTraceEventData
    {
        public ConsumerTraceEventType Type { get; set; }

        public long Timestamp;

        public string Direction { get; set; }

        public object Info { get; set; }
    }

    public record ConsumerObserverEvents
    {
        public List<object> Close { get; set; } = new();
        public List<object> Pause { get; set; } = new();
        public List<object> Resume { get; set; } = new();
        public Tuple<ConsumerScore> Score { get; set; }
        public Tuple<ConsumerLayers?> Layerschange { get; set; }
        public Tuple<ConsumerTraceEventData> Trace { get; set; }
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



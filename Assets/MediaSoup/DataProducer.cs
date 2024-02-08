using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;
using Mediasoup.SctpParameter;
using Mediasoup.Internal;

namespace Mediasoup.DataProducers 
{
    public interface IDataProducer 
    {
        string id { get; }
        RTCDataChannel dataChannel { get; }
        bool isClosed { get; }
        SctpStreamParameters sctpStreamParameters { get; }
        object dataProducerAppData { get; }
        EnhancedEventEmitter observer { get; }

        void Close();
        void TransportClosed();
        void Send(object message);

    }

    public class DataProducer<TDataProducerAppData> : EnhancedEventEmitter<DataProducerEvents>, IDataProducer
    {
        public string id { get; private set; }

        public RTCDataChannel dataChannel { get; private set; }

        public bool isClosed { get; private set; }

        public SctpStreamParameters sctpStreamParameters { get; private set; }

        public object dataProducerAppData { get; private set; }

        public EnhancedEventEmitter observer { get; private set; }


        public DataProducer(string _id, RTCDataChannel _dataChannel, SctpStreamParameters _sctpStreamParameters,
                                TDataProducerAppData _appData)
        {
            id = _id;
            dataChannel = _dataChannel;
            sctpStreamParameters = _sctpStreamParameters;
            if (_appData != null) dataProducerAppData = _appData ?? typeof(TDataProducerAppData).New<TDataProducerAppData>()!;

            observer = new EnhancedEventEmitter<ConsumerObserverEvents>();

            HandleDataChannel();
        }

        ~DataProducer()
        {
            UnSubscribeEvents();
        }


        public void Close()
        {
            if (isClosed) return;

            isClosed = true;
            dataChannel.Close();

            _ = Emit("@close");
            _ = observer.SafeEmit("close");
        }

        public void TransportClosed()
        {
            if (isClosed) return;

            isClosed = true;
            dataChannel.Close();

            _ = Emit("transportclose");
            _ = observer.SafeEmit("close");
        }

        public void Send(object message)
        {
            if (isClosed) 
            {
                throw new InvalidOperationException("closed");
            }

            if (message is string)
            {
                string msg = (string)message;
                dataChannel.Send(msg);
            } else if (message is byte[]) 
            {
                byte[] msg = (byte[])message;
                dataChannel.Send(msg);
            }
        }


        private void HandleDataChannel()
        {
            dataChannel.OnOpen += OnDataChannelOpen;
            dataChannel.OnError += OnDataChannelError;
            dataChannel.OnMessage += OnDataChannelMessageReceived;
            dataChannel.OnClose += OnDatachannelClose;
        }

        private void UnSubscribeEvents()
        {
            dataChannel.OnOpen -= OnDataChannelOpen;
            dataChannel.OnError -= OnDataChannelError;
            dataChannel.OnMessage -= OnDataChannelMessageReceived;
            dataChannel.OnClose -= OnDatachannelClose;
        }


        private void OnDataChannelOpen()
        {
            if (isClosed) return;
            _ = SafeEmit("open");
        }


        private void OnDatachannelClose()
        {
            if (isClosed) return;

            _ = Emit("@close");
            _ = SafeEmit("close");
            _ = observer.SafeEmit("close");

        }

        private void OnDataChannelMessageReceived(byte[] bytes)
        {
            if (isClosed) return;
        }

        private void OnDataChannelError(RTCError error)
        {
            if (isClosed) return;

            if (error.message == "sctp-failure")
            {
                Debug.Log($"DataChannel SCTP error {error.message} with code {error.errorType.ToString()}");
            }
            else
            {
                Debug.Log($"DataChannel error {error.message} with code {error.errorType.ToString()}");
            }

            _ = SafeEmit("error", error);
        }

        
    }

    public class DataProducerOptions<TDataProducerAppData>
    {
        public bool ordered;
        public int maxPacketLifeTime;
        public int maxRetransmits;
        public string label;
        public string protocol;
        public TDataProducerAppData dataConsumerAppData;
    }

    public class DataProducerEvents
    {
        public List<Action> TransportClosed { get; set; } = new List<Action>();
        public List<Action> Open { get; set; } = new List<Action>();

        public List<Action<string>> Error { get; set; } = new List<Action<string>>();
        public List<Action> Close { get; set; } = new List<Action>();
        public List<Action<object>> Message { get; set; } = new List<Action<object>>();
        public List<Action> OnClose { get; set; } = new List<Action>();
    }

    public class DataProducerObserverEvents
    {
        public List<Action> Close { get; set; } = new List<Action>();
    }
}



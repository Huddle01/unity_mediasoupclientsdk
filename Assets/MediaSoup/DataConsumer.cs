using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;
using Mediasoup.SctpParameter;
using Mediasoup.Internal;


namespace Mediasoup.DataConsumers 
{
    public interface IDataConsumer 
    {
        string id { get;}
        string dataProducerId { get;}
        RTCDataChannel dataChannel { get;}
        bool isClosed { get;}
        SctpStreamParameters sctpStreamParameters { get;}
        object dataConsumerAppData { get;}
        EnhancedEventEmitter observer { get;}

        void Close();
        void TransportClosed();
    }

    public class DataConsumer<TDataConsumerAppData> : EnhancedEventEmitter<ConsumerEvents>, IDataConsumer
    {
        public string id { get; private set; }
        public string dataProducerId { get; private set; }
        public RTCDataChannel dataChannel { get; private set; }
        public bool isClosed { get; private set; }
        public SctpStreamParameters sctpStreamParameters { get; private set; }
        public object dataConsumerAppData { get; private set; }
        public EnhancedEventEmitter observer { get; private set; }

        public DataConsumer(string _id,string _dataProducerId, RTCDataChannel _dataChannel, SctpStreamParameters _sctpStreamParameters,
                                TDataConsumerAppData _appData) 
        {
            id = _id;
            dataProducerId = _dataProducerId;
            dataChannel = _dataChannel;
            sctpStreamParameters = _sctpStreamParameters;
            if (_appData != null) dataConsumerAppData = _appData ?? typeof(TDataConsumerAppData).New<TDataConsumerAppData>()!;

            observer = new EnhancedEventEmitter<ConsumerObserverEvents>();

            HandleDataChannel();
        }

        ~DataConsumer() 
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

            _ = SafeEmit("message", bytes);

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

            _ = SafeEmit("error",error);
        }

    }


    public class DataConsumerOptions<TDataConsumerAppData> 
    {
        public string id;
        public string datProducerId;
        public SctpParameters sctpStreamParameters;
        public string label;
        public string protocol;
        public TDataConsumerAppData dataConsumerAppData;
    }

    public class DataConsumerEvents 
    {
        public List<Action> TransportClosed { get; set; } = new List<Action>();
        public List<Action> Open { get; set; } = new List<Action>();

        public List<Action<string>> Error { get; set; } = new List<Action<string>>();
        public List<Action> Close { get; set; } = new List<Action>();
        public List<Action<object>> Message { get; set; } = new List<Action<object>>();
        public List<Action> OnClose { get; set; } = new List<Action>();
    }

    public class DataConsumerObserverEvents 
    {
        public List<Action> Close { get; set; } = new List<Action>();
    }

}
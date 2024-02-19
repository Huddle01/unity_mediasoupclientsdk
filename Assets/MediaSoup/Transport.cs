using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Unity.WebRTC;
using Mediasoup.Internal;
using Mediasoup.DataConsumers;
using Mediasoup.DataProducers;
using Mediasoup.RtpParameter;
using Mediasoup.SctpParameter;
using Mediasoup.Ortc;
using Mediasoup;
using System.Threading.Tasks;
using Mediasoup.Types;
using Newtonsoft.Json.Converters;

/// <summary>
/// 
/// </summary>

namespace Mediasoup.Transports
{
    public interface ITransport 
    {
        string id { get; }
        bool isClosed { get; }
        string direction { get; }
        object extendedRtpCapabilities { get; }
        CanProduceByKind canProduceKind { get; }
        int maxSctpMessageSize { get; }
        HandlerInterface handlerInterface { get; }
        RTCIceGatheringState iceGatheringState { get; }
        RTCIceConnectionState connectionState { get; }
        object appData { get; }
        Dictionary<string,IProducer> producers { get ; }
        Dictionary<string,IConsumer> consumers { get; }
        Dictionary<string, IDataConsumer> dataConsumers { get; }
        Dictionary<string, IDataProducer> datapPorducers { get; }
        bool _probatorConsumerCreated { get; }
        List<ConsumerCreationClass> pendingConsumerTasks { get; }
        bool consumerCreationInProgress { get; }
        Dictionary<string, IConsumer> pendingResumeConsumers { get; }
        bool consumerPauseInProgress { get; }
        Dictionary<string, IConsumer> pendingPauseConsumers { get; }
        bool consumerResumeInProgress { get; }
        Dictionary<string, IConsumer> pendingCloseConsumers { get; }
        bool consumerCloseInProgress { get; }

        EnhancedEventEmitter observer { get; set; }

        void Close();
        RTCStatsReport GetStats();
        Task RestartIceAsync(IceParameters iceParameters);
        Task RestartIceAsync(List<RTCIceServer> iceServers);

        Task<Producer<ProducerAppData>> ProduceAsync<ProducerAppData>(
        ProducerOptions<ProducerAppData> options = null) where ProducerAppData : AppData, new();

        Task<Producer<ConsumerAppData>> ConsumeAsync<ConsumerAppData>(
        ProducerOptions<ConsumerAppData> options = null) where ConsumerAppData : AppData, new();

        Task<Producer<DataProducerAppData>> ProduceDataAsync<DataProducerAppData>(
        ProducerOptions<DataProducerAppData> options = null) where DataProducerAppData : AppData, new();

        Task<Producer<DataConsumerAppData>> ConsumeDataAsync<DataConsumerAppData>(
        ProducerOptions<DataConsumerAppData> options = null) where DataConsumerAppData : AppData, new();


        void PausePendingConsumers();
        void ResumePendingConsumers();
        void ClosePendingConsumers();



    }

    public class Transport<TTransportAppData> : EnhancedEventEmitter<TransportEvents>, ITransport
    {
        public string id { get; private set; }

        public bool isClosed { get; private set; }

        public string direction { get; private set; }

        public object extendedRtpCapabilities { get; private set; }

        public CanProduceByKind canProduceKind { get; private set; }

        public int maxSctpMessageSize { get; private set; }

        public HandlerInterface handlerInterface { get; private set; }

        public RTCIceGatheringState iceGatheringState { get; private set; }

        public RTCIceConnectionState connectionState { get; private set; }

        public object appData { get; private set; }

        public Dictionary<string, IProducer> producers { get; private set; }

        public Dictionary<string, IConsumer> consumers { get; private set; }

        public Dictionary<string, IDataConsumer> dataConsumers { get; private set; }

        public Dictionary<string, IDataProducer> datapPorducers { get; private set; }

        public bool _probatorConsumerCreated { get; private set; }

        public List<ConsumerCreationClass> pendingConsumerTasks { get; private set; }

        public bool consumerCreationInProgress { get; private set; }

        public Dictionary<string, IConsumer> pendingResumeConsumers { get; private set; }

        public bool consumerPauseInProgress { get; private set; }

        public Dictionary<string, IConsumer> pendingPauseConsumers { get; private set; }

        public bool consumerResumeInProgress { get; private set; }

        public Dictionary<string, IConsumer> pendingCloseConsumers { get; private set; }

        public bool consumerCloseInProgress { get; private set; }

        public EnhancedEventEmitter observer { get; set; }

        //Constructor
        public Transport(string _direction,string _id,IceParameters _iceParameters,List<RTCIceCandidate> _iceCandidate,
                        DtlsParameters _dtlsParameters,SctpParameters _sctpParameters,List<RTCIceServer> _iceServers,
                        RTCIceTransportPolicy _iceTransportPolicy,object _additionalSettings, object _proprietaryConstraints,
                        TTransportAppData _appData, HandlerInterface handlerFactory,object _extendedRtpCapabilities,
                        CanProduceByKind _canProduceKind) 
        {
            id = _id;
            direction = _direction;
            extendedRtpCapabilities = _extendedRtpCapabilities;
            canProduceKind = _canProduceKind;
            maxSctpMessageSize = _sctpParameters != null? _sctpParameters.maxMessageSize:0;

            // Clone and sanitize additionalSettings.
            //additionalSettings = utils.clone(additionalSettings) || { };
            //delete additionalSettings.iceServers;
            //delete additionalSettings.iceTransportPolicy;
            //delete additionalSettings.bundlePolicy;
            //delete additionalSettings.rtcpMuxPolicy;
            //delete additionalSettings.sdpSemantics;

            handlerInterface = new HandlerInterface("Unity");

            HandlerRunOptions handlerRunOptions = new HandlerRunOptions();
            handlerRunOptions.direction = _direction;
            handlerRunOptions.iceParameters = _iceParameters;
            handlerRunOptions.iceCandidates = _iceCandidate;
            handlerRunOptions.dtlsParameters = _dtlsParameters;
            handlerRunOptions.sctpParameters = _sctpParameters;
            handlerRunOptions.iceServers = _iceServers;
            handlerRunOptions.iceTransportPolicy = _iceTransportPolicy;
            handlerRunOptions.additionalSettings = _additionalSettings;
            handlerRunOptions.proprietaryConstraints = _proprietaryConstraints;
            handlerRunOptions.extendedRtpCapabilities = _extendedRtpCapabilities;

            handlerInterface.Run(handlerRunOptions);
            observer = new EnhancedEventEmitter<TransportObserverEvents>();
            if (_appData != null) appData = _appData ?? typeof(TTransportAppData).New<TTransportAppData>()!;
        }

        public void Close()
        {
            if (this.isClosed) return;

            isClosed = true;

            // Stop the AwaitQueue.
            //this._awaitQueue.stop();

            // Close the handler.
            //this._handler.close();

            connectionState = RTCIceConnectionState.Closed;

            foreach (var item in producers) 
            {
                item.Value.TransportClosed();
            }

            producers.Clear();

            foreach (var item in consumers)
            {
                item.Value.TransportClosed();
            }
            consumers.Clear();

            foreach (var item in datapPorducers)
            {
                item.Value.TransportClosed();
            }
            datapPorducers.Clear();


            foreach (var item in dataConsumers)
            {
                item.Value.TransportClosed();
            }
            dataConsumers.Clear();

            _ = observer.SafeEmit("close");

        }

        public RTCStatsReport GetStats()
        {
            throw new NotImplementedException();
        }

        public Task RestartIceAsync(IceParameters iceParameters)
        {
            throw new NotImplementedException();
        }

        public Task RestartIceAsync(List<RTCIceServer> iceServers)
        {
            throw new NotImplementedException();
        }

        public Task<Producer<ProducerAppData>> ProduceAsync<ProducerAppData>(ProducerOptions<ProducerAppData> options = null) where ProducerAppData : AppData, new()
        {
            throw new NotImplementedException();
        }

        public Task<Producer<ConsumerAppData>> ConsumeAsync<ConsumerAppData>(ProducerOptions<ConsumerAppData> options = null) where ConsumerAppData : AppData, new()
        {
            throw new NotImplementedException();
        }

        public Task<Producer<DataProducerAppData>> ProduceDataAsync<DataProducerAppData>(ProducerOptions<DataProducerAppData> options = null) where DataProducerAppData : AppData, new()
        {
            throw new NotImplementedException();
        }

        public Task<Producer<DataConsumerAppData>> ConsumeDataAsync<DataConsumerAppData>(ProducerOptions<DataConsumerAppData> options = null) where DataConsumerAppData : AppData, new()
        {
            throw new NotImplementedException();
        }

        public void PausePendingConsumers()
        {
            throw new NotImplementedException();
        }

        public void ResumePendingConsumers()
        {
            throw new NotImplementedException();
        }

        public void ClosePendingConsumers()
        {
            throw new NotImplementedException();
        }

        private void HandleHandler() 
        {
        
        }

        private void HandleProducer(Producer<AppData> _producer) 
        {
            _producer.On("@close", async _ =>
            {
                producers.Remove(_producer.id);
            });
        }

        private void HandleConsumer(Consumer<AppData> _consumer)
        {
            _consumer.On("@close", async _ =>
            {
                consumers.Remove(_consumer.id);
            });
        }

        private void HandleDataProducer(DataProducer<AppData> _dataProducer)
        {
            _dataProducer.On("@close", async _ =>
            {
                datapPorducers.Remove(_dataProducer.id);
            });
        }

        private void HandleDataConsumer(DataConsumer<AppData> _dataConsumer)
        {
            _dataConsumer.On("@close", async _ => 
            {
                dataConsumers.Remove(_dataConsumer.id);
            });
        }
    }


    public class TransportOptions<TTransportAppData>
    {
        public string id;
        public IceParameters iceParameters;
        public List<IceCandidate> IceCandidates = new List<IceCandidate>();
        public DtlsParameters dtlsParameters;
        public SctpParameters sctpParameters;
        public List<RTCIceServer> iceServers = new List<RTCIceServer>();
        public RTCIceTransportPolicy iceTransportPolicy;
        public object additionalSettings;
        public object proprietaryConstraints;
        public TTransportAppData appData;
    }

    
    public class CanProduceByKind
    {
        public bool audio;
        public bool video;
        Dictionary<string, bool> booleanDictionary = new Dictionary<string, bool>();
    }

    [Serializable]
    public class IceParameters 
    {
        public string usernameFragment;
        public string password;
        public bool iceLite;
    }

    [Serializable]
    public class IceCandidate 
    {
        public string foundation;
        public int priority;
        public string ip;
        public string protocol; //"udp" || "tcp"
        public int port;
        public string type;//'host' | 'srflx' | 'prflx' | 'relay'
        public string tcpType; //'active' | 'passive' | 'so';
    }

    [Serializable]
    public class DtlsParameters 
    {
        public DtlsRole role;
        public List<DtlsFingerprint> fingerprints = new List<DtlsFingerprint>();
    }

    public enum DtlsRole 
    {
        auto,
        client,
        server
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum FingerPrintAlgorithm
    {
        [StringValue("sha-1")]
        sha1,
        [StringValue("sha-224")]
        sha224,
        [StringValue("sha-256")]
        sha256,
        [StringValue("sha-384")]
        sha384,
        [StringValue("sha-512")]
        sha512
    }

    /*
     | 'sha-1'
	| 'sha-224'
	| 'sha-256'
	| 'sha-384'
	| 'sha-512';
     */


    [Serializable]
    
    public class DtlsFingerprint 
    {
        public FingerPrintAlgorithm algorithm;
        public string value;
    }

    [Serializable]
    public class PlainRtpParameters 
    {
        public string ip;
        public string ipVersion; //
        public int port;
    }

    public class TransportEvents 
    {
        public Tuple<Action<DtlsParameters>, Action<string>> Connect;
        public Action<RTCIceGatheringState> Icegatheringstatechange;
        public Action<RTCIceConnectionState> connectionstatechange;
        public Tuple<MediaKind, RtpParameters, object, Action<string>, Action<string>> Produce;
        public Tuple<SctpStreamParameters, string, string, object, Action<string>, Action<string>> ProduceData;
    }

    public class TransportObserverEvents 
    {
        public List<object> Close { get; set; } = new();
        public Tuple<IProducer> Newproducer { get; set; }
        public Tuple<IConsumer> Newconsumer { get; set; }
        public Tuple<IDataProducer> Newdataproducer { get; set; }
        public Tuple<IDataConsumer> Newdataconsumer { get; set; }
    }

    public class ConsumerCreationClass 
    {
        public ConsumerOptions<object> consumerOptions;

        public Action<IConsumer> resolve;
        public Action<string> reject;


        public ConsumerCreationClass(ConsumerOptions<object> _consumerOptions) 
        {
            consumerOptions = _consumerOptions;
        }
    }

}
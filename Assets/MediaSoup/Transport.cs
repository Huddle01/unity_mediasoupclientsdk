using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
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
using UnityEngine;
using Mediasoup.Ortc;

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
        ExtendedRtpCapabilities extendedRtpCapabilities { get; }
        CanProduceByKind canProduceKind { get; }
        int maxSctpMessageSize { get; }
        HandlerInterface handlerInterface { get; }
        RTCIceGatheringState iceGatheringState { get; }
        RTCIceConnectionState connectionState { get; }
        AppData appData { get; }
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

        EnhancedEventEmitter<TransportObserverEvents> observer { get; set; }

        void Close();
        RTCStatsReport GetStats();
        Task RestartIceAsync(IceParameters iceParameters);
        Task UpdateIceServers(List<RTCIceServer> iceServers);

        Task<Producer<ProducerAppData>> ProduceAsync<ProducerAppData>(Func<Unity.WebRTC.TrackKind, RtpParameters, AppData,Task<int>> GetProducerIdCallback,
        ProducerOptions<ProducerAppData> options = null) where ProducerAppData : AppData, new();

        Task<Consumer<AppData>> ConsumeAsync<ConsumerAppData>(
        ConsumerOptions options = null) where ConsumerAppData : AppData, new();

        Task<DataProducer<AppData>> ProduceDataAsync<DataProducerAppData>(Func<SctpStreamParameters, string, string,AppData, Task<int>> GetProducerIdCallback,
        DataProducerOptions options = null) where DataProducerAppData : AppData, new();

        Task<DataConsumer<AppData>> ConsumeDataAsync<DataConsumerAppData>(
        DataConsumerOptions options = null) where DataConsumerAppData : AppData, new();


        Task PausePendingConsumers();
        Task ResumePendingConsumers();
        Task ClosePendingConsumers();

    }

    public class Transport<TTransportAppData> : EnhancedEventEmitter<TransportEvents>, ITransport where TTransportAppData:AppData
    {
        public string id { get; private set; }

        public bool isClosed { get; private set; }

        public string direction { get; private set; }

        public ExtendedRtpCapabilities extendedRtpCapabilities { get; private set; }

        public CanProduceByKind canProduceKind { get; private set; }

        public int maxSctpMessageSize { get; private set; }

        public HandlerInterface handlerInterface { get; private set; }

        public RTCIceGatheringState iceGatheringState { get; private set; }

        public RTCIceConnectionState connectionState { get; private set; }

        public AppData appData { get; private set; }

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

        public EnhancedEventEmitter<TransportObserverEvents> observer { get; set; }

        public AwaitQueue awaitQueue;

        //Constructor
        public Transport(string _direction,string _id,IceParameters _iceParameters,List<IceCandidate> _iceCandidate,
                        DtlsParameters _dtlsParameters,SctpParameters _sctpParameters,List<RTCIceServer> _iceServers,
                        RTCIceTransportPolicy _iceTransportPolicy,object _additionalSettings, object _proprietaryConstraints,
                        TTransportAppData _appData, HandlerInterface handlerFactory, ExtendedRtpCapabilities _extendedRtpCapabilities,
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

            awaitQueue = new AwaitQueue();

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
            return null;
        }
        public Task RestartIceAsync(IceParameters iceParameters)
        {
            if (isClosed) 
            {
                throw new InvalidOperationException("Closed");
            }else if (iceParameters==null) 
            {
                throw new ArgumentNullException("missing iceParameters");
            }

            return awaitQueue.Push(async () => 
            {
                await handlerInterface.RestartIce(iceParameters);
                return new object();

            }, "transport.RestartIceAsync");
        }

        public Task UpdateIceServers(List<RTCIceServer> iceServers)
        {
            if (isClosed)
            {
                throw new InvalidOperationException("Closed");
            }
            else if (iceServers == null || iceServers.Count < 0)
            {
                throw new ArgumentNullException("missing iceParameters");
            }

            return awaitQueue.Push(async () =>
            {
                await handlerInterface.UpdateIceServers(iceServers);
                return new object();

            }, "transport.UpdateIceServers");

        }

        public async Task<Producer<ProducerAppData>> ProduceAsync<ProducerAppData>(Func<Unity.WebRTC.TrackKind, RtpParameters, AppData, Task<int>> GetProducerIdCallback, 
                                    ProducerOptions<ProducerAppData> options = null) where ProducerAppData : AppData, new()
        {
            if (isClosed)
            {
                throw new InvalidOperationException("Closed");
            } else if (options.track == null)
            {
                throw new ArgumentNullException("missing track");
            } else if (direction != "send")
            {
                throw new InvalidOperationException("not a sending transport");
            } else if (canProduceKind == null)// todo will write a better check system 
            {
                throw new InvalidOperationException($"cannot produce {options.track.Kind}");
            } else if (options.track.ReadyState == TrackState.Ended)
            {
                throw new InvalidOperationException("track ended");
            } else if (ListenerCount("connect") == 0 && connectionState == RTCIceConnectionState.New)
            {
                throw new Exception("no 'connect' listener set into this transport");
            } else if (ListenerCount("produce") == 0 && connectionState == RTCIceConnectionState.New)
            {
                throw new Exception("no 'produce' listener set into this transport");
            } else if (appData ==null) 
            {
                throw new InvalidCastException("if given, appData must be an object");
            }

            Producer<ProducerAppData> producer = null;

            await awaitQueue.Push(async () =>
            {
                List<RtpEncodingParameters> normalizedEncodings = new List<RtpEncodingParameters>();

                if (options.encodings == null)
                {
                    throw new ArgumentException("encodings must be an arra");
                } else if (options.encodings.Count == 0)
                {
                    normalizedEncodings = null;
                } else if (options.encodings!=null) 
                {
                     normalizedEncodings = options.encodings.Select(encoding =>
                    {
                        RtpEncodingParameters normalizedEncoding = new RtpEncodingParameters {Active = true };

                        if (!encoding.Active) 
                        {
                            normalizedEncoding.Active = false;
                        }

                        normalizedEncoding.Dtx = encoding.Dtx;
                        normalizedEncoding.ScalabilityMode = encoding.ScaleResolutionDownBy.Value.ToString();
                        normalizedEncoding.ScaleResolutionDownBy = encoding.ScaleResolutionDownBy.Value;
                        normalizedEncoding.MaxBitrate = encoding.MaxBitrate.Value;
                        normalizedEncoding.MaxFramerate = encoding.MaxFramerate.Value;
                        normalizedEncoding.AdaptivePtime = encoding.AdaptivePtime.Value;
                        normalizedEncoding.priority = encoding.priority;
                        normalizedEncoding.networkPriority = encoding.networkPriority;

                        return normalizedEncoding;
                    }).ToList();
                }

                HandlerSendOptions handlerSendOptions = new HandlerSendOptions
                {
                    track = options.track,
                    codec = options.codec,
                    codecOptions = options.codecOptions,
                    encodings = normalizedEncodings
                };

                HandlerSendResult handlerSendResult = await handlerInterface.Send(handlerSendOptions);
                try 
                {
                    ORTC.ValidateRtpParameters(handlerSendResult.rtpParameters);

                    //Adding a func param so that a method can be injected which can provide producer id
                    int num = await GetProducerIdCallback.Invoke(options.track.Kind, handlerSendResult.rtpParameters,appData);

                    producer = new Producer<ProducerAppData>(num.ToString(), handlerSendResult.localId, 
                        handlerSendResult.rtpSender,options.track, handlerSendResult.rtpParameters,options.stopTracks,
                        options.disableTrackOnPause,options.zeroRtpOnPause, options.appData);

                    producers.Add(producer.id, producer);
                    HandleProducer(producer);

                    _ = await observer.SafeEmit("newproducer",producer);
                    return producer;

                } catch (Exception ex) 
                {
                    throw new ArgumentException();
                }
            }, "transport.resumePendingConsumers");

            return producer;
        }

        public async Task<Consumer<AppData>> ConsumeAsync<ConsumerAppData>(ConsumerOptions options = null) where ConsumerAppData : AppData, new()
        {
            if (isClosed)
            {
                throw new InvalidOperationException("Closed");
            }
            else if (direction != "recv")
            {
                throw new InvalidOperationException("not a sending transport");
            }
            else if (string.IsNullOrEmpty(options.id))
            {
                throw new ArgumentNullException("missing id");
            } 
            else if (string.IsNullOrEmpty(options.producerId))
            {
                throw new ArgumentNullException("missing producer id");
            }
            else if (options.kind != "audio" || options.kind != "video")
            {
                throw new ArgumentNullException("unsupported media kind");
            }
            else if (ListenerCount("connect") == 0 && connectionState == RTCIceConnectionState.New) 
            {
                throw new ArgumentNullException("no 'connect' listener set into this transport");
            }
            else if (appData == null)
            {
                throw new InvalidCastException("if given, appData must be an object");
            }

            var canConsume = ORTC.CanReceive(options.rtpParameters,extendedRtpCapabilities);

            if (!canConsume) 
            {
                throw new InvalidOperationException("cannot comsume this producer");
            }

            var consumerCreationTask = new ConsumerCreationClass(options);

            pendingConsumerTasks.Add(consumerCreationTask);

            // There is no Consumer creation in progress, create it now.
            _ = Task.Run(() =>
              {
                  if (isClosed)
                  {
                      return;
                  }

                  if (!consumerCreationInProgress)
                  {
                      CreatePendingConsumer<ConsumerAppData>();
                  }
              });

            return await consumerCreationTask.Promise;

        }

        public async Task<DataProducer<AppData>> ProduceDataAsync<DataProducerAppData>(Func<SctpStreamParameters, string, string, AppData, Task<int>> GetProducerIdCallback,
                                                        DataProducerOptions options = null) where DataProducerAppData : AppData, new()
        {

            DataProducer<AppData> dataProducer = null;

            if (isClosed)
            {
                throw new InvalidOperationException("Closed");
            }
            else if (direction != "send")
            {
                throw new InvalidOperationException("not a sending transport");
            } else if (maxSctpMessageSize == -1)
            {
                throw new InvalidOperationException("SCTP not enabled by remote Transport");
            } else if (ListenerCount("connect") == 0 && connectionState == RTCIceConnectionState.New)
            {
                throw new ArgumentNullException("no 'connect' listener set into this transport");
            } else if (ListenerCount("produceData") == 0)
            {
                throw new ArgumentNullException("no 'producedata' listener set into this transport");
            }
            else if (appData == null)
            {
                throw new ArgumentNullException("if given, appData must be an object");
            }


            if (options.maxPacketLifeTime!=-1 || options.maxPacketLifeTime!=-1) 
            {
                options.ordered = false;
            }

             await awaitQueue.Push(async () =>
            {

                HandlerSendDataChannelOptions sendDataOption = new HandlerSendDataChannelOptions 
                {
                    ordered = options.ordered,
                    maxPacketLifeTime = options.maxPacketLifeTime,
                    maxRetransmits = options.maxRetransmits,
                    label = options.label,
                    protocol = options.protocol
                };

                HandlerSendDataChannelResult sendDataResult = await handlerInterface.SendDataChannel(sendDataOption);

                ORTC.ValidateSctpStreamParameters(sendDataResult.sctpStreamParameters);

                //Adding a func param so that a method can be injected which can provide producer id
                int num = await GetProducerIdCallback.Invoke(sendDataResult.sctpStreamParameters,options.label, options.protocol, 
                                                            options.dataConsumerAppData);

                dataProducer = new DataProducer<AppData>(num.ToString(), sendDataResult.dataChannel,
                                                        sendDataResult.sctpStreamParameters, options.dataConsumerAppData);

                datapPorducers.Add(dataProducer.id,dataProducer);
                HandleDataProducer(dataProducer);
                _ = observer.SafeEmit("newdataproducer", datapPorducers);
                return dataProducer;

            }, "transport.produceData()");

            return dataProducer;
        }

        public async Task<DataConsumer<AppData>> ConsumeDataAsync<DataConsumerAppData>(DataConsumerOptions options = null) where DataConsumerAppData : AppData, new()
        {
            SctpStreamParameters sctpStreamParams = Utils.Clone<SctpStreamParameters>(options.sctpStreamParameters);

            DataConsumer<AppData> dataConsumer = null;

            if (isClosed)
            {
                throw new InvalidOperationException("Closed");
            }
            else if (direction != "recv")
            {
                throw new InvalidOperationException("not a sending transport");
            }
            else if (maxSctpMessageSize == -1)
            {
                throw new InvalidOperationException("SCTP not enabled by remote Transport");
            } else if (string.IsNullOrEmpty(options.id))
            {
                throw new ArgumentNullException("missing id");
            } else if (string.IsNullOrEmpty(options.datProducerId)) 
            {
                throw new ArgumentNullException("missing data producer id");
            }
            else if (ListenerCount("connect") == 0 && connectionState == RTCIceConnectionState.New)
            {
                throw new ArgumentNullException("no 'connect' listener set into this transport");
            }
            else if (appData == null)
            {
                throw new ArgumentNullException("if given, appData must be an object");
            }

            ORTC.ValidateSctpStreamParameters(options.sctpStreamParameters);

            await awaitQueue.Push(async () =>
            {
                HandlerReceiveDataChannelOptions sendDataoption = new HandlerReceiveDataChannelOptions 
                {
                    label = options.label,
                    protocol = options.protocol,
                    sctpStreamParameters = options.sctpStreamParameters
                };

                RTCDataChannel sendDataResult = await handlerInterface.ReceiveDataChannel(sendDataoption);


                dataConsumer = new DataConsumer<AppData>(options.id, options.datProducerId,
                                                        sendDataResult, options.sctpStreamParameters,options.dataConsumerAppData);

                dataConsumers.Add(dataConsumer.id, dataConsumer);
                HandleDataConsumer(dataConsumer);
                _ = observer.SafeEmit("newdataconsumer", dataConsumer);
                return dataConsumers;

            }, "transport.produceData()");

            return dataConsumer;
        }

        public async Task CreatePendingConsumer<ConsumerAppData>() where ConsumerAppData:AppData
        {
            await awaitQueue.Push(async () =>
            {
                if (pendingConsumerTasks.Count==0) 
                {
                    Debug.LogError("createPendingConsumers() | there is no Consumer to be created");
                    return new object();
                }


                List<ConsumerCreationClass> tempPendingConsumerTask = new List<ConsumerCreationClass>(pendingConsumerTasks);
                pendingConsumerTasks.Clear();

                Consumer<AppData> videoConsumerForProbator = null;

                List<HandlerReceiveOptions> optionsList = new List<HandlerReceiveOptions>();

                foreach (ConsumerCreationClass task in tempPendingConsumerTask)
                {
                    HandlerReceiveOptions tempOption = new HandlerReceiveOptions 
                    {
                        kind = task.ConsumerOptions.kind,
                        streamId = task.ConsumerOptions.streamId,
                        rtpParameters = task.ConsumerOptions.rtpParameters,
                        trackId = task.ConsumerOptions.id
                    };

                    optionsList.Add(tempOption);
                }

                try 
                {
                    List<HandlerReceiveResult> results = await handlerInterface.Receive(optionsList);

                    for (int i = 0; i < results.Count; i++)
                    {
                        ConsumerCreationClass task = tempPendingConsumerTask[i];
                        HandlerReceiveResult result = results[i];

                        var tempId = task.ConsumerOptions.id;
                        var tempProducerId = task.ConsumerOptions.producerId;
                        var tempkind = task.ConsumerOptions.kind;
                        var tempRtpParam = task.ConsumerOptions.rtpParameters;
                        var tempAppData = task.ConsumerOptions.appData;

                        var tempLocalId = result.localId;
                        var tempRtpReceiver = result.rtpReceiver;
                        var tempTrack = result.track;

                        Consumer<AppData> tempConsumer = new Consumer<AppData>(tempId,tempLocalId,tempProducerId,
                                                            tempRtpReceiver,tempTrack, tempRtpParam, tempAppData);

                        consumers.Add(tempConsumer.id,tempConsumer);
                        HandleConsumer(tempConsumer);

                        if (!_probatorConsumerCreated && videoConsumerForProbator!=null && tempkind=="video")
                        {
                            videoConsumerForProbator = tempConsumer;
                        }

                        _ = observer.SafeEmit("newconsumer",tempConsumer);

                        task.ResolveConsumer(tempConsumer);

                    }

                } catch (Exception ex) 
                {
                    foreach (var task in pendingConsumerTasks)
                    {
                        task.RejectWithError(new Exception("Rejecting consumer"));
                    }
                }

                if (videoConsumerForProbator!=null) 
                {
                    try 
                    {
                        var probatorRtpParameters = ORTC.GenerateProbatorRtpParameters(videoConsumerForProbator.rtpParameters);

                        _ = await handlerInterface.Receive(new List<HandlerReceiveOptions> 
                        {
                            new HandlerReceiveOptions
                            {
                                trackId = "probator",
                                kind = "video",
                                rtpParameters = probatorRtpParameters
                            }
                        });

                        _probatorConsumerCreated = true;

                    } 
                    catch (Exception ex) 
                    {
                        throw new Exception("createPendingConsumers() | failed to create Consumer for RTP probation");
                    }
                }

                return new object();
            }, "transport.produceData()").ContinueWith(task=> 
            {
                consumerCreationInProgress = false;

                if (pendingConsumerTasks.Count>0) 
                {
                    CreatePendingConsumer<AppData>();
                }

            });
        }

        public async Task PausePendingConsumers()
        {
            consumerPauseInProgress = true;

            try
            {
                await awaitQueue.Push(async () =>
                {
                    if (pendingPauseConsumers.Count == 0)
                    {
                        Console.WriteLine("pausePendingConsumers() | there is no Consumer to be paused");
                        return new object();
                    }

                    var pendingPauseConsumersList = pendingPauseConsumers.Values.ToList();

                    pendingPauseConsumers.Clear();

                    try
                    {
                        List<string> localIds = pendingPauseConsumersList.Select(x => x.localId).ToList();
                        await handlerInterface.ResumeReceiving(localIds);
                        return new object();
                    }
                    catch (Exception error)
                    {
                        throw new Exception(error.Message);
                    }
                }, "transport.resumePendingConsumers");

            }
            finally
            {
                consumerPauseInProgress = false;

                if (pendingPauseConsumers.Count > 0)
                {
                    await PausePendingConsumers();
                }
            }
        }

        public async Task ResumePendingConsumers()
        {
            consumerResumeInProgress = true;

            try
            {
                await awaitQueue.Push(async () =>
                {
                    if (pendingResumeConsumers.Count == 0)
                    {
                        Console.WriteLine("resumePendingConsumers() | there is no Consumer to be resumed");
                        return new object();
                    }

                    var pendingCloseConsumersList = pendingResumeConsumers.Values.ToList();

                    pendingResumeConsumers.Clear();

                    try
                    {
                        List<string> localIds = pendingCloseConsumersList.Select(x => x.localId).ToList();
                        await handlerInterface.ResumeReceiving(localIds);
                        return new object();
                    }
                    catch (Exception error)
                    {
                        throw new Exception(error.Message);
                    }
                }, "transport.resumePendingConsumers");

            }
            finally
            {
                consumerResumeInProgress = false;

                if (pendingResumeConsumers.Count > 0)
                {
                    await ResumePendingConsumers();
                }
            }
        }

        public async Task ClosePendingConsumers()
        {
            consumerCloseInProgress = true;

            try
            {
                await awaitQueue.Push(async () =>
                {
                    if (pendingCloseConsumers.Count==0)
                    {
                        Console.WriteLine("closePendingConsumers() | There is no Consumer to be closed");
                        return new object();
                    }

                    var pendingCloseConsumersList = pendingCloseConsumers.Values.ToList();

                    pendingCloseConsumers.Clear();

                    try
                    {
                        List<string> localIds = pendingCloseConsumersList.Select(x => x.localId).ToList();
                        await handlerInterface.StopReceiving(localIds);
                        return new object();
                    }
                    catch (Exception error)
                    {
                        throw new Exception(error.Message);
                    }
                }, "transport.closePendingConsumers");

            }
            finally
            {
                consumerCloseInProgress = false;

                if (pendingCloseConsumers.Count > 0)
                {
                    await ClosePendingConsumers();
                }
            }

        }

        private void HandleHandler() 
        {
            handlerInterface.On("@connect", async (args) =>
            {
                var parameters = (Tuple<DtlsParameters, Action, Action<string>>)args[0];
                DtlsParameters dtlsParams = parameters.Item1;
                var connectCallback = parameters.Item2;
                var connectErrback = parameters.Item3;

                if (isClosed) { 
                    connectErrback("closed");
                    return;
                }

                _ = await handlerInterface.SafeEmit("connect", dtlsParams, connectCallback, connectErrback);
            });

            handlerInterface.On("@icegatheringstatechange", async (args) =>
            {
                RTCIceGatheringState _iceGatheringState = (RTCIceGatheringState)args[0];

                if (iceGatheringState == _iceGatheringState) 
                {
                    return;
                }

                iceGatheringState = _iceGatheringState;

                if (!isClosed)
                {
                    _ = await handlerInterface.SafeEmit("icegatheringstatechange", _iceGatheringState);
                }
            });

            handlerInterface.On("@connectionstatechange", async (args) =>
            {
                RTCIceConnectionState _connectionState = (RTCIceConnectionState)args[0];

                if (connectionState == _connectionState)
                {
                    return;
                }

                connectionState = _connectionState;

                if (!isClosed)
                {
                    _ = await handlerInterface.SafeEmit("connectionstatechange", _connectionState);
                }
            });


        }

        private void HandleProducer<TAppData>(Producer<TAppData> _producer) where TAppData: AppData
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
        public string address;
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
        public Tuple<DtlsParameters, Action, Action<string>> Connect;
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
        public ConsumerOptions ConsumerOptions { get; }
        public Task<Consumer<AppData>> Promise { get; }
        private TaskCompletionSource<Consumer<AppData>> TaskCompletionSource { get; } = new TaskCompletionSource<Consumer<AppData>>();


        public ConsumerCreationClass(ConsumerOptions consumerOptions)
        {
            ConsumerOptions = consumerOptions;
            Promise = TaskCompletionSource.Task;
        }

        public void ResolveConsumer(Consumer<AppData> consumer)
        {
            TaskCompletionSource.TrySetResult(consumer);
        }

        public void RejectWithError(Exception error)
        {
            TaskCompletionSource.TrySetException(new TaskCanceledException("Consumer creation failed", error));
        }

    }

}
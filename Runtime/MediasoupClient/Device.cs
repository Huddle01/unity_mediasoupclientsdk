using Newtonsoft.Json;
using Unity.WebRTC;
using Mediasoup.RtpParameter;
using Mediasoup.SctpParameter;
using Mediasoup.Ortc;
using System;
using System.Collections.Generic;
using Mediasoup.Transports;
using Mediasoup.Types;
using System.Threading.Tasks;

namespace Mediasoup 
{
    public class Device
    {
        private bool isLoaded;
        private RtpCapabilities rtpCapabilities;
        private SctpCapabilities sctpCapabilities;
        private HandlerInterface handler;
        private ExtendedRtpCapabilities extendedRtpCapabilities;
        private Dictionary<MediaKind, bool> canProduceByKind = new Dictionary<MediaKind, bool>();
        private RtpCapabilities recvRtpCapabilities;

        public Device() 
        {
            handler = new HandlerInterface("Unity");
        }

        ~Device() 
        {
        
        }

        public bool IsLoaded() 
        {
            return isLoaded;
        }

        public RtpCapabilities GetRtpCapabilities() 
        {
            return recvRtpCapabilities;
        }

        public SctpCapabilities GetSctpCapabilities() 
        {
            return sctpCapabilities;
        }

        public async Task Load(RtpCapabilities routerRtpCapabilities) 
        {
            if (isLoaded) { 
                throw new System.Exception("already loaded");
            }

            UnityEngine.Debug.Log("Got Router RTP Capabilities: " + JsonConvert.SerializeObject(routerRtpCapabilities));

            // This may throw
            ORTC.ValidateRtpCapabilities(routerRtpCapabilities);

            var nativeRtpCapabilities = await handler.GetNativeRtpCapabilities();

            UnityEngine.Debug.Log("Got native RTP Capabilities: " + JsonConvert.SerializeObject(nativeRtpCapabilities));

            // This may throw
            ORTC.ValidateRtpCapabilities(nativeRtpCapabilities);

            this.extendedRtpCapabilities = ORTC.GetExtendedRtpCapabilities(nativeRtpCapabilities, routerRtpCapabilities);

            UnityEngine.Debug.Log("Got Extended RTP Capabilities: " + JsonConvert.SerializeObject(extendedRtpCapabilities));

            canProduceByKind.Add(MediaKind.AUDIO, ORTC.CanSend(MediaKind.AUDIO, extendedRtpCapabilities));
            canProduceByKind.Add(MediaKind.VIDEO, ORTC.CanSend(MediaKind.VIDEO, extendedRtpCapabilities));

            recvRtpCapabilities = ORTC.GetRecvRtpCapabilities(extendedRtpCapabilities);

            UnityEngine.Debug.Log("Got Recieve RTP Capabilities: " + JsonConvert.SerializeObject(recvRtpCapabilities));

            // This may throw
            ORTC.ValidateRtpCapabilities(recvRtpCapabilities);

            sctpCapabilities = handler.GetNativeSctpCapabilities();

            // This may throw
            ORTC.ValidateSctpCapabilities(sctpCapabilities);

            isLoaded = true;

            UnityEngine.Debug.Log("Loaded");
        }

        public bool CanProduce(MediaKind kind) 
        {
            if (isLoaded == false)
            {
                throw new Exception("Not loaded");
            }
            else if (kind != MediaKind.AUDIO && kind != MediaKind.VIDEO) { 
                throw new ArgumentException("kind must be 'audio' or 'video'");
            }

            bool canProduce;

            if (canProduceByKind.TryGetValue(kind, out canProduce)) {
                return canProduce;
            }

            return false;
        }

        public Transport<AppData> CreateSendTransport(
            string id,
            IceParameters iceParameters,
            List<IceCandidate> iceCandidates,
            DtlsParameters dtlsParameters,
            SctpParameters sctpParameters,
            List<RTCIceServer>? iceServers,
            RTCIceTransportPolicy? iceTransportPolicy,
            object additionalSettings,
            object proprietaryConstraints,
            AppData appData
            )
        {

            foreach ( var candidate in iceCandidates )
            {
                if (candidate.address == "127.0.0.1") {
                    string ipv4 = IPManager.GetIP(ADDRESSFAM.IPv4);
                    candidate.address = ipv4;
                    candidate.ip = ipv4;
                }
            }

            ORTC.ValidateIceParameters(iceParameters);
            ORTC.ValidateIceCandidates(iceCandidates);

            UnityEngine.Debug.Log("Transport Constructor: " + JsonConvert.SerializeObject(extendedRtpCapabilities));

            return new Transport<AppData>("send", id, iceParameters, iceCandidates, dtlsParameters, sctpParameters, iceServers,
                iceTransportPolicy, additionalSettings, proprietaryConstraints, appData, handler, extendedRtpCapabilities, canProduceByKind);
        }

        public Transport<AppData> CreateRecvTransport(string id, IceParameters iceParameters, List<IceCandidate> iceCandidates,
            DtlsParameters dtlsParameters, SctpParameters sctpParameters, List<RTCIceServer> iceServers, RTCIceTransportPolicy iceTransportPolicy,
            object additionalSettings, object proprietaryConstraints, AppData appData) {

            foreach (var candidate in iceCandidates)
            {
                if (candidate.address == "127.0.0.1")
                {
                    string ipv4 = IPManager.GetIP(ADDRESSFAM.IPv4);
                    candidate.address = ipv4;
                    candidate.ip = ipv4;
                }
            }

            return new Transport<AppData>("recv", id, iceParameters, iceCandidates, dtlsParameters, sctpParameters, iceServers,
                iceTransportPolicy, additionalSettings, proprietaryConstraints, appData, handler, extendedRtpCapabilities, canProduceByKind);
        }

        public enum Direction { 
            send,
            recv
        }

        public Transport<AppData> CreateTransport(Direction direction, string id, IceParameters iceParameters, List<IceCandidate> iceCandidates,
            DtlsParameters dtlsParameters, SctpParameters sctpParameters, List<RTCIceServer> iceServers, RTCIceTransportPolicy iceTransportPolicy,
            object additionalSettings, object proprietaryConstraints, AppData appData) {
            if (isLoaded == false) {
                throw new Exception("Device not loaded");
            }

            return new Transport<AppData>(direction.ToString().ToLower(), id, iceParameters, iceCandidates, dtlsParameters, sctpParameters, iceServers,
                iceTransportPolicy, additionalSettings, proprietaryConstraints, appData, handler, extendedRtpCapabilities, canProduceByKind);
        }
    }
}



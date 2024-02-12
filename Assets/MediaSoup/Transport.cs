using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Unity.WebRTC;

/// <summary>
/// 
/// </summary>

namespace Mediasoup.Transports
{
    public class Transport
    {

    }

    [Serializable]
    public class TransportOptions
    {

    }

    [Serializable]
    public class CanProduceByKind
    {
    
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
        public DtlsFingerprint[] fingerprints;
    }

    public enum DtlsRole 
    {
        auto,
        client,
        server
    }

    public enum FingerPrintAlgorithm
    {
        sha1,
	    sha224,
	    sha256,
	    sha384,
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








}
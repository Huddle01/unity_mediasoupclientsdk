using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Unity.WebRTC;

namespace Mediasoup 
{
    public class Device
    {
        public Device() 
        {
        
        }

        ~Device() 
        {
        
        }

        public bool IsLoaded() 
        {
            return false;
        }

        public string GetRtpCapabilities() 
        {
            return null;
        }

        public string GetSctpCapabilities() 
        {
            return null;
        }

        public void Load(string routerRtpCapabilities,PeerConnection.Options peerConnectionOptions) 
        {
            
        }

        public bool CanProduce(string kind) 
        {
            return false;
        }

    }
}



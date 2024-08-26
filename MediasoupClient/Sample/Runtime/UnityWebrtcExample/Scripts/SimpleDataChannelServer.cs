using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp.Server;
using System.Net.Sockets;
using System.Net;

public class SimpleDataChannelServer : MonoBehaviour
{
    private WebSocketServer _webSocketServer;
    private string _serverIp4Address;
    private int _serverPort = 8080;

    private void Awake()
    {
        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily ==AddressFamily.InterNetwork) 
            {
                _serverIp4Address = ip.ToString();
                Debug.Log($"Server ip address : {_serverIp4Address}");
                break;
            }
        }

        _webSocketServer = new WebSocketServer($"ws://{_serverIp4Address}:{_serverPort}");
        _webSocketServer.AddWebSocketService<SimpleDataChannelService>($"/{nameof(SimpleDataChannelService)}");
        _webSocketServer.Start();

    }

    private void OnDestroy()
    {
        _webSocketServer.Stop();
    }
}

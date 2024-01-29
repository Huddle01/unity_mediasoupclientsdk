using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

public class SimpleDataChannelService : WebSocketBehavior
{

    protected override void OnOpen()
    {
        Debug.Log("SERVER SimpleDataChannelService Started!");
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        Debug.Log($"{ID} - DataChannel SERVER got message {e.Data}");

        //forward message to all other clients
        foreach (var id in Sessions.ActiveIDs)
        {
            if (id!=ID) 
            {
                Sessions.SendTo(e.Data, id);
            }
        }
    }
}

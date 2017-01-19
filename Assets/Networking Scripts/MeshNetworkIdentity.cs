using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;
using System;

public class MeshNetworkIdentity : MonoBehaviour {

    /// <summary>
    /// 
    ///     MeshNetworkIdentity is a component that must be attached to every
    ///     object that is to be synchronized across the mesh network. It receives
    ///     packets from the ServerManager based on its objectID, and routes them
    ///     to the correct component on the object (MeshNetworkTransform, etc). It
    ///     is packet-unaware, meaning that it will take any MeshPacket and route it
    ///     to the assigned component. MeshNetworkIdentity also is the dispatch point
    ///     for outgoing packets, where a component on the object (MeshNetworkTransform, etc)
    ///     will use the Identity to send the packets.
    /// 
    /// </summary>

    
    ushort objectID;
    IReceivesPacket<MeshPacket> attachedComponent;

    public void Start() {
        attachedComponent = gameObject.GetComponent<IReceivesPacket<MeshPacket>>();
    }

    public void HandlePacket(MeshPacket p) {
        
    }
}

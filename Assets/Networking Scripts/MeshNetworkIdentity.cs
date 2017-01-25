using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;
using System;

public class MeshNetworkIdentity : MonoBehaviour, IMeshSerializable {

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
    ///     As it is a MonoBehavior, there is no constructor method. When constructing
    ///     a new network object that uses MeshNetworkIdentity, use DeserializeAndApply
    ///     with the proper serialized byte data to set the properties of the MeshNetworkIdentity.
    ///
    /// 
    /// </summary>

    public const int NETWORK_IDENTITY_BYTE_SIZE = 5;


    ushort objectID;
    ushort prefabID;
    byte ownerID;
    public IReceivesPacket<MeshPacket> attachedComponent;

    

    public void HandlePacket(MeshPacket p) {
        
    }

    public byte[] GetSerializedBytes() {
        List<byte> output = new List<byte>();
        output.AddRange(BitConverter.GetBytes(objectID));
        output.AddRange(BitConverter.GetBytes(prefabID));
        output.Add(ownerID);
        if(output.ToArray().Length != NETWORK_IDENTITY_BYTE_SIZE) {
            Debug.LogError("Something's wrong with the network identity serialization");
            Debug.LogError("GetSerializedBytes returned " + output.ToArray().Length + "bytes");
        }
        return output.ToArray();
    }
    public void DeserializeAndApply(byte[] data) {
        objectID = BitConverter.ToUInt16(data, 0);
        prefabID = BitConverter.ToUInt16(data, 2);
        ownerID = data[4];
    }

    public ushort GetObjectID() {
        return objectID;
    }
    public void SetObjectID(ushort id) {
        objectID = id;
    }
    public ushort GetPrefabID() {
        return prefabID;
    }
    public void SetPrefabID(ushort id) {
        prefabID = id;
    }
    public byte GetOwnerID() {
        return ownerID;
    }
    public void SetOwnerID(byte id) {
        ownerID = id;
    }
}

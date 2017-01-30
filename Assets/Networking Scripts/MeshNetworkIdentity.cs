using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;
using System;

public class MeshNetworkIdentity : IReceivesPacket<MeshPacket>, IMeshSerializable {

    /// <summary>
    /// 
    ///     MeshNetworkIdentity is a script that allows objects to be 
    ///     synchronized across the mesh network. It receives
    ///     packets from the ServerManager based on its objectID, and routes them
    ///     to the correct component on the object (MeshNetworkTransform, etc). It
    ///     is packet-unaware, meaning that it will take any MeshPacket and route it
    ///     to the assigned component. MeshNetworkIdentity also is the dispatch point
    ///     for outgoing packets, where a component on the object (MeshNetworkTransform, etc)
    ///     will use the Identity to send the packets.
    ///     
    ///     MeshNetworkIdentity lives inside a container component on a networked prefab.
    ///     It contains a list attachedComponents, which keeps track of all of the networked
    ///     components living on the prefab. It will broadcast the incoming packet
    ///     indiscriminately to all components in this list. Each component must be
    ///     responsible for sanity-checking the incoming packet. The vast majority
    ///     of usage-cases will involve only one networked component attached to this
    ///     MeshNetworkIdentity. However, if there are multiple, you should use the
    ///     Utilities.PacketType enumeration to differentiate your data types.
    ///
    /// 
    /// </summary>

    public const int NETWORK_IDENTITY_BYTE_SIZE = 12;


    ushort objectID;
    ushort prefabID;
    ulong ownerID;

    //Not serialized across the network! This gets initialized and populated
    //when the container component is enabled. All IReceivesPacket components
    //attached to the relevant object will wind up in this List<>.
    public List<IReceivesPacket<MeshPacket>> attachedComponents;

    public MeshNetworkIdentity(ushort objectID, ushort prefabID, ulong ownerID) {
        this.objectID = objectID;
        this.prefabID = prefabID;
        this.ownerID = ownerID;
    }
    public MeshNetworkIdentity() {
        this.objectID = (ushort)ReservedObjectIDs.Unspecified;
        this.prefabID = (ushort)ReservedPrefabIDs.Unspecified;
        this.ownerID = (ulong)ReservedPlayerIDs.Unspecified;
    }

    public void ReceivePacket(MeshPacket p) {
        if(attachedComponents.Count == 0) {
            Debug.Log("This MeshNetworkIdentity has no associated components! Forgot to populate it?");
        }
        foreach(IReceivesPacket<MeshPacket> component in attachedComponents) {
            component.ReceivePacket(p);
        }
    }

    public byte[] GetSerializedBytes() {
        List<byte> output = new List<byte>();
        output.AddRange(BitConverter.GetBytes(objectID));
        output.AddRange(BitConverter.GetBytes(prefabID));
        output.AddRange(BitConverter.GetBytes(ownerID));
        if(output.ToArray().Length != NETWORK_IDENTITY_BYTE_SIZE) {
            Debug.LogError("Something's wrong with the network identity serialization");
            Debug.LogError("GetSerializedBytes returned " + output.ToArray().Length + "bytes");
        }
        return output.ToArray();
    }
    public void DeserializeAndApply(byte[] data) {
        objectID = BitConverter.ToUInt16(data, 0);
        prefabID = BitConverter.ToUInt16(data, 2);
        ownerID = BitConverter.ToUInt64(data, 4);
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
    public ulong GetOwnerID() {
        return ownerID;
    }
    public void SetOwnerID(ulong id) {
        ownerID = id;
    }
}

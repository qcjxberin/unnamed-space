using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

public class IdentityContainer : MonoBehaviour {

    private MeshNetworkIdentity identity;

    public void PopulateComponents() {
        if(identity != null) {
            List<IReceivesPacket<MeshPacket>> components = new List<IReceivesPacket<MeshPacket>>();
            components.AddRange(gameObject.GetComponents<IReceivesPacket<MeshPacket>>());
            identity.attachedComponents = components;
            foreach(IReceivesPacket<MeshPacket> c in components) {
                if(c is INetworked<MeshNetworkIdentity>) {
                    INetworked<MeshNetworkIdentity> networked = c as INetworked<MeshNetworkIdentity>;
                    networked.SetIdentity(identity);
                }
                else {
                    Debug.LogError("An attached component does not support the INetworked interface!");
                }
            }
        }
        else {
            Debug.Log("This container's MeshNetworkIdentity doesn't exist. Something very weird just happened.");
        }
    }

    public void SetIdentity(MeshNetworkIdentity id) {
        identity = id;
        PopulateComponents();
    }

    public MeshNetworkIdentity GetIdentity() {
        return identity;
    }
}

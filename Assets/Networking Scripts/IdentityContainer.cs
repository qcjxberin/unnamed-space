using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdentityContainer : MonoBehaviour {

    private MeshNetworkIdentity identity;

    public void SetIdentity(MeshNetworkIdentity id) {
        identity = id;
    }

    public MeshNetworkIdentity GetIdentity() {
        return identity;
    }
}

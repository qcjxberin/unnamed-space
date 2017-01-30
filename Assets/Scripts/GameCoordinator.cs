using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;

public class GameCoordinator : MonoBehaviour {

    public MeshNetwork meshnet;

    //Network Prefab Registry
    Dictionary<ushort, GameObject> networkPrefabs = new Dictionary<ushort, GameObject>();

    public void EnterGame(CSteamID lobbyID) {
        return;
    }

    //This simply instantiates a network prefab. It does not update the database.
    //Intended functionality is that this method is called by the the procedure
    //that has already registered this MeshNetworkIdentity on the database.
    public GameObject SpawnObject(MeshNetworkIdentity i) {

        if (networkPrefabs.ContainsKey(i.GetPrefabID()) == false) {
            Debug.LogError("NetworkPrefab registry error: Requested prefab ID does not exist.");
            return null;
        }
        GameObject g = Instantiate(networkPrefabs[i.GetPrefabID()]);
        IdentityContainer c = g.GetComponent<IdentityContainer>();
        if (c == null) {
            Debug.LogError("NetworkPrefab error: spawned prefab does not contain IdentityContainer");
            return null;
        }
        c.SetIdentity(i);
        return g;
    }
}

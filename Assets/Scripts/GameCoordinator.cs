using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using Utilities;

[RequireComponent(typeof(MeshNetwork))]
public class GameCoordinator : MonoBehaviour {

    /// <summary>
    ///     
    ///     GameCoordinator.cs
    ///     Copyright 2017 Finn Sinclair
    ///     
    ///     High-level coordinator for game status. Creates and destroys actual
    ///     gameobjects.
    /// 
    /// </summary>

    public MeshNetwork meshnet;

    //Network Prefab Registry
    Dictionary<ushort, GameObject> networkPrefabs = new Dictionary<ushort, GameObject>();

    public void Start() {
        DontDestroyOnLoad(gameObject);
        meshnet = gameObject.GetComponent<MeshNetwork>();
        GameObject[] prefabs = Resources.LoadAll<GameObject>("NetworkPrefabs");
        
        foreach(GameObject prefab in prefabs) {
            if(prefab.GetComponent<IdentityContainer>() == null) {
                Debug.LogError("A NetworkPrefab is missing an IdentityContainer.");
            }
            else{
                string prefabID = prefab.name.Substring(prefab.name.LastIndexOf('_') + 1);
                networkPrefabs.Add(ushort.Parse(prefabID), prefab);
            }
        }


        Debug.Log("GameCoordinator tried to register " + prefabs.Length + " network prefabs, succeeded with " + networkPrefabs.Count + ".");
        
    }

    public void EnterGame(CSteamID lobbyID) {
        return;
    }

    //This simply instantiates a network prefab. It does not update the database.
    //Intended functionality is that this method is called by the the procedure
    //that has already registered this MeshNetworkIdentity on the database.
    public GameObject SpawnObject(MeshNetworkIdentity i) {

        if(meshnet == null) {
            Debug.LogError("Trying to spawn object when underlying mesh network not intialized.");
            return null;
        }

        i.meshnetReference = meshnet; //set a reference to the mesh network
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

using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class spawnTest : NetworkBehaviour {
    public GameObject prefab;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.A)) {
            NetworkServer.Spawn((GameObject)Instantiate(prefab, transform.root));
        }
	}
}

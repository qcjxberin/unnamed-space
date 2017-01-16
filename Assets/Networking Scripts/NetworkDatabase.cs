using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkDatabase : MonoBehaviour {
    Player[] playerList = new Player[128]; //one slot for every possible UniqueID
    byte myId = 0; //uniqueID of zero indicates nonexistant player

	// Use this for initialization
	void Start () {
        playerList[0] = new Player();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public Player GetSelf() {
        return playerList[myId];
    }

    public Player LookupPlayer(byte id) {
        return playerList[id];
    }
}

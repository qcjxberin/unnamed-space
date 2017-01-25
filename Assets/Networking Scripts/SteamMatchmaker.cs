using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;

public class SteamMatchmaker : MonoBehaviour {

	// Use this for initialization
	void Start () {
        if (SteamManager.Initialized) {
            Debug.Log(SteamFriends.GetPersonaName());
        }
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}

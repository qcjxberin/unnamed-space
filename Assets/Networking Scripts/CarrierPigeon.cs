using UnityEngine;
using System.Collections;
using System;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using System.Reflection;
using System.Collections.Generic;

public class CarrierPigeon : NetworkManager {
    public bool ready = false;
    NetworkMatch networkMatch;
    FieldInfo clientIDField;

    Action<string, string, string> joinGameCallback;

	// Use this for initialization
	void Awake () {
        networkMatch = gameObject.AddComponent<NetworkMatch>();

        clientIDField = typeof(NetworkClient).GetField("m_ClientId", BindingFlags.NonPublic | BindingFlags.Instance);
        ready = true;
	}
	
	public void HostGame(string guid, string externalIP) { //include info to advertise
        string name = string.Join(":", new string[] { externalIP, Network.player.ipAddress, guid }); //Format the name of the match with the server information that clients will need
        networkMatch.CreateMatch(name, 2, true, "", externalIP, Network.player.ipAddress, 0, 0, OnMatchCreate);

    }

    public void JoinGame(Action<string, string, string> callback) {
        joinGameCallback = callback;
        networkMatch.ListMatches(0, 1, "", true, 0, 0, OnMatchList);
    }

    public override void OnMatchList(bool success, string extendedInfo, List<MatchInfoSnapshot> matchList) {
        if (!success) {
            Debug.Log("OnMatchList(): Matches not listed successfully");
            return;
        }
        if(matchList.Count == 0) {
            Debug.Log("No matches found.");
            return;
        }
        string[] data = matchList[0].name.Split(':');
        string exIP = data[0];
        string inIP = data[1];
        string guid = data[2];

        joinGameCallback(exIP, inIP, guid);
    }


}

using UnityEngine;
using System.Collections;
using System;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using System.Reflection;
using System.Collections.Generic;
using Utilities;

public class CarrierPigeon : NetworkManager {
    public bool ready = false;
    NetworkMatch networkMatch;
    FieldInfo clientIDField;

    Action<List<Game>> joinGameCallback;
    
	// Use this for initialization
	void Awake () {
        networkMatch = gameObject.AddComponent<NetworkMatch>();
        
        clientIDField = typeof(NetworkClient).GetField("m_ClientId", BindingFlags.NonPublic | BindingFlags.Instance);
        ready = true;
	}
	
	public void HostGame(string name, string password, string guid, string externalIP) { //include info to advertise
        string encodedName = string.Join(":", new string[] { externalIP, Network.player.ipAddress, guid, name, password }); //Format the name of the match with the server information that clients will need
        Debug.Log("Creating Match");
        networkMatch.CreateMatch(encodedName, 2, true, "", externalIP, Network.player.ipAddress, 0, 0, OnMatchCreate);
        
    }

    public void QueryGames(Action<List<Game>> callback) {
        joinGameCallback = callback;
        networkMatch.ListMatches(0, 4, "", true, 0, 0, OnMatchList);
    }

    public override void OnMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo) {
        base.OnMatchCreate(success, extendedInfo, matchInfo);
        Debug.Log("Match created.");
    }

    public override void OnMatchList(bool success, string extendedInfo, List<MatchInfoSnapshot> matchList) {
        
        if (!success) {
            Debug.Log("OnMatchList(): Matches not listed successfully");
            return;
        }
        if(matchList.Count == 0) {
            Debug.Log("No matches found.");
            joinGameCallback(new List<Game>());
            return;
        }
        Debug.Log("Found match information.");
        List<Game> games = new List<Game>();
        foreach(MatchInfoSnapshot info in matchList) {
            string[] data = info.name.Split(':');
            string exIP = data[0];
            string inIP = data[1];
            string guid = data[2];
            string name = data[3];
            string pass = data[4];
            games.Add(new Game(name, pass, guid, inIP, exIP));
        }
        //string[] data = matchList[0].name.Split(':');
        

        joinGameCallback(games);
    }


}

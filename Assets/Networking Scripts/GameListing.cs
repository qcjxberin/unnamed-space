using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;
using System;
using Steamworks;

public class GameListing : MonoBehaviour {
    public string lobbyName;
    public CSteamID id;
    public Action<CSteamID> callback;
    public UnityEngine.UI.Text label;
	
    public void SelectGame() {
        callback(id);
    }
    
}

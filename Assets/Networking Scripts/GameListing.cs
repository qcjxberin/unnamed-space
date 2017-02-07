using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;
using System;
using Steamworks;

public class GameListing : MonoBehaviour {
    public GameMatchmakingInfo info;
    public UnityEngine.UI.Text label;
	
    public void SelectGame() {
        info.callback(new CSteamID(info.id));
    }
        
    public void UpdateLabel() {
        label.text = info.name;
    }
    
}

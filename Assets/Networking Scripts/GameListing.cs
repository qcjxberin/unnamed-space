using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

public class GameListing : MonoBehaviour {
    public Game thisGame;
    public NetworkCoordinator nc;
    public UnityEngine.UI.Text label;
	
    public void SelectGame() {
        if(thisGame == null) {
            Debug.LogError("Selected game has no <Game> object attached!");
            return;
        }
        nc.SelectGame(thisGame);
    }
    
    public void UpdateLabel() {
        label.text = thisGame.name;
    }
}

using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Utilities;
using Steamworks;

public class UIController : MonoBehaviour {
    public GameObject WelcomeUIContainer;
    public GameObject GameInfoContainer;
    public GameObject PasswordContainer;
    public GameObject GameListContainer;
    public GameObject GameEntryPrefab;
    public RectTransform scrollArea;
    public GameObject ConnectingContainer;
    public GameObject ConnectedContainer;

    public Text PasswordLabel;

    Action<GamePublishingInfo> HostingInfoDelegate;
    Action<CSteamID> LobbySelectionDelegate;
    Action<string> PasswordDelegate;

    public bool isVR = false;
    UIMode mode = UIMode.Welcome;

    GameMatchmakingInfo[] lobbyStorage = new GameMatchmakingInfo[0];
    List<GameObject> entries = new List<GameObject>();

	// Use this for initialization
	void Awake () {
        WelcomeUIContainer.SetActive(false);
        GameInfoContainer.SetActive(false);
        GameListContainer.SetActive(false);
        ConnectingContainer.SetActive(false);
        ConnectedContainer.SetActive(false);
        SetUIMode(UIMode.Welcome);
        
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void SetUIMode(UIMode newmode) {
        mode = newmode;
        WelcomeUIContainer.SetActive(false);
        GameInfoContainer.SetActive(false);
        GameListContainer.SetActive(false);
        ConnectingContainer.SetActive(false);
        PasswordContainer.SetActive(false);
        switch (newmode) {
            case UIMode.Welcome:
            WelcomeUIContainer.SetActive(true);
            break;

            case UIMode.AskForGameInfo:
            GameInfoContainer.SetActive(true);
            break;

            case UIMode.DisplayGames:
            GameListContainer.SetActive(true);
            break;

            case UIMode.Connecting:
            ConnectingContainer.SetActive(true);
            break;

            case UIMode.AskForPassword:
            PasswordContainer.SetActive(true);
            break;

        }
    }

    public void RequestHostingInfo(Action<GamePublishingInfo> callback) {
        HostingInfoDelegate = callback;
        SetUIMode(UIMode.AskForGameInfo);
    }

    public void RequestLobbySelection(Action<CSteamID> callback, GameMatchmakingInfo[] games) {
        LobbySelectionDelegate = callback;
        SetUIMode(UIMode.DisplayGames);
        lobbyStorage = games;
        PopulateGames();
    }

    public void RequestPassword(Action<string> callback) {
        PasswordDelegate = callback;
        SetUIMode(UIMode.AskForPassword);
    }

    public void AlertPasswordMismatch() {
        if(mode == UIMode.AskForPassword) {
            PasswordLabel.text = "Incorrect password.";
        }
    }

    public void PopulateGames(GameMatchmakingInfo[] lobbies) {
        lobbyStorage = lobbies;
        PopulateGames();
    }

    public void PopulateGames() {
        foreach (GameObject entry in entries) {
            Destroy(entry, 0);
        }
        entries.Clear();
        scrollArea.sizeDelta = new Vector2(scrollArea.sizeDelta.x, (lobbyStorage.Length * 35) + 40);
        int i = 0;
        foreach (GameMatchmakingInfo info in lobbyStorage) {
            GameObject newListing = GameObject.Instantiate(GameEntryPrefab, scrollArea) as GameObject;
            entries.Add(newListing);
            RectTransform rt = newListing.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -5 - (35* i));
            GameListing gl = newListing.GetComponent<GameListing>();
            gl.info = info;
            gl.UpdateLabel();
            i++;
        }
    }

    public void ListingCallback(CSteamID id) {
        LobbySelectionDelegate(id);
    }

    public void PasswordCallback(string pwd) {
        PasswordDelegate(pwd);
    }

    public void GameSetupCallback(string gameName, string pwd) {
        GamePublishingInfo g = new GamePublishingInfo();
        g.name = gameName;
        g.password = pwd;
        HostingInfoDelegate(g);
    }
    
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

public class UIController : MonoBehaviour {
    public GameObject WelcomeUIContainer;
    public GameObject GameInfoContainer;
    public GameObject GameListContainer;
    public GameObject GameEntryPrefab;
    public RectTransform scrollArea;
    public GameObject ConnectingContainer;
    public GameObject ConnectedContainer;


    public NetworkCoordinator nc;

    public bool isVR = false;
    UIMode mode = UIMode.Welcome;

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

        }
    }

    public void PopulateGames(List<Game> games) {
        foreach (GameObject entry in entries) {
            Destroy(entry, 0);
        }
        entries.Clear();
        scrollArea.sizeDelta = new Vector2(scrollArea.sizeDelta.x, (games.Count * 35) + 40);
        int i = 0;
        foreach (Game g in games) {
            GameObject newListing = GameObject.Instantiate(GameEntryPrefab, scrollArea) as GameObject;
            entries.Add(newListing);
            RectTransform rt = newListing.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -5 - (35* i));
            GameListing gl = newListing.GetComponent<GameListing>();
            gl.nc = nc;
            gl.thisGame = g;
            gl.UpdateLabel();
            i++;
        }
    }
}

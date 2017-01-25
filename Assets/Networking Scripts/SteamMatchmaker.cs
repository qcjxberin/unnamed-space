using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;

public class SteamMatchmaker : MonoBehaviour {
    //called when lobby is created
    private CallResult<LobbyCreated_t> m_LobbyCreated;

	
	private void OnEnable () {
        if (SteamManager.Initialized) {
            Debug.Log(SteamFriends.GetPersonaName());
            m_LobbyCreated = CallResult<LobbyCreated_t>.Create(OnCreateLobby);
            MakeLobby();
        }
	}
	
	// Update is called once per frame
	void FixedUpdate () {
        
    }

    public void MakeLobby() {
        SteamAPICall_t handle = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePrivate, 4);
        m_LobbyCreated.Set(handle);
    }

    private void OnCreateLobby(LobbyCreated_t pCallback, bool bIOFailure) {
        Debug.Log("Result: " + pCallback.m_eResult);
        Debug.Log("ID: " + pCallback.m_ulSteamIDLobby);

        CSteamID testID = SteamMatchmaking.GetLobbyMemberByIndex(new CSteamID(pCallback.m_ulSteamIDLobby), 0);
        Debug.Log("My id: " + SteamUser.GetSteamID() + ", server member id: " + testID);
    }
}

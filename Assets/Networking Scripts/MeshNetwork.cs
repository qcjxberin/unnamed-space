using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using Utilities;

public class MeshNetwork : MonoBehaviour {
    UIController networkUIController;
    NetworkDatabase database;
    GameCoordinator game;

    //Current lobby
    CSteamID lobby;

    //Steamworks callbacks/callresults
    CallResult<LobbyCreated_t> m_LobbyCreated;
    public void OnEnable() {
        database = gameObject.GetComponent<NetworkDatabase>();
        game = gameObject.GetComponent<GameCoordinator>();
        if (SteamManager.Initialized) {
            m_LobbyCreated = CallResult<LobbyCreated_t>.Create(OnCreateLobby);
        }
        else {
            Debug.LogError("SteamManager not initialized!");
        }
    }

    //Create a networked player given SteamID information.
    //Pass in SteamUser.GetSteamID() for <id> if you want to
    //construct your own player object.
    public Player ConstructPlayer(bool isProvider, CSteamID id) {
        Player p = new Player();
        string name = SteamFriends.GetFriendPersonaName(id);
        if (name.Equals("")) {
            Debug.LogError("Name request returned blank, (probably) lobby not ready");
            return null;
        }
        p.SetName(name);
        p.SetSteamID(id);
        if (isProvider) {
            p.SetUniqueID((byte)ReservedPlayerIDs.Provider);
        }
        else {
            byte byteID = database.RequestAvailableID();
            if (byteID >= (byte)ReservedPlayerIDs.FirstAvailable)
                p.SetUniqueID(byteID);
            else {
                Debug.LogError("Can't find available byte ID.");
                return null;
            }
        }
        p.SetPrivateKey("key");
        return p;
    }


    #region Provider-oriented code

    public void HostGame() {
        //First, we get our own player object, and we make ourselves the provider.
        Player me = ConstructPlayer(true, SteamUser.GetSteamID());
        database.AddPlayer(me);
        database.SetMyID(me.GetUniqueID());

        //Actually create the lobby. Password info, etc, will be set after this.
        m_LobbyCreated.Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePrivate, 4));
    }
    public void OnCreateLobby(LobbyCreated_t pCallback, bool bIOFailure) {
        if(pCallback.m_eResult != EResult.k_EResultOK) {
            Debug.LogError("Lobby creation didn't work.");
            return;
        }
        lobby = new CSteamID(pCallback.m_ulSteamIDLobby);
        //Then, we switch the server UI so that the player can enter the information.
        //The UI has buttons that contain references to various callbacks here.
        networkUIController.RequestHostingInfo(OnGetHostingInfo);
    }
    public void OnGetHostingInfo(GameInfo info) {

        //Set basic info
        SteamMatchmaking.SetLobbyData(lobby, "name", info.name);
        SteamMatchmaking.SetLobbyData(lobby, "pwd", info.password);

        //Now that we have the password in place, we can make it public
        SteamMatchmaking.SetLobbyType(lobby, ELobbyType.k_ELobbyTypePublic);

        game.EnterGame(lobby);
    }

    #endregion

    #region Client-oriented code

    public void JoinGame() {
        //First, we need the UI to show the player the available lobbies.
        networkUIController.RequestLobbySelection(OnGetLobbySelection);
    }
    public void OnGetLobbySelection(CSteamID selectedLobby) {

    }

    #endregion

}
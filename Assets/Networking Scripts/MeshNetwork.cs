using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using Utilities;

[RequireComponent(typeof(UIController))]
public class MeshNetwork : MonoBehaviour {
    UIController networkUIController;
    NetworkDatabase database;
    GameCoordinator game;
    MeshEndpoint endpoint;
    //Current lobby
    CSteamID lobby;

    //Steamworks callbacks/callresults
    CallResult<LobbyCreated_t> m_LobbyCreated;
    CallResult<LobbyEnter_t> m_JoinedLobby;


    void OnEnable() {
        Debug.Log("hello!");

        networkUIController = gameObject.GetComponent<UIController>();

        database = gameObject.AddComponent<NetworkDatabase>();
        database.meshnet = this;
        endpoint = gameObject.AddComponent<MeshEndpoint>();
        endpoint.ndb = database;
        game = gameObject.AddComponent<GameCoordinator>();
        
        if (SteamManager.Initialized) {
            m_LobbyCreated = CallResult<LobbyCreated_t>.Create(OnCreateLobby);
            m_JoinedLobby = CallResult<LobbyEnter_t>.Create(OnJoinedLobby);
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

    public void RoutePacket(MeshPacket p) {
        
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
        lobby = selectedLobby;
        SteamMatchmaking.JoinLobby(selectedLobby);
    }

    protected void OnJoinedLobby(LobbyEnter_t pCallback, bool bIOfailure) {
        networkUIController.RequestPassword(OnGetPassword);
    }

    protected void OnGetPassword(string pwd) {
        if(SteamMatchmaking.GetLobbyData(lobby, "pwd").Equals(pwd)) {
            RegisterWithProvider();
        }
        else {
            Debug.Log("Password doesn't match!");
            SteamMatchmaking.LeaveLobby(lobby);
        }
    }

    protected void RegisterWithProvider() {

        //Create a PlayerJoin packet, which the provider will use as a trigger to
        //register a new player. It will update its internal database, and will
        //distribute this info as a normal DatabaseUpdate.
        MeshPacket p = new MeshPacket(new byte[0],
            PacketType.PlayerJoin,
            (byte)ReservedPlayerIDs.Unspecified,
            (byte)ReservedPlayerIDs.Provider,
            (byte)ReservedObjectIDs.Unspecified,
            (byte)ReservedObjectIDs.DatabaseObject);

        byte[] packetData = p.GetSerializedBytes();

        SteamNetworking.SendP2PPacket(SteamMatchmaking.GetLobbyOwner(lobby),
            packetData,
            (uint)packetData.Length,
            EP2PSend.k_EP2PSendReliable);

        //Soon, we will receive a DatabaseUpdate with all of the up to date database information,
        //including our own player object!

    }

    #endregion
    
}
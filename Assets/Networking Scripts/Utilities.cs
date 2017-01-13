using UnityEngine;
using System.Collections;

public class PeerInfo {

    public int connectionId;
    
    public PeerInfo(int id) {
        connectionId = id;
    }
}

public class OutboundPunchContainer {
    public string serverExternalIP;
    public string serverInternalIP;
    public string serverGUID;
    public int punchID;

    public OutboundPunchContainer(string serverExIP, string serverInIP, string serverID, int id) {
        serverExternalIP = serverExIP;
        serverInternalIP = serverInIP;
        serverGUID = serverID;
        punchID = id;
    }
}

public class Player {
    string displayName;
    string uniqueName;
    string address;
    string privateKey;

    public Player() {
        displayName = "DefaultPlayerName";
        uniqueName = "DefaultUniqueName";
        address = "0.0.0.0";
        privateKey = "DefaultPrivateKey";
    }
}

public class Game{
    public string name;
    string password;
    string providerGUID;
    string providerInternalIP;
    string providerExternalIP;

    public Game() {
        name = "DefaultGameName";
        password = "";
        providerGUID = "";
        providerInternalIP = "0.0.0.0";
        providerExternalIP = "0.0.0.0";
    }

    public Game(string nameIn, string passwordIn, string guidIN, string inIPIn, string exIPIn) {
        name = nameIn;
        password = passwordIn;
        providerGUID = guidIN;
        providerInternalIP = inIPIn;
        providerExternalIP = exIPIn;
    }
}



namespace Utilities {
    public enum NATStatus {
        Uninitialized,
        Idle,
        ConnectingToFacilitator,
        Listening,
        Punching,
        Rebooting,
        AfterPunching
    }

    public enum CoordinatorStatus {
        Uninitialized,
        Idle,
        CreatingGame, //setting up game
        Hosting, //Providing a game.
        Joining  //Joining a friend's game.
    }

    public enum UIMode {
        Welcome,
        AskForGameInfo,
        DisplayGames
    }
}


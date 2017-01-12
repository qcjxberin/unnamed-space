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


namespace Utilities {
    public enum NATStatus { Uninitialized, Idle, ConnectingToFacilitator, Listening, Punching, Rebooting, AfterPunching};
}


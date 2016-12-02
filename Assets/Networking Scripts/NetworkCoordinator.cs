using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NetworkCoordinator : MonoBehaviour {
    ServerManager sm = new ServerManager();
    NATHelper nath;
    CarrierPigeon pigeon;
    int punchCounter = 0;

    List<OutboundPunchContainer> outboundPunches = new List<OutboundPunchContainer>();
    

	// Use this for initialization
	void Start () {
        nath = gameObject.GetComponent<NATHelper>();
        pigeon = gameObject.AddComponent<CarrierPigeon>();
	}
	
	public void CoordinatorHostGame() {
        if(pigeon.ready && nath.isReady) {
            pigeon.HostGame(nath.guid, nath.externalIP);
        }
            
            
    }
    public void CoordinatorJoinGame() {
        if (pigeon.ready && nath.isReady) {
            pigeon.JoinGame(OnInfoAcquired); //Once the match is successfully joined, OnInfoAcquired is called with the pigeon's data
        }
    }

    public void OnInfoAcquired(string serverExternalIP, string serverInternalIP, string serverGUID) { //Carrier pigeon has delivered game owner's data. Punch through to game owner.
        EnterPunchingMode(); //Reboot NAT system. This may take some time. Punch() will wait for it.
        OutboundPunchContainer pc = new OutboundPunchContainer(serverExternalIP, serverInternalIP, serverGUID, punchCounter); //Record pigeon data for future use.
        punchCounter++;
        outboundPunches.Add(pc);
        StartCoroutine(Punch(serverGUID)); //Punch() will punch to GUID when the NAT system is ready to do so.
    }

    void onHolePunchedClient(int listenPort, int connectPort, string serverGUID) {
        Debug.Log("Punched hole to server GUID " + serverGUID + ", about to make connection.");
        
        int portToConnectTo = connectPort;
        string addressToConnectTo = "";
        OutboundPunchContainer punchInfo = null;
        //retrieve outbound punch information
        foreach(OutboundPunchContainer pc in outboundPunches) {
            if(pc.serverGUID == serverGUID) {
                punchInfo = pc;
            }
        }
        if(punchInfo == null) {
            Debug.Log("Can't find history of this hole being requested");
            return;
        }

        if(punchInfo.serverExternalIP == nath.externalIP) { //We're on the same LAN
            if(punchInfo.serverInternalIP == Network.player.ipAddress) { //We're on the same computer!
                addressToConnectTo = "127.0.0.1";
            } else {
                addressToConnectTo = punchInfo.serverInternalIP; //Just stay inside the LAN.
            }
        }
        sm.SpawnServer(listenPort);


    }

    IEnumerator Punch(string guid) {
        while (!nath.isReady)
            yield return new WaitForEndOfFrame();
        nath.punchThroughToServer(guid, onHolePunchedClient);
    }
    public void EnterPunchingMode() {
        nath.RestartNAT();
    }
}

class OutboundPunchContainer {
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

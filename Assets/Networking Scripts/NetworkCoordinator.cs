using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.UI;
using Utilities;

public class NetworkCoordinator : MonoBehaviour {
    ServerManager sm = new ServerManager();
    NATHelper nath;
    CarrierPigeon pigeon;
    int punchCounter = 0;
    public Text debugText;
    List<OutboundPunchContainer> outboundPunches = new List<OutboundPunchContainer>();

    public Text NATStatusText;

    const float PUNCH_TIME_SPACING = 0.2f;
    float lastPunchTime = 0;

	// Use this for initialization
	void Start () {
        NetworkTransport.Init();
        nath = gameObject.GetComponent<NATHelper>();
        pigeon = gameObject.GetComponent<CarrierPigeon>();
	}

    void Update() {
        sm.Receive();
        UpdateDebug();
        UpdateNATStatus();
        if (outboundPunches.Count > 0 && nath.mode == NATStatus.Idle) {
            
            lastPunchTime = Time.time;
            OutboundPunchContainer toPunch = outboundPunches[0];
            outboundPunches.RemoveAt(0);
            StartCoroutine(RobustPunch(toPunch));

        }
        

    }
	
	public void CoordinatorHostGame() {
        if(pigeon.ready && nath.isReady) {
            pigeon.HostGame(nath.guid, nath.externalIP);
        }
        Debug.Log("Beginning to listen for incoming punches.");
        StartCoroutine(WaitForPunches());
            
    }

    void onHolePunchedServer(int listenPort, string exIP) {
        Debug.Log("Server receieved hole punch, spawning server on port");
        sm.SpawnServer(listenPort);
        StartCoroutine(WaitForPunches());
    }

    public void CoordinatorJoinGame() {
        if (pigeon.ready && nath.isReady) {
            pigeon.JoinGame(OnInfoAcquired); //Once the match is successfully joined, OnInfoAcquired is called with the pigeon's data
        }
    }

    public void OnInfoAcquired(string serverExternalIP, string serverInternalIP, string serverGUID) { //Carrier pigeon has delivered game owner's data. Punch through to game owner.
        Debug.Log("Acquired match info. Preparing to punch.");
        
        OutboundPunchContainer pc = new OutboundPunchContainer(serverExternalIP, serverInternalIP, serverGUID, punchCounter); //Record pigeon data for future use.
        QueuePunch(pc);
        //RobustPunch(serverGUID, pc); //Punch() will punch to GUID when the NAT system is ready to do so.
        //PunchNow(serverGUID);
    }   

    void QueuePunch(OutboundPunchContainer pc) {
        outboundPunches.Insert(0, pc);
    }

    void onHolePunchedClient(int listenPort, int connectPort, string serverGUID, OutboundPunchContainer pc) {
        //return;
        Debug.Log("Punched hole to server GUID " + serverGUID + ", about to make connection.");
        
        int portToConnectTo = connectPort;
        string addressToConnectTo = "";
        OutboundPunchContainer punchInfo = pc;
        
        if(punchInfo == null) {
            Debug.Log("Punch information missing.");
            return;
        }

        if(punchInfo.serverGUID != serverGUID) {
            Debug.Log("OutboundPunchContainer data GUID does not match punched GUID.");
            return;
        }

        if(punchInfo.serverExternalIP == nath.externalIP) { //We're on the same LAN
            Debug.Log("Target is on the same LAN!");
            if(punchInfo.serverInternalIP == Network.player.ipAddress) { //We're on the same computer!
                Debug.Log("Target is on the same computer!");
                addressToConnectTo = "127.0.0.1";
            } else {
                addressToConnectTo = punchInfo.serverInternalIP; //Just stay inside the LAN.
            }
        }
        else {
            addressToConnectTo = punchInfo.serverExternalIP;
        }
        //ServerManager.SpawnClient() creates an ordinary node and then connects it to the specified target.
        sm.SpawnServerThenConnect(listenPort, addressToConnectTo, connectPort);
        nath.RebootNAT();
        Debug.Log("OnHolePunchedClient in NetworkCoordinator has finished.");

    }

    void onPunchFailClient(OutboundPunchContainer pc) {
        Debug.Log("ALERT: Hole Punch Failed. Either your router or your friend's router does not support NAT punchthrough.");
        nath.RebootNAT();
    }
    
    public IEnumerator RobustPunch(OutboundPunchContainer pc) {
        while(nath.mode != NATStatus.Idle) {
            yield return new WaitForEndOfFrame();
        }
        Debug.Log("Punch() routine is about to punchThroughToServer()");
        nath.punchThroughToServer(pc.serverGUID, 8f, onHolePunchedClient, onPunchFailClient, pc);
        yield break;
    }

    IEnumerator WaitForPunches() {
        Debug.Log("IEnumerator WaitForPunches()");
        while (!nath.isReady)
            yield return new WaitForEndOfFrame();
        nath.startListeningForPunchthrough(onHolePunchedServer);
        yield break;
    }
    

    public void PingAll() {
        sm.PingAllPeers();
        
    }

    public void UpdateDebug() {
        string output = "";
        foreach(Server s in sm.servers) {
            output += "Server " + s.getSocketID() + " watching port " + s.getPort() + "\n";
            foreach(PeerInfo pi in s.getPeers()) {
                output += "     Peer #" + pi.connectionId + "\n";
            }
        }
        debugText.text = output;
    }

    public void UpdateNATStatus() {
        //if (NATStatusText == null)
            //return;
        NATStatusText.text = "NAT Status: " + nath.mode;
    }

    
}



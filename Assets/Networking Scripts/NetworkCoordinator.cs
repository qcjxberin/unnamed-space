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
    UIController serverUI;
    int punchCounter = 0;
    public Text debugText;
    List<OutboundPunchContainer> outboundPunches = new List<OutboundPunchContainer>();

    public Text NATStatusText;

    const float PUNCH_TIME_SPACING = 0.2f;
    float lastPunchTime = 0;

    public CoordinatorStatus status = CoordinatorStatus.Uninitialized;

	// Use this for initialization
	void Start () {
        NetworkTransport.Init();
        nath = gameObject.GetComponent<NATHelper>();
        pigeon = gameObject.GetComponent<CarrierPigeon>();
        serverUI = gameObject.GetComponent<UIController>();
        status = CoordinatorStatus.Idle;
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
        status = CoordinatorStatus.CreatingGame;
        StartCoroutine(WaitUntilAbleToHost()); //this exists because UIButtons can't start coroutines
            
    }

    IEnumerator WaitUntilAbleToHost() {
        float startTime = Time.time;
        while(!(pigeon.ready && nath.mode == NATStatus.Idle)) {
            if (Time.time - startTime > 8f) { //hardcoded timeouts are lame
                Debug.Log("WaitUntilAbleToHost() timed out. Network components weren't ready in time.");
                yield break;
            }
            if (nath.mode != NATStatus.Idle)
                Debug.Log("WaitUntilAbleToHost is rebooting NAT.");
                nath.RebootNAT();
            yield return new WaitForEndOfFrame();
        }
        serverUI.SetUIMode(UIMode.AskForGameInfo);
    }

    //this gets called by the Publish button in the GameInfoContainer
    public void PublishGameInfo(string gameName, string gamePassword) {
        if(status != CoordinatorStatus.CreatingGame) {
            Debug.Log("Something tried to publish a game when we weren't trying to do that.");
            return;
        }
        pigeon.HostGame(gameName, gamePassword, nath.guid, nath.externalIP);
    }

    void onHolePunchedServer(int listenPort, string exIP) {
        Debug.Log("Server receieved hole punch, spawning server on port");
        sm.SpawnServer(listenPort);
        StartCoroutine(WaitForPunches());
    }


    //



    public void CoordinatorJoinGame() {
        StartCoroutine(WaitUntilAbleToJoin());
    }

    public void RefreshGames() {
        StartCoroutine(WaitUntilAbleToJoin());
    }
    IEnumerator WaitUntilAbleToJoin() {
        float startTime = Time.time;
        while (!(pigeon.ready && nath.mode == NATStatus.Idle)) {
            if (Time.time - startTime > 8f) { //hardcoded timeouts are lame
                Debug.Log("WaitUntilAbleToJoin() timed out. Network components weren't ready in time.");
                yield break;
            }
            if (nath.mode != NATStatus.Idle)
                Debug.Log("WaitUntilAbleToJoin is rebooting NAT.");
                nath.RebootNAT();
            yield return new WaitForEndOfFrame();
        }
        pigeon.QueryGames(DisplayGames);

    }

    void DisplayGames(List<Game> games) {
        serverUI.SetUIMode(UIMode.DisplayGames);
        serverUI.PopulateGames(games);
    }

    public void SelectGame(Game g) {
        if(status != CoordinatorStatus.Joining) {
            Debug.Log("Something tried to select a game when we weren't trying to do that.");
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

    void onHolePunchedClient(int listenPort, int connectPort, OutboundPunchContainer pc) {
        if (pc == null) {
            Debug.Log("Punch information missing.");
            return;
        }

        Debug.Log("Holepunch callback to server GUID " + pc.serverGUID + ", about to make connection.");
        
        int portToConnectTo = connectPort;
        string addressToConnectTo = "";
        
        if(pc.serverExternalIP == nath.externalIP) { //We're on the same LAN
            Debug.Log("Target is on the same LAN!");
            if(pc.serverInternalIP == Network.player.ipAddress) { //We're on the same computer!
                Debug.Log("Target is on the same computer!");
                addressToConnectTo = "127.0.0.1";
            } else {
                addressToConnectTo = pc.serverInternalIP; //Just stay inside the LAN.
            }
        }
        else {
            addressToConnectTo = pc.serverExternalIP;
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
        nath.punchThroughToServer(8f, onHolePunchedClient, onPunchFailClient, pc);
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



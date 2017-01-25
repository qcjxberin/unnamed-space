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
    NetworkDatabase ndb;
    int punchCounter = 0;
    public Text debugText;
    List<OutboundPunchContainer> outboundPunches = new List<OutboundPunchContainer>();

    public Text NATStatusText;
    const float PUNCH_TIME_SPACING = 0.2f;
    float lastPunchTime = 0;

    //This name should be set from UI, from SteamAPI, etc.
    string nameToUse = "TestName";

    private bool isProvider = false;

    //False until the provider acknowledges this player joining the game.
    private bool providerAcknowledge = false;

    public CoordinatorStatus status = CoordinatorStatus.Uninitialized;

    //As NetworkCoordinator punches to various players, it will write its history
    //to this dictionary. It can be used to verify whether the system has connected
    //to a certain player.
    Dictionary<Player, ConnectionStatus> connectionHistory = new Dictionary<Player, ConnectionStatus>();
    bool isFirstConnection = true;

	// Use this for initialization
	void Start () {
        NetworkTransport.Init();
        nath = gameObject.GetComponent<NATHelper>();
        pigeon = gameObject.GetComponent<CarrierPigeon>();
        serverUI = gameObject.GetComponent<UIController>();
        serverUI.nc = this;
        ndb = gameObject.GetComponent<NetworkDatabase>();
        sm.ndb = ndb;
        sm.coordinator = this;
        sm.voipReceiver = gameObject.GetComponent<VoipReceiver>();
        status = CoordinatorStatus.Idle;
        Utilities.Testing.DebugDatabaseSerialization();
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


        if(ndb.GetPlayers().Values.Count > 0) {
            foreach(Player p in ndb.GetPlayers().Values) {
                if(p.GetUniqueID() != ndb.GetSelf().GetUniqueID()
                    && connectionHistory.ContainsKey(p) == false) { //if we have not made a P2P link yet

                    QueuePunch(p.ConstructPunchContainer(false));
                }
            }
        }
        

    }
	
	public void CoordinatorHostGame() {
        status = CoordinatorStatus.CreatingGame;
        isProvider = true;
        StartCoroutine(WaitUntilAbleToHost());
            
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
        status = CoordinatorStatus.CreatingGame;
        serverUI.SetUIMode(UIMode.AskForGameInfo);
    }

    //this gets called by the Publish button in the GameInfoContainer
    public void PublishGameInfo(string gameName, string gamePassword) {
        if(status != CoordinatorStatus.CreatingGame) {
            Debug.Log("Something tried to publish a game when we weren't trying to do that.");
            return;
        }
        status = CoordinatorStatus.Hosting;
        pigeon.HostGame(gameName, gamePassword, nath.guid, nath.externalIP);
        nath.startListeningForPunchthrough(onHolePunchedServer);
    }

    


    //



    public void CoordinatorJoinGame() {
        StartCoroutine(WaitUntilAbleToJoin());
    }

    public void RefreshGames() {
        if (status != CoordinatorStatus.Joining) {
            Debug.Log("Something tried to refresh when we weren't trying to do that.");
            return;
        }
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
        status = CoordinatorStatus.Joining;
        pigeon.QueryGames(DisplayGames);
        
    }

    void DisplayGames(List<Game> games) {
        serverUI.SetUIMode(UIMode.DisplayGames);
        serverUI.PopulateGames(games);
    }

    public void RefreshListings() {
        
        pigeon.QueryGames(DisplayGames);
    }

    public void SelectGame(Game g) {
        if(status != CoordinatorStatus.Joining) {
            Debug.Log("Something tried to select a game when we weren't trying to do that.");
            return;
        }
        serverUI.SetUIMode(UIMode.Connecting);
        QueuePunch(g.ConstructPunchContainer(false));
    }

    void QueuePunch(OutboundPunchContainer pc) {
        outboundPunches.Add(pc);
    }

    public IEnumerator RobustPunch(OutboundPunchContainer pc) {
        while (nath.mode != NATStatus.Idle) {
            yield return new WaitForEndOfFrame();
        }
        Debug.Log("Punch() routine is about to punchThroughToServer()");
        nath.punchThroughToServer(8f, onHolePunchedClient, onPunchFailClient, pc);
        yield break;
    }

    

    void onHolePunchedClient(int listenPort, ushort connectPort, OutboundPunchContainer pc) {
        if (pc == null) {
            Debug.Log("Punch information missing.");
            return;
        }

        Debug.Log("Holepunch callback to server GUID " + pc.serverGUID + ", about to make connection.");
        
        ushort portToConnectTo = connectPort;
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
        sm.SpawnServerThenConnect(listenPort, addressToConnectTo, connectPort, pc.punchToProvider);
        nath.RebootNAT();
        Debug.Log("OnHolePunchedClient in NetworkCoordinator has finished.");


        
        
    }
    
    //Called by ServerManager when confirming a Peer that has been flagged as the provider.
    //Should only happen once per game session, as there is only one connection to the provider.
    public void OnProviderConfirmed(PeerInfo providerInfo) {
        if(status != CoordinatorStatus.Joining) {
            Debug.LogError("Whoa! Somebody confirmed a provider connection when we weren't connecting to a provider.");
            return;
        }
        StartCoroutine(UploadInfoToProvider());
    }
    

    void onPunchFailClient(OutboundPunchContainer pc) {
        Debug.Log("ALERT: Hole Punch Failed. Either your router or your friend's router does not support NAT punchthrough.");
        nath.RebootNAT();
    }
    
    


    //   SERVER BELOW HERE

    IEnumerator WaitForPunches() {
        Debug.Log("IEnumerator WaitForPunches()");
        while (!nath.isReady)
            yield return new WaitForEndOfFrame();
        nath.startListeningForPunchthrough(onHolePunchedServer);
        yield break;
    }

    void onHolePunchedServer(int listenPort, string exIP) {
        Debug.Log("Server receieved hole punch, spawning server on port");
        sm.SpawnServer(listenPort);
        StartCoroutine(WaitForPunches());
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



    //This takes a player object reference (presumably originating from the database)
    //and mutates it to contain the most up-to-date network information from the
    //mesh network architecture.
    public void UpdatePlayer(Player p) {
        if(nath.GetMode() == NATStatus.Uninitialized) {
            Debug.LogError("NAT not intialized, can't retrieve network information.");
            return;
        }
        p.SetExternalAddress(nath.externalIP);
        p.SetInternalAddress(Network.player.ipAddress);
        p.SetGUID(nath.guid);
    }

    public IEnumerator UploadInfoToProvider(Player me) {
        if(me == null) {
            me = new Player("TestName",
            (byte)ReservedPlayerIDs.Unspecified,
            nath.externalIP,
            Network.player.ipAddress,
            nath.guid,
            "abcd");
        }

        

        DatabaseUpdate db = new DatabaseUpdate();
        db.playerList.Add(p.GetUniqueID(), p);

        MeshPacket packet = new MeshPacket(db.GetSerializedBytes(),
            PacketType.PlayerJoin,
            (byte)ReservedPlayerIDs.Unspecified,
            (byte)ReservedPlayerIDs.Provider,
            (ushort)ReservedObjectIDs.DatabaseObject,
            (ushort)ReservedObjectIDs.DatabaseObject);

        
        while (sm.GetNumberOfPeers() == 0) {
            yield return new WaitForEndOfFrame();
        }
        sm.Broadcast(packet);
        
    }



    public void RoutePacketToServers(MeshPacket p) {
        sm.Broadcast(p);
    }
}



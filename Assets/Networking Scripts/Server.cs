using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;


class Server {

    

    public bool isSetup = false;
    public bool isListening = false;

    public string shout;

    int port;
    byte reliableChannel;
    byte unreliableChannel;
    int socketId;

    List<PeerInfo> peers = new List<PeerInfo>();

	// Use this for initialization
	public Server() {
	    
	}

    public int getSocketId() {
        return socketId;
    }
    public int getPort() {
        return port;
    }

    public void SetupServer(int listenPort) {
        Debug.Log("Starting up host at port " + listenPort);
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();

        reliableChannel = config.AddChannel(QosType.Reliable);
        unreliableChannel = config.AddChannel(QosType.Unreliable);
        
        HostTopology topology = new HostTopology(config, 10);

        socketId = NetworkTransport.AddHost(topology, listenPort);
        isSetup = true;
    }

    public bool Connect(string address, int port) {
        byte err;
        int connId;
        connId = NetworkTransport.Connect(socketId, address, port, 0, out err);
        NetworkError error = (NetworkError)err;
        Debug.Log("Server with id connected to peer " + address + ":" + port + " with error code " + error.ToString());
        if (error.Equals(NetworkError.Ok)) {
            bool success = AcceptPeer(connId);
            if (success) {
                Debug.Log("Successfully registered new connection.");
                return true;
            }else {
                Debug.Log("Server did not register new connection.");
                return false;
            }
        }else {
            Debug.Log("NetworkTransport.Connect() had an error. Did not connect to peer.");
            return false;
        }
        
    }
	
	
	public void Update () {
	}

    public bool AcceptPeer(int id) {
        bool isIdUnique = true;
        foreach(PeerInfo ci in peers) {
            if(ci.connectionId == id) {
                isIdUnique = false;
            }
        }
        if (!isIdUnique) {
            return false;
        }

        PeerInfo newConnection = new PeerInfo(id);
        peers.Add(newConnection);
        return true;
    }

    public bool BroadcastAll(byte[] packet) {
        bool successflag = true;
        byte error;
        foreach(PeerInfo peer in peers) {
            byte err;
            bool success = NetworkTransport.Send(socketId, peer.connectionId, reliableChannel, packet, packet.Length, out err);
            if (!success) {
                successflag = false;
                error = err;
            }
                
        }
        return successflag;
        
    }
    

    
}

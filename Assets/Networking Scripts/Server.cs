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

    List<ConnectionInfo> connections = new List<ConnectionInfo>();

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
        Debug.Log("Starting up server at port " + listenPort);
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();

        reliableChannel = config.AddChannel(QosType.Reliable);
        unreliableChannel = config.AddChannel(QosType.Unreliable);
        
        HostTopology topology = new HostTopology(config, 10);

        socketId = NetworkTransport.AddHost(topology, listenPort);
        isSetup = true;
    }
	
	
	public void Update () {
	}

    public bool AcceptClient(int id) {
        bool isIdUnique = true;
        foreach(ConnectionInfo ci in connections) {
            if(ci.connectionId == id) {
                isIdUnique = false;
            }
        }
        if (!isIdUnique) {
            return false;
        }

        ConnectionInfo newConnection = new ConnectionInfo(id);
        connections.Add(newConnection);
        return true;
    }

    public bool BroadcastAll(byte[] packet) {
        bool successflag = true;
        byte error;
        foreach(ConnectionInfo ci in connections) {
            byte err;
            bool success = NetworkTransport.Send(socketId, ci.connectionId, reliableChannel, packet, packet.Length, out err);
            if (!success) {
                successflag = false;
                error = err;
            }
                
        }
        return successflag;
        
    }
    

    
}

using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using Utilities;
using System;

public class Server {

    

    public bool isSetup = false;
    public bool isListening = false;

    public string shout;

    int port;
    byte reliableChannel;
    byte unreliableChannel;
    byte VOIPChannel;
    int socketID;

    List<PeerInfo> peers = new List<PeerInfo>();

	// Use this for initialization
	public Server() {
	    
	}

    public int getSocketID() {
        return socketID;
    }
    public int getPort() {
        return port;
    }

    public List<PeerInfo> getPeers() {
        return peers;
    }

    public void SetupServer(int listenPort) {
        port = listenPort;
        Debug.Log("Starting up host at port " + listenPort);
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        
        //config.PacketSize = 2000;
        reliableChannel = config.AddChannel(QosType.Reliable);
        unreliableChannel = config.AddChannel(QosType.Unreliable);
        VOIPChannel = config.AddChannel(QosType.UnreliableSequenced);
        
        HostTopology topology = new HostTopology(config, 10);

        socketID = NetworkTransport.AddHost(topology, listenPort);
        isSetup = true;
    }

    public bool Connect(string address, ushort port, bool provider) {
        byte err;
        int connId;
        connId = NetworkTransport.Connect(socketID, address, port, 0, out err);
        NetworkError error = (NetworkError)err;
        Debug.Log("Server with socketID " + socketID + "tried to connect to peer " + address + ":" + port + " with error code " + error.ToString());
        if (error.Equals(NetworkError.Ok)) {
            Debug.Log("Server successfully made connection with peer, awaiting connect confirmation.");
            PeerInfo info = new PeerInfo(connId);
            info.address = address;
            info.destPort = port;
            info.isProvider = provider;
            peers.Add(info);
            return true;
        }else {
            Debug.Log("NetworkTransport.Connect() had an error. Did not connect to peer.");
            return false;
        }
        
    }
	
	
	public void Update () {
	}

    public bool ConfirmPeer(int id, Action<PeerInfo> providerConfirmationCallback) {
        foreach(PeerInfo info in peers) {
            if(info.connectionId == id) {
                info.confirmed = true;
                if (info.isProvider) {
                    providerConfirmationCallback(info);
                }
                return true;
            }
        }
        return false;
    }

    public bool BroadcastAll(byte[] packet, QosType qos) {
        byte channel;
        switch (qos) {
            case QosType.Reliable:
            channel = reliableChannel;
            break;

            case QosType.Unreliable:
            channel = unreliableChannel;
            break;

            case QosType.UnreliableSequenced:
            channel = VOIPChannel;
            break;

            default:
            channel = unreliableChannel;
            break;
        }

        //Debug.Log("BroadcastAll");
        bool successflag = true;
        byte error;
        foreach(PeerInfo peer in peers) {
            byte err;
            bool success = NetworkTransport.Send(socketID, peer.connectionId, channel, packet, packet.Length, out err);
            if (!success) {
                successflag = false;
                error = err;
            }
                
        }
        return successflag;
        
    }

    //public void TransmitInfo()
    

    
}

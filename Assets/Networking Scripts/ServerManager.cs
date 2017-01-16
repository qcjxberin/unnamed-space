using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Utilities;

public class ServerManager {

    public string shout;
    public VoipReceiver voipReceiver;
    public NetworkDatabase ndb;
    public List<Server> servers = new List<Server>();

    //Request to create a new server in the conglomeration of servers.
    //Probably originates from a NAT punchthrough. We should check if
    //the port is already being watched, though, by combing through our
    //list of existing servers.
    public Server SpawnServer(int newPort) {
        bool isPortAlreadyWatched = false;
        foreach(Server s in servers) {
            if (s.isSetup && s.getPort() == newPort)
                isPortAlreadyWatched = true;
        }
        if (isPortAlreadyWatched) {
            return null;
        }
        Server newServer = new Server();
        newServer.SetupServer(newPort);
        servers.Add(newServer);
        return newServer;
    }

    void RobustConnect(Server server, string connectAddress, int connectPort) {
        while (true) {
            bool success = server.Connect(connectAddress, connectPort);
            if (success) {
                Debug.Log("Congratulations, peer connection achieved. Ready to begin.");
                break;
            }
            else {
                Debug.Log("Retrying");
            }
        }
    }


    
    public bool SpawnServerThenConnect(int listenPort, string connectAddress, int connectPort) {
        Server newServer = SpawnServer(listenPort);
        RobustConnect(newServer, connectAddress, connectPort);
        return true;
    }

    

    //Checks for packets from all servers.
    public void Receive() {
        if (servers.Count < 1)
            return;
        int recHostId;
        int connectionId;
        int channelId;
        byte[] recBuffer = new byte[1400];
        int bufferSize = 1400;
        int dataSize;
        byte error;
        NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);
        switch (recData) {

            case NetworkEventType.Nothing:         //1
            break;

            case NetworkEventType.ConnectEvent:    //2
            Debug.Log("Recieved ConnectEvent, registering peer");

            RegisterPeer(connectionId, recHostId);
            break;

            case NetworkEventType.DataEvent:       //3
            recData = NetworkEventType.Nothing;
            ParseData(recBuffer);
            break;

            case NetworkEventType.DisconnectEvent: //4
            Debug.Log("Recieved DisonnectEvent");
            break;
        }
    }

    void RegisterPeer(int connectID, int serverID) {
        foreach(Server s in servers) {
            if(s.getSocketID() == serverID) {
                Debug.Log("Registering new peer to serverId " + serverID);
                bool success = s.AcceptPeer(connectID);
                switch (success) {
                    case true:
                    Debug.Log("Successfully registered new peer.");
                    break;

                    case false:
                    Debug.Log("Server refused new peer registration.");
                    break;
                }
            }
        }
    }
    void ParseData(byte[] data) {
        Debug.Log("Parsing " + data.Length + " bytes");
        string preview = "";
        for(int i = 0; i < 10; i++) {
            preview += data[i];
        }
        Debug.Log("Preview: " + preview);
        if(data[0] == 1){ //Generic packet, we have to examine this
            byte playerID = data[1];
            Player source = ndb.LookupPlayer(playerID); //retrieve which player sent this packet
            if(source == null) { //hmmm, the NBD can't find the player
                Debug.Log("Player from which packet originated does not exist on local NDB.");
                return;
            }

            byte typeData = data[2]; //typeByte describes what kind of packet this is
            switch (typeData) {
                case 20: //VOIP packet
                Debug.Log("Found an audio packet");
                voipReceiver.ReceiveAudio(data);
                break;

                case 7: //network ping
                GameObject.FindObjectOfType<indicator>().Ping();
                break;

                default:
                Debug.Log("Unknown packet type, header " + typeData);
                break;
            }


            
        }
    }
    


    
    public void PingAllPeers() {
        Debug.Log("Pinging all clients.");
        PingPacket ping = new PingPacket();
        Broadcast(ping);
    }
    
    public void Broadcast(MeshPacket p) {
        
        byte playerID = ndb.GetSelf().GetUniqueID();
        //byte playerID = 0;
        byte[] outboundData = new byte[1 + p.GetData().Length + 1];
        outboundData[0] = 1; //normal 
        outboundData[1] = playerID; //it's coming from us
        p.GetData().CopyTo(outboundData, 2);
        Debug.Log("Broadcasting " + outboundData.Length + "bytes");
        foreach (Server s in servers) {
            bool success = s.BroadcastAll(outboundData, p.GetQOS());
            if (!success)
                Debug.Log("Broadcast failed.");
        }
    }

}

public class PingPacket : MeshPacket {
    
    public PingPacket() : base(new byte[1]) { //always feed packet constructor a one-byte array, this is a ping
        
    }

    public override PacketType GetTypeByte() {
        return PacketType.Ping;
    }

    public override QosType GetQOS() {
        return QosType.Reliable;
    }
}

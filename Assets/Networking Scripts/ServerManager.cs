using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class ServerManager {

    public string shout;

    List<Server> servers = new List<Server>();

    //Request to create a new server in the conglomeration of servers.
    //Probably originates from a NAT punchthrough. We should check if
    //the port is already being watched, though, by combing through our
    //list of existing servers.
    public bool SpawnServer(int newPort) {
        bool isPortAlreadyWatched = false;
        foreach(Server s in servers) {
            if (s.isSetup && s.getPort() == newPort)
                isPortAlreadyWatched = true;
        }
        if (isPortAlreadyWatched) {
            return false;
        }
        Server newServer = new Server();
        newServer.SetupServer(newPort);
        servers.Add(newServer);
        return true;
    }

    void RobustConnect(Server server, string connectAddress, int connectPort) {
        while (true) {
            bool success = server.Connect(connectAddress, connectPort);
            if (success) {
                Debug.Log("Congratulations, peer connection achieved. Ready to begin.");
                break;
            }
            else {
                //Debug.Log("Retrying");
            }
        }
    }

    public bool SpawnClient(int listenPort, string connectAddress, int connectPort) {
        bool isPortAlreadyWatched = false;
        foreach (Server s in servers) {
            if (s.isSetup && s.getPort() == listenPort)
                isPortAlreadyWatched = true;
        }
        if (isPortAlreadyWatched) {
            return false;
        }
        Server newServer = new Server();
        newServer.SetupServer(listenPort);
        servers.Add(newServer);
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
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
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
            Debug.Log("Recieved DataEvent");
            Debug.Log("Raw data: " + recBuffer.ToString());
            ParseData(recBuffer);
            break;

            case NetworkEventType.DisconnectEvent: //4
            Debug.Log("Recieved DisonnectEvent");
            break;
        }
    }

    void RegisterPeer(int connectId, int serverId) {
        foreach(Server s in servers) {
            if(s.getSocketId() == serverId) {
                Debug.Log("Registering new peer to serverId " + serverId);
                bool success = s.AcceptPeer(connectId);
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
        if (data[0] == 2) { //shout
            Debug.Log("Recieved a shout!");
            byte[] newData = new byte[0];
            data.CopyTo(newData, 1);
            ParseAsShout(newData);
        }
    }
    void ParseAsShout(byte[] data) {
        Stream stream = new MemoryStream(data);
        BinaryFormatter formatter = new BinaryFormatter();
        string message = formatter.Deserialize(stream) as string;
        shout = message;
        Debug.Log("I got shouted at! " + message);
    }


    public void ShoutAtAllPeers() {
        Debug.Log("Shouting at all clients.");
        byte[] shout = new byte[50];
        Stream stream = new MemoryStream(shout);
        BinaryFormatter formatter = new BinaryFormatter();
        formatter.Serialize(stream, "Hey you!");
        byte[] packet = new byte[51];
        packet[0] = 2;
        shout.CopyTo(packet, 1);
        foreach (Server s in servers) {
            bool success = s.BroadcastAll(packet);
            if (!success) {
                Debug.Log("BroadcastAll couldn't broadcast properly.");
            }
        }
    }
}

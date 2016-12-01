using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class ServerManager : MonoBehaviour {

    public string shout;

    List<Server> servers = new List<Server>();
    NATHelper nath;




    void Start() {
    }

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

    //Always check for new packets.
    void Update() {
        Receive();
    }

    //Checks for packets from all servers.
    void Receive() {
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
            RegisterClient(connectionId, recHostId);
            Debug.Log("Recieved ConnectEvent");
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

    void RegisterClient(int connectId, int serverId) {
        foreach(Server s in servers) {
            if(s.getSocketId() == serverId) {
                Debug.Log("Registering new client to serverId " + serverId);
                bool success = s.AcceptClient(connectId);
                switch (success) {
                    case true:
                    Debug.Log("Successfully registered new client.");
                    break;

                    case false:
                    Debug.Log("Server refused new client registration.");
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


    public void ShoutAtAllClients() {
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

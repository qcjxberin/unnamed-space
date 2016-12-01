using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class Server {

    

    public bool isSetup = false;
    public bool isListening = false;

    public string shout;

    int port;
    byte reliableChannel;
    byte unreliableChannel;
    int socketId;

    List<ConnectionInfo> connections = new List<ConnectionInfo>();

	// Use this for initialization
	void Start () {
	
	}

    public void SetupServer(int listenPort) {
        Debug.Log("Starting up server at port " + listenPort);
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();

        reliableChannel = config.AddChannel(QosType.Reliable);
        unreliableChannel = config.AddChannel(QosType.Unreliable);
        
        HostTopology topology = new HostTopology(config, 10);

        socketId = NetworkTransport.AddHost(topology, listenPort);
    }
	
	
	public void Update () {
	
	}

    void AcceptClient(int id) {
        ConnectionInfo newConnection = new ConnectionInfo(id);
        connections.Add(newConnection);
    }

    

    void Receive() {
        //Debug.Log("Receive");
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
            AcceptClient(connectionId);
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
    void ParseData(byte[] data) {
        if(data[0] == 2) { //shout
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
        Debug.Log("Shouting at all " + connections.Count + " client(s).");
        byte[] shout = new byte[50];
        Stream stream = new MemoryStream(shout);
        BinaryFormatter formatter = new BinaryFormatter();
        formatter.Serialize(stream, "Hey you!");
        byte[] packet = new byte[51];
        packet[0] = 2;
        shout.CopyTo(packet, 1);
        foreach (ConnectionInfo ci in connections) {
            byte err;
            NetworkTransport.Send(socketId, ci.connectionId, reliableChannel, packet, packet.Length, out err);
        }
    }
}

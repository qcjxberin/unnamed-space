using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.UI;
using System.Collections.Generic;

public class LowLevelNetworkManager : MonoBehaviour {
    public string ipAddress;
    public int listenPort;
    public int connectPort;
    public Text status;
    public Text hashText;
    string currentStatus = "None";
    bool isHost = false;
    string myHash = "";
    NATHelper nath;
    List<ConnectionInfo> connections = new List<ConnectionInfo>();

    public string targetGUID;
    bool isListening = false;
    int socketId = 0;
    byte myReliableChannelId = 0;
    //byte myUnreliableChannelId = 0;
    int hostConnectionId = 0;
    // Use this for initialization

    //void Start() {
    //SetupHost();
    //}

    public void Start() {
        nath = GetComponent<NATHelper>();
    }
    public void StartListening() {
        
        nath.startListeningForPunchthrough(GotPunched);
    }

    public void Punch() {
        nath.punchThroughToServer(targetGUID, SuccessfullyPunched);
    }

    public void SetIP(string ip) {
        ipAddress = ip;
    }
    public void SetTargetGUID(string id) {
        targetGUID = id;
    }
    public void GotPunched(int input, string ip) {
        Debug.Log("GotPunched: " + input);
        currentStatus = "GotPunched: " + input;
        listenPort = input;
        //ipAddress = ip;
        SetupHost();
    }
    public void SuccessfullyPunched(int listenPort, int connectPort) {
        Debug.Log("SuccessfullyPunched: " + listenPort + ", " + connectPort);
        currentStatus = "SuccessfullyPunched: " + listenPort + ", " + connectPort;
        this.listenPort = listenPort;
        this.connectPort = connectPort;
        SetupClient();

    }
    public void SetPort(string inport) {
        listenPort = int.Parse(inport);
    }
    public void SetupHost() {
        //port = nath
        Debug.Log("Setting up host at listenPort " + listenPort);
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        myReliableChannelId = config.AddChannel(QosType.Reliable);
        //myUnreliableChannelId = config.AddChannel(QosType.Unreliable);
        HostTopology topology = new HostTopology(config, 10);
        socketId = NetworkTransport.AddHost(topology, listenPort);
        currentStatus = "Socket Open. SocketId is: " + socketId;
        Debug.Log("Reliable: " + myReliableChannelId);
        Debug.Log(config.Channels[0].QOS);
        //Debug.Log("Uneliable: " + myUnreliableChannelId);
        isListening = true;
        myHash = Md5Sum(System.DateTime.Now.Second.ToString());
        isHost = true;
    }

    public void SetupClient() {
        Debug.Log("SetupClient()");
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        myReliableChannelId = config.AddChannel(QosType.Reliable);
        //myUnreliableChannelId = config.AddChannel(QosType.Unreliable);
        HostTopology topology = new HostTopology(config, 10);
        socketId = NetworkTransport.AddHost(topology, listenPort);
        currentStatus = "Socket Open at port " + listenPort + ". SocketId is: " + socketId;
        Debug.Log("Socket Open at port " + listenPort + ". SocketId is: " + socketId);
        Debug.Log("Reliable: " + myReliableChannelId);
        Debug.Log(config.Channels[0].QOS);
        //Debug.Log("Uneliable: " + myUnreliableChannelId);
        isListening = true;
        TryConnection();
    }

    void TryConnection() {
        Debug.Log("TryConnection(): target ip " + ipAddress + ", target port " + connectPort + ", using socketId " + socketId);
        byte error;
        
        hostConnectionId = NetworkTransport.Connect(socketId, ipAddress, connectPort, 0, out error);
        Debug.Log("Connect() error byte: " + ((NetworkError)error).ToString());
        Debug.Log("Connected to server. ConnectionId: " + hostConnectionId);
        //NetworkTransport.Disconnect(hostId, connectionId, out error);
        //NetworkTransport.Send(socketId, connectionId, myReliableChannelId, buffer, bufferLength, out error);
    }

    

    public void BroadcastToPeers() {
        if (!isHost)
            return;
        foreach(ConnectionInfo ci in connections) {
            Debug.Log("sending message...");
            byte error;
            byte[] buffer = new byte[1024];
            Stream stream = new MemoryStream(buffer);
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, "Hello");

            int bufferSize = 1024;
            bool success = NetworkTransport.Send(socketId, ci.connectionId, myReliableChannelId, buffer, bufferSize, out error);
        }
    }
    public void BroadcastToHost() {
        if (isHost)
            return;
        
        Debug.Log("sending message...");
        byte error;
        byte[] buffer = new byte[1024];
        Stream stream = new MemoryStream(buffer);
        BinaryFormatter formatter = new BinaryFormatter();
        formatter.Serialize(stream, "Hello");

        int bufferSize = 1024;
        bool success = NetworkTransport.Send(socketId, hostConnectionId, myReliableChannelId, buffer, bufferSize, out error);
        
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.S)) {
            SetupHost();
        }
        if (Input.GetKeyDown(KeyCode.C)) {
            TryConnection();
        }
        if (Input.GetKeyDown(KeyCode.R)) {
            Receive();
        }
        if(isListening)
            Receive();
        status.text = "Status: " + currentStatus;
        hashText.text = "Hash: " + myHash;
        
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
        Stream stream;
        BinaryFormatter formatter;
        NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);
        switch (recData) {
            case NetworkEventType.Nothing:         //1
            break;

            case NetworkEventType.ConnectEvent:    //2
            Debug.Log("Recieved ConnectEvent");
            string newhash = Md5Sum(System.DateTime.Now.Second.ToString());
            connections.Add(new ConnectionInfo(connectionId, newhash));
            byte err;
            byte[] buffer = new byte[1024];
            stream = new MemoryStream(buffer);
            formatter = new BinaryFormatter();
            formatter.Serialize(stream, "hash" + newhash);

            int size = 1024;
            NetworkTransport.Send(socketId, connectionId, myReliableChannelId, buffer, size, out err);
            break;

            case NetworkEventType.DataEvent:       //3
            Debug.Log("Recieved DataEvent");
            stream = new MemoryStream(recBuffer);
            formatter = new BinaryFormatter();
            string message = formatter.Deserialize(stream) as string;
            Debug.Log("incoming message event received: " + message);
            ParseData(recBuffer);
            break;

            case NetworkEventType.DisconnectEvent: //4
            Debug.Log("Recieved DisonnectEvent");
            break;
        }
    }

    void ParseData(byte[] data) {
        Stream stream = new MemoryStream(data);
        BinaryFormatter formatter = new BinaryFormatter();
        string message = formatter.Deserialize(stream) as string;
        if(message.IndexOf("hash") == 0) {
            myHash = message.Substring(4);
        }
    }

    public string Md5Sum(string strToEncrypt) {
        System.Text.UTF8Encoding ue = new System.Text.UTF8Encoding();
        byte[] bytes = ue.GetBytes(strToEncrypt);

        // encrypt bytes
        System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
        byte[] hashBytes = md5.ComputeHash(bytes);

        // Convert the encrypted bytes back to a string (base 16)
        string hashString = "";

        for (int i = 0; i < hashBytes.Length; i++) {
            hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
        }

        return hashString.PadLeft(32, '0');
    }

    class ConnectionInfo {
        public int connectionId;
        public string hashGiven;

        public ConnectionInfo(int id, string hash) {
            connectionId = id;
            hashGiven = hash;
        }


    }
}



using UnityEngine;
using System.Collections;
using System.Text;
using UnityEngine.Networking;
using Utilities;
public class PeerInfo {

    public int connectionId;
    
    public PeerInfo(int id) {
        connectionId = id;
    }
}

public class OutboundPunchContainer {
    public string serverExternalIP;
    public string serverInternalIP;
    public string serverGUID;
    public int punchID;

    public OutboundPunchContainer(string serverExIP, string serverInIP, string serverID, int id) {
        serverExternalIP = serverExIP;
        serverInternalIP = serverInIP;
        serverGUID = serverID;
        punchID = id;
    }
}

public class Player {
    string displayName;
    byte uniqueID;
    string address;
    string privateKey;

    public Player() {
        displayName = "DefaultPlayerName";
        uniqueID = 0;
        address = "0.0.0.0";
        privateKey = "DefaultPrivateKey";
    }

    public Player(string name, byte id, string address, string privateKey) {
        SetName(name);
        SetUniqueID(id);
        SetAddress(address);
        SetPrivateKey(privateKey);

    }

    public void SetName(string n) {
        displayName = n.Replace(":", "$COLON");
    }
    public string GetNameSanitized() {
        return displayName;
    }
    public string GetNameDesanitized() {
        return displayName.Replace("$COLON", ":");
    }

    public void SetUniqueID(byte id) {
        uniqueID = id;
    }
    public byte GetUniqueID() {
        return uniqueID;
    }

    public void SetAddress(string a) {
        address = a;
    }
    public string GetAddress() {
        return address;
    }

    public void SetPrivateKey(string k) {
        privateKey = k;
    }
    public string GetPrivateKey() {
        return privateKey;
    }

    public byte[] SerializeFull() {
        
        byte[] result = Encoding.ASCII.GetBytes(GetNameSanitized() + ":" + uniqueID + ":" + address + ":" + privateKey);
        return result;
    }
    
}

public class MeshPacket {
    
    byte[] data;
    

    public MeshPacket() { //if no data supplied, generate empty packet with generic typebyte 
        SetData(new byte[0]);
    }
    public MeshPacket(byte[] data) { //if data supplied, generate packet with generic typebyte
        SetData(data);
    }

    public virtual PacketType GetTypeByte() {
        return 0;
    }

    public byte[] GetData() {
        return data;
    }
    public void SetData(byte[] data) {
        this.data = new byte[data.Length + 1];
        this.data[0] = (byte)GetTypeByte();
        data.CopyTo(this.data, 1);
    }

    public virtual QosType GetQOS() {
        return QosType.Unreliable;
    }

    
}

public class AudioPacket : MeshPacket {

    public AudioPacket(byte[] audio) : base(audio) { //pass the provided audio to the packet constructor. it will use the proper typebyte and qos
    }

    public override PacketType GetTypeByte() {
        return PacketType.VOIP;
    }

    public override QosType GetQOS() {
        return QosType.Unreliable;
    }

}


public class Game{
    public string name;
    public string password;
    public string providerGUID;
    public string providerInternalIP;
    public string providerExternalIP;

    public OutboundPunchContainer ConstructPunchContainer() {
        return new OutboundPunchContainer(providerExternalIP, providerInternalIP, providerGUID, -1);
    }



    public Game() {
        name = "DefaultGameName";
        password = "";
        providerGUID = "";
        providerInternalIP = "0.0.0.0";
        providerExternalIP = "0.0.0.0";
    }

    public Game(string nameIn, string passwordIn, string guidIN, string inIPIn, string exIPIn) {
        name = nameIn;
        password = passwordIn;
        providerGUID = guidIN;
        providerInternalIP = inIPIn;
        providerExternalIP = exIPIn;
    }

    
}





namespace Utilities {
    public enum NATStatus {
        Uninitialized,
        Idle,
        ConnectingToFacilitator,
        Listening,
        Punching,
        Rebooting,
        AfterPunching
    }

    public enum CoordinatorStatus {
        Uninitialized,
        Idle,
        CreatingGame, //setting up game
        Hosting, //Providing a game.
        Joining  //Joining a friend's game.
    }

    public enum UIMode {
        Welcome,
        AskForGameInfo,
        DisplayGames,
        Connecting
    }

    public enum PacketType : byte {
        Ping = 7,
        Generic = 0,
        VOIP = 20
    }

    
}


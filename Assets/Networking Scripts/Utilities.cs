using UnityEngine;
using System.Collections;
using System.Text;
using UnityEngine.Networking;
using Utilities;
using System.Collections.Generic;
using System;
using Steamworks;



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

    public enum ReservedObjectIDs : ushort {
        Unspecified = 0,
        DatabaseObject = 1
    }
    public enum ReservedPlayerIDs : byte {
        Unspecified = 0,
        Broadcast = 1,
        Self = 2,
        Provider = 3
    }

    public enum CoordinatorStatus {
        Uninitialized,
        Idle,
        CreatingGame, //setting up game
        Hosting, //Providing a game.
        Joining,  //Joining a friend's game.
        Playing,
        PlayingAsProvider
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
        VOIP = 20,
        DatabaseUpdate = 10,
        PlayerJoin = 11
    }

    public enum ConnectionStatus {
        Pending, Connected, Disconnected
    }

    public class PeerInfo {

        public int connectionId;
        public string address;
        public ushort destPort;
        public bool confirmed;
        public bool isProvider;

        public PeerInfo(int id) {
            connectionId = id;
        }
    }

    public class OutboundPunchContainer {
        public string serverExternalIP;
        public string serverInternalIP;
        public string serverGUID;
        public int punchID;
        public bool punchToProvider;

        public OutboundPunchContainer(string serverExIP, string serverInIP, string serverID, int id, bool isTargetProvider) {
            serverExternalIP = serverExIP;
            serverInternalIP = serverInIP;
            serverGUID = serverID;
            punchID = id;
            punchToProvider = isTargetProvider;
        }

        public OutboundPunchContainer(string serverExIP, string serverInIP, string serverID, int id) {
            serverExternalIP = serverExIP;
            serverInternalIP = serverInIP;
            serverGUID = serverID;
            punchID = id;
            punchToProvider = false;
        }
    }
    [Serializable]
    public class Player {
        private string displayName;
        private byte uniqueID;
        private CSteamID id;
        private string externalIP;
        private string internalIP;
        private string GUID;
        private string privateKey;

        public Player() {
            displayName = "DefaultPlayerName";
            uniqueID = 0;
            externalIP = "0.0.0.0";
            internalIP = "0.0.0.0";
            GUID = "0";
            privateKey = "DefaultPrivateKey";
            
        }

        public Player(string name, 
            byte id, 
            string exIP, 
            string inIP,
            string guid,
            string privateKey) {
            SetName(name);
            SetUniqueID(id);
            SetExternalAddress(exIP);
            SetInternalAddress(inIP);
            SetGUID(guid);
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

        public void SetExternalAddress(string a) {
            externalIP = a;
        }
        public string GetExternalAddress() {
            return externalIP;
        }
        public void SetInternalAddress(string a) {
            internalIP = a;
        }
        public string GetInternalAddress() {
            return internalIP;
        }
        public void SetGUID(string a) {
            GUID = a;
        }
        public string GetAddress() {
            return GUID;
        }

        public void SetPrivateKey(string k) {
            privateKey = k;
        }
        public string GetPrivateKey() {
            return privateKey;
        }

        public byte[] SerializeFull() {
            byte[] result = Encoding.ASCII.GetBytes(GetNameSanitized() + ":"
                + uniqueID + ":"
                + externalIP + ":"
                + internalIP + ":"
                + GUID + ":"
                + privateKey);
            return result;
        }

        public OutboundPunchContainer ConstructPunchContainer(bool isTargetProvider) {
            return new OutboundPunchContainer(externalIP, internalIP, GUID, -1, isTargetProvider);
        }

        public static Player DeserializeFull(byte[] bytes) {
            string s = Encoding.ASCII.GetString(bytes);
            string[] parts = s.Split(':');

            Player p = new Player(parts[0], byte.Parse(parts[1]), parts[2], parts[3], parts[4], parts[5]);
            return p;
        }

        

    }

    public class MeshPacket {

        private byte[] data;
        private PacketType type;
        private byte srcPlayerId;
        private byte targetPlayerId;
        private ushort srcObjectId;
        private ushort targetObjectId;
        public QosType qos;


        public MeshPacket() { //if no data supplied, generate empty packet with generic typebyte
            data = new byte[1];
            type = PacketType.Generic;
            srcPlayerId = 0;
            targetPlayerId = 0;
            srcObjectId = 0;
            targetObjectId = 0;
        }
        public MeshPacket(byte[] serializedData) { //if data supplied, generate packet with generic typebyte

            type = (PacketType)serializedData[0];
            srcPlayerId = serializedData[1];
            targetPlayerId = serializedData[2];
            srcObjectId = BitConverter.ToUInt16(serializedData, 3);
            targetObjectId = BitConverter.ToUInt16(serializedData, 5);
            data = new byte[serializedData.Length - 7];
            Buffer.BlockCopy(serializedData, 7, data, 0, data.Length);

        }
        public MeshPacket(byte[] contents, PacketType type, byte srcPlayer, byte targetPlayer, ushort srcObject, ushort targetObject) {
            data = contents;
            this.type = type;
            srcPlayerId = srcPlayer;
            targetPlayerId = targetPlayer;
            srcObjectId = srcObject;
            targetObjectId = targetObject;
        }
        public byte[] GetData() {
            return data;
        }

        public void SetPacketType(PacketType p) {
            type = p;
        }
        public PacketType GetPacketType() {
            return type;
        }
        public void SetData(byte[] data) {
            this.data = data;
        }
        public byte GetSourcePlayerId() {
            return srcPlayerId;
        }
        public byte GetTargetPlayerId() {
            return targetPlayerId;
        }
        public void SetSourcePlayerId(byte i) {
            srcPlayerId = i;
        }
        public void SetTargetPlayerId(byte i) {
            targetPlayerId = i;
        }

        public ushort GetSourceObjectId() {
            return srcObjectId;
        }
        public ushort GetTargetObjectId() {
            return targetObjectId;
        }
        public void SetSourceObjectId(ushort i) {
            srcObjectId = i;
        }
        public void SetTargetObjectId(ushort i) {
            targetObjectId = i;
        }

        public byte[] GetSerializedBytes() {
            List<byte> output = new List<byte>();

            output.Add((byte)type);
            output.Add(srcPlayerId);
            output.Add(targetPlayerId);
            output.AddRange(BitConverter.GetBytes(srcObjectId));
            output.AddRange(BitConverter.GetBytes(targetObjectId));
            output.AddRange(data);

            return output.ToArray();
        }

        public static DatabaseUpdate ParseContentAsDatabaseUpdate(byte[] serializedData) {

            Dictionary<byte, Player> playerList = new Dictionary<byte, Player>();
            Dictionary<ushort, MeshNetworkIdentity> networkObjects = new Dictionary<ushort, MeshNetworkIdentity>();
            
            


            byte[] rawData = serializedData;
            byte numOfNewPlayers = rawData[0];
            int pointer = 1;
            byte i = 0;
            while (i < numOfNewPlayers) {
                int blobLength = rawData[pointer];

                pointer++; //pointer is now at the beginning of the player data blob

                byte[] playerData = new byte[blobLength];
                Buffer.BlockCopy(rawData, pointer, playerData, 0, blobLength);
                Player p = Player.DeserializeFull(playerData);
                playerList.Add(p.GetUniqueID(), p);

                pointer += blobLength; //pointer now at the byte after the player data blob
                i++;
            }
            byte numOfObjects = rawData[pointer];
            Debug.Log("DatabaseDeserialize numOfObjects = " + numOfObjects);
            pointer++; //pointer now at the beginning of the first MeshNetworkIdentity data
            byte j = 0;
            while (j < numOfObjects) {
                MeshNetworkIdentity netid = new MeshNetworkIdentity();
                byte[] trimmed = new byte[MeshNetworkIdentity.NETWORK_IDENTITY_BYTE_SIZE];
                Buffer.BlockCopy(rawData, pointer, trimmed, 0, trimmed.Length);
                netid.DeserializeAndApply(trimmed);
                networkObjects.Add(netid.GetObjectID(), netid);
                pointer += MeshNetworkIdentity.NETWORK_IDENTITY_BYTE_SIZE; //pointer now at the byte after
                j++;
            }
            
            return new DatabaseUpdate(playerList, networkObjects);
        }

    }

    
    public class DatabaseUpdate {
        
        //These dictionaries are treated as deltas (why send the entire database?)
        public Dictionary<byte, Player> playerList = new Dictionary<byte, Player>();
        public Dictionary<ushort, MeshNetworkIdentity> networkObjects = new Dictionary<ushort, MeshNetworkIdentity>();

        public DatabaseUpdate() {
            playerList = new Dictionary<byte, Player>();
            networkObjects = new Dictionary<ushort, MeshNetworkIdentity>();
            
        }

        public DatabaseUpdate(Dictionary<byte, Player> players,
            Dictionary<ushort, MeshNetworkIdentity> objects) {

            playerList = players;
            networkObjects = objects;
        }
        

        public void DeserializeAndApply(byte[] serializedData) {
            DatabaseUpdate decoded = MeshPacket.ParseContentAsDatabaseUpdate(serializedData);
            this.playerList = decoded.playerList;
            this.networkObjects = decoded.networkObjects;
            
        }

        public byte[] GetSerializedBytes() {
            List<byte> output = new List<byte>();
            

            byte numPlayers = (byte)playerList.Keys.Count;
            output.Add(numPlayers);
            foreach (byte playerID in playerList.Keys) {
                byte[] serializedPlayer = playerList[playerID].SerializeFull();
                output.Add((byte)serializedPlayer.Length);
                output.AddRange(serializedPlayer);
            }
            byte numObjects = (byte)networkObjects.Keys.Count;
            output.Add(numObjects);
            foreach(ushort objectID in networkObjects.Keys) {
                byte[] serializedObject = networkObjects[objectID].GetSerializedBytes();
                output.AddRange(serializedObject);
            }
            
            return output.ToArray();
        }

        
    }


    public class Game {
        public string name;
        public string password;
        public string providerGUID;
        public string providerInternalIP;
        public string providerExternalIP;

        public OutboundPunchContainer ConstructPunchContainer(bool isTargetProvider) {
            return new OutboundPunchContainer(providerExternalIP, providerInternalIP, providerGUID, -1, isTargetProvider);
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

    public interface IReceivesPacket<MeshPacket> {
        void ReceivePacket(MeshPacket p);
    }
    public interface IMeshSerializable {
        byte[] GetSerializedBytes();
    }

    public class Testing {
        public static void DebugDatabaseSerialization() {
            Debug.Log("Creating player named Mary Jane.");
            Player p1 = new Player("Mary Jaaannee", 23, "1.2.3.4", "1.2.3.4", "thisismyguid", "abcde");
            Debug.Log("Creating player named John Smith");
            Player p2 = new Player("John Smith", 52, "1.2.3.4", "1.2.3.4", "thisismyguid", "12345");

            DatabaseUpdate db = new DatabaseUpdate();
            db.playerList.Add(p1.GetUniqueID(), p1);
            db.playerList.Add(p2.GetUniqueID(), p2);

            

            //db.networkObjects.Add(obj1.GetObjectID(), obj1);
            //db.networkObjects.Add(obj2.GetObjectID(), obj2);

            Debug.Log("Total payload length: " + db.GetSerializedBytes().Length);

            MeshPacket p = new MeshPacket();
            p.SetPacketType(PacketType.DatabaseUpdate);
            p.SetSourceObjectId((byte)ReservedObjectIDs.DatabaseObject);
            p.SetSourcePlayerId(120);
            p.SetTargetObjectId((byte)ReservedObjectIDs.DatabaseObject);
            p.SetTargetPlayerId((byte)ReservedPlayerIDs.Broadcast);
            p.SetData(db.GetSerializedBytes());
            
            byte[] transmitData = p.GetSerializedBytes();

            //THIS WOULD GET SENT ACROSS THE NETWORK

            MeshPacket received = new MeshPacket(transmitData);
            Debug.Log("Received packet:");
            Debug.Log("packetType: " + received.GetPacketType());
            Debug.Log("sourceObjectID: " + received.GetSourceObjectId());
            Debug.Log("sourcePlayerID: " + received.GetSourcePlayerId());
            Debug.Log("targetObjectID: " + received.GetTargetObjectId());
            Debug.Log("targetPlayerID: " + received.GetTargetPlayerId());
            Debug.Log("Payload length: " + received.GetData().Length);

            DatabaseUpdate receivedDB = MeshPacket.ParseContentAsDatabaseUpdate(received.GetData());
            Debug.Log("Received DatabaseUpdate:");
            Debug.Log("Total number of objects: " + receivedDB.networkObjects.Count);
            int i = 1;
            foreach(ushort id in receivedDB.networkObjects.Keys) {
                Debug.Log("Object " + i + ": ");
                Debug.Log("objectID: " + receivedDB.networkObjects[id].GetObjectID());
                Debug.Log("prefabID: " + receivedDB.networkObjects[id].GetPrefabID());
                Debug.Log("ownerID : " + receivedDB.networkObjects[id].GetOwnerID());
                i++;
            }
            Debug.Log("Total number of players: " + receivedDB.playerList.Count);
            i = 1;
            foreach (byte id in receivedDB.playerList.Keys) {
                Debug.Log("Player " + i + ": ");
                Debug.Log("Desanitized Name: " + receivedDB.playerList[id].GetNameDesanitized());
                Debug.Log("Sanitized Name: " + receivedDB.playerList[id].GetNameSanitized());
                Debug.Log("uniqueID: " + receivedDB.playerList[id].GetUniqueID());
                Debug.Log("address: " + receivedDB.playerList[id].GetAddress());
                Debug.Log("privateKey: " + receivedDB.playerList[id].GetPrivateKey());
                i++;
            }
        }
    }


}


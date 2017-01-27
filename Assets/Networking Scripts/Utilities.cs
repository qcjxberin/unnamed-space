using UnityEngine;
using System.Collections;
using System.Text;
using UnityEngine.Networking;
using Utilities;
using System.Collections.Generic;
using System;
using Steamworks;



namespace Utilities {

    public enum ReservedObjectIDs : ushort {
        Unspecified = 0,
        DatabaseObject = 1
    }
    public enum ReservedPlayerIDs : byte {
        Unspecified = 0,
        Broadcast = 1,
        Self = 2,
        Provider = 3,
        FirstAvailable = 4
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
        AskForPassword,
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

    public struct GameInfo {
        public string name;
        public string password;
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

    
    public class Player {
        private string displayName;
        private byte uniqueID;
        private CSteamID steamID;
        private string privateKey;

        public Player() {
            displayName = "DefaultPlayerName";
            uniqueID = 0;
            privateKey = "DefaultPrivateKey";
            steamID = CSteamID.Nil;
        }

        public Player(string name, 
            byte id,
            string privateKey,
            CSteamID steamID) {

            SetName(name);
            SetUniqueID(id);
            SetSteamID(steamID);
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
        
        public void SetSteamID(CSteamID id) {
            steamID = id;
        }
        public CSteamID GetSteamID() {
            return steamID;
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
                + privateKey + ":"
                + steamID.m_SteamID);
            return result;
        }

        public static Player DeserializeFull(byte[] bytes) {
            string s = Encoding.ASCII.GetString(bytes);
            string[] parts = s.Split(':');

            Player p = new Player(parts[0], byte.Parse(parts[1]), parts[2], new CSteamID(ulong.Parse(parts[3])));
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
        public EP2PSend qos;


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
            DatabaseUpdate decoded = DatabaseUpdate.ParseContentAsDatabaseUpdate(serializedData);
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

    

    public interface IReceivesPacket<MeshPacket> {
        void ReceivePacket(MeshPacket p);
    }
    public interface IMeshSerializable {
        byte[] GetSerializedBytes();
    }
    public interface INetworked<MeshNetworkIdentity> {
        MeshNetworkIdentity GetIdentity();
    }

    public class Testing {
        public static void DebugDatabaseSerialization() {
            Debug.Log("Creating player named Mary Jane.");
            Player p1 = new Player("Mary Jaaannee", 23, "abcde", CSteamID.Nil);
            Debug.Log("Creating player named John Smith");
            Player p2 = new Player("John Smith", 52, "12345", CSteamID.Nil);

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

            DatabaseUpdate receivedDB = DatabaseUpdate.ParseContentAsDatabaseUpdate(received.GetData());
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
                Debug.Log("steamID: " + receivedDB.playerList[id].GetSteamID());
                Debug.Log("privateKey: " + receivedDB.playerList[id].GetPrivateKey());
                i++;
            }
        }
    }


}


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
    public enum ReservedPlayerIDs : ulong {
        Unspecified = 0,
        Broadcast = 1,
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

    

    
    public class Player {
        private string displayName;
        private ulong uniqueID;
        private string privateKey;

        public Player() {
            displayName = "DefaultPlayerName";
            uniqueID = 0;
            privateKey = "DefaultPrivateKey";
            
        }

        public Player(string name, 
            ulong id,
            string privateKey) {

            SetName(name);
            SetUniqueID(id);
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

        public void SetUniqueID(ulong id) {
            uniqueID = id;
        }
        public ulong GetUniqueID() {
            return uniqueID;
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
                + privateKey);
            return result;
        }

        public static Player DeserializeFull(byte[] bytes) {
            string s = Encoding.ASCII.GetString(bytes);
            string[] parts = s.Split(':');

            Player p = new Player(parts[0], ulong.Parse(parts[1]), parts[2]);
            return p;
        }

        

    }

    public class MeshPacket {

        private byte[] data;
        private PacketType type;
        private ulong srcPlayerId;
        private ulong targetPlayerId;
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
            int bytesRead = 0;
            type = (PacketType)serializedData[0];
            bytesRead++;
            srcPlayerId = BitConverter.ToUInt64(serializedData, 1);
            bytesRead += 8;
            targetPlayerId = BitConverter.ToUInt64(serializedData, 9);
            bytesRead += 8;
            srcObjectId = BitConverter.ToUInt16(serializedData, 17);
            bytesRead += 2;
            targetObjectId = BitConverter.ToUInt16(serializedData, 19);
            bytesRead += 2;
            data = new byte[serializedData.Length - bytesRead];
            Buffer.BlockCopy(serializedData, bytesRead, data, 0, data.Length);

        }
        public MeshPacket(byte[] contents, PacketType type, ulong srcPlayer, ulong targetPlayer, ushort srcObject, ushort targetObject) {
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
        public ulong GetSourcePlayerId() {
            return srcPlayerId;
        }
        public ulong GetTargetPlayerId() {
            return targetPlayerId;
        }
        public void SetSourcePlayerId(ulong i) {
            srcPlayerId = i;
        }
        public void SetTargetPlayerId(ulong i) {
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
            output.AddRange(BitConverter.GetBytes(srcPlayerId));
            output.AddRange(BitConverter.GetBytes(targetPlayerId));
            output.AddRange(BitConverter.GetBytes(srcObjectId));
            output.AddRange(BitConverter.GetBytes(targetObjectId));
            output.AddRange(data);

            return output.ToArray();
        }

        

    }

    
    public class DatabaseUpdate {

        //These dictionaries are treated as deltas (why send the entire database?)
        public Dictionary<ulong, Player> playerList = new Dictionary<ulong, Player>();
        public Dictionary<ushort, MeshNetworkIdentity> objectList = new Dictionary<ushort, MeshNetworkIdentity>();

        public DatabaseUpdate() {
            playerList = new Dictionary<ulong, Player>();
            objectList = new Dictionary<ushort, MeshNetworkIdentity>();
            
        }

        public DatabaseUpdate(Dictionary<ulong, Player> players,
            Dictionary<ushort, MeshNetworkIdentity> objects) {

            playerList = players;
            objectList = objects;
        }
        

        public void DeserializeAndApply(byte[] serializedData) {
            DatabaseUpdate decoded = DatabaseUpdate.ParseContentAsDatabaseUpdate(serializedData);
            this.playerList = decoded.playerList;
            this.objectList = decoded.objectList;
            
        }

        public byte[] GetSerializedBytes() {
            List<byte> output = new List<byte>();
            

            byte numPlayers = (byte)playerList.Keys.Count;
            output.Add(numPlayers);
            foreach (ulong playerID in playerList.Keys) {
                byte[] serializedPlayer = playerList[playerID].SerializeFull();
                output.Add((byte)serializedPlayer.Length);
                output.AddRange(serializedPlayer);
            }
            byte numObjects = (byte)objectList.Keys.Count;
            output.Add(numObjects);
            foreach(ushort objectID in objectList.Keys) {
                byte[] serializedObject = objectList[objectID].GetSerializedBytes();
                output.AddRange(serializedObject);
            }
            
            return output.ToArray();
        }

        public static DatabaseUpdate ParseContentAsDatabaseUpdate(byte[] serializedData) {

            Dictionary<ulong, Player> playerList = new Dictionary<ulong, Player>();
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
            Player p1 = new Player("Mary Jananee", 2233443, "abcde");
            Debug.Log("Creating player named John Smith");
            Player p2 = new Player("John Smith", 52342342, "12345");

            DatabaseUpdate db = new DatabaseUpdate();
            db.playerList.Add(p1.GetUniqueID(), p1);
            db.playerList.Add(p2.GetUniqueID(), p2);

            

            //db.objectList.Add(obj1.GetObjectID(), obj1);
            //db.objectList.Add(obj2.GetObjectID(), obj2);

            Debug.Log("Total payload length: " + db.GetSerializedBytes().Length);
            Debug.Log("Database hash: " + NetworkDatabase.GenerateDatabaseChecksum(db.playerList, db.objectList));
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
            Debug.Log("Database hash: " + NetworkDatabase.GenerateDatabaseChecksum(db.playerList, db.objectList));
            Debug.Log("Total number of objects: " + receivedDB.objectList.Count);
            int i = 1;
            foreach(ushort id in receivedDB.objectList.Keys) {
                Debug.Log("Object " + i + ": ");
                Debug.Log("objectID: " + receivedDB.objectList[id].GetObjectID());
                Debug.Log("prefabID: " + receivedDB.objectList[id].GetPrefabID());
                Debug.Log("ownerID : " + receivedDB.objectList[id].GetOwnerID());
                i++;
            }
            Debug.Log("Total number of players: " + receivedDB.playerList.Count);
            i = 1;
            foreach (ulong id in receivedDB.playerList.Keys) {
                Debug.Log("Player " + i + ": ");
                Debug.Log("Desanitized Name: " + receivedDB.playerList[id].GetNameDesanitized());
                Debug.Log("Sanitized Name: " + receivedDB.playerList[id].GetNameSanitized());
                Debug.Log("uniqueID: " + receivedDB.playerList[id].GetUniqueID());
                Debug.Log("privateKey: " + receivedDB.playerList[id].GetPrivateKey());
                i++;
            }
        }
    }


}


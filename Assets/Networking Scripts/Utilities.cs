using UnityEngine;
using System.Collections;
using System.Text;
using UnityEngine.Networking;
using Utilities;
using System.Collections.Generic;
using System;
using Steamworks;

    /*
        Utilities.cs
        Copyright 2017 Finn Sinclair

        Assorted helper classes, wrapper classes, data enumerations,
        and other useful bits and pieces.

        Data serialization and deserialization routines are included
        in MeshPacket and DatabaseUpdate. More comprehensive summaries
        of those two classes can be found next to their location in the
        source.

        The large amount of code at the bottom simply simulates a full
        database update, to verify that the serialization systems are working.
    */

namespace Utilities {

    public enum ReservedObjectIDs : ushort {
        Unspecified = 0,
        DatabaseObject = 1
    }
    public enum ReservedPlayerIDs : ulong {
        Unspecified = 0,
        Broadcast = 1,
    }
    public enum ReservedPrefabIDs : ushort {
        Unspecified = 0,
        Database = 1
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
        FullUpdateRequest = 9,
        DatabaseUpdate = 10,
        PlayerJoin = 11,
        DatabaseChangeRequest = 12
        
    }

    public enum ConnectionStatus {
        Pending, Connected, Disconnected
    }

    public enum StateChange:byte {
        Addition = 0,
        Removal = 1,
        Change = 2,
        Override = 3
    }

    public struct GamePublishingInfo {
        public string name;
        public string password;
    }

    public struct GameMatchmakingInfo {
        public string name;
        public ulong id;
        public Action<CSteamID> callback;
    }



    /*
        Utilities.Player

        Data structure for player information in the networked object model.
        Has self-contained serialization and deserialization methods, which
        use byte arrays for transmission across the network.
    */
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

    /*
        Utilities.MeshPacket

        Data structure which contains raw byte data along with metadata
        concerning the source, destination, and type of contents. This is
        the main currency of the distributed network object model. Each
        packet is designed to be self-sufficient, meaning that each packet
        is unique, identifiable, self-representative, and serializable (both ways).
    */

    public class MeshPacket {

        private byte[] contents;
        private PacketType type;
        private ulong srcPlayerId;
        private ulong targetPlayerId;
        private ushort srcObjectId;
        private ushort targetObjectId;
        public EP2PSend qos;


        public MeshPacket() { //if no data supplied, generate empty packet with generic typebyte
            contents = new byte[1];
            type = PacketType.Generic;
            srcPlayerId = 0;
            targetPlayerId = 0;
            srcObjectId = 0;
            targetObjectId = 0;
        }
        public MeshPacket(byte[] serializedData) { //Deserialize packet.
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
            contents = new byte[serializedData.Length - bytesRead];
            Buffer.BlockCopy(serializedData, bytesRead, contents, 0, contents.Length);

        }
        public MeshPacket(byte[] contents, PacketType type, ulong srcPlayer, ulong targetPlayer, ushort srcObject, ushort targetObject) {
            this.contents = contents;
            this.type = type;
            srcPlayerId = srcPlayer;
            targetPlayerId = targetPlayer;
            srcObjectId = srcObject;
            targetObjectId = targetObject;
        }
        public byte[] GetContents() {
            return contents;
        }

        public void SetPacketType(PacketType p) {
            type = p;
        }
        public PacketType GetPacketType() {
            return type;
        }
        public void SetContents(byte[] contents) {
            this.contents = contents;
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
            output.AddRange(contents);

            return output.ToArray();
        }

        

    }


    /*
        Utilities.DatabaseUpdate

        Delta updates that drive the distributed network databases. Only the data
        that is modified is sent. This usually happens one change at a time. However,
        in some cases, multiple changes can be sent in the same DatabaseUpdate. (This
        usually occurs when a mass quantity of game state information needs to be sent.)

        The usual sequence of events is as follows:

        - Master database executes update
        - Master database creates checksum of entire database
        - Master database compiles DatabaseUpdate containing the necessary
            (usually just one) change
        - Master database broadcasts DatabaseUpdate to all peers
        - Each peer applies the update, generates a checksum of their own local database
        - If checksums don't match, the peer requests a full (non-delta) update from the master
    */


    public class DatabaseUpdate {

        //These dictionaries are treated as deltas (why send the entire database?)
        public Dictionary<Player, StateChange> playerDelta = new Dictionary<Player, StateChange>();
        public Dictionary<MeshNetworkIdentity, StateChange> objectDelta = new Dictionary<MeshNetworkIdentity, StateChange>();
        public ushort fullHash;

        public DatabaseUpdate() {
            playerDelta = new Dictionary<Player, StateChange>();
            objectDelta = new Dictionary<MeshNetworkIdentity, StateChange>();
            fullHash = 0;
        }

        public DatabaseUpdate(Dictionary<Player, StateChange> players,
            Dictionary<MeshNetworkIdentity, StateChange> objects,
            ushort databaseHash) {
            
            playerDelta = players;
            objectDelta = objects;
            fullHash = databaseHash;
        }
        

        public void DeserializeAndApply(byte[] serializedData) {
            DatabaseUpdate decoded = DatabaseUpdate.ParseContentAsDatabaseUpdate(serializedData);
            this.playerDelta = decoded.playerDelta;
            this.objectDelta = decoded.objectDelta;
        }


        //Serialize the update into a bytestream, recursively serializing all
        //contained objects and players.
        public byte[] GetSerializedBytes() {
            List<byte> output = new List<byte>();
            

            byte numPlayers = (byte)playerDelta.Keys.Count;
            output.Add(numPlayers);
            foreach (Player p in playerDelta.Keys) {
                byte[] serializedPlayer = p.SerializeFull();
                output.Add((byte)serializedPlayer.Length);
                output.AddRange(serializedPlayer);
                output.Add((byte)playerDelta[p]);
            }
            byte numObjects = (byte)objectDelta.Keys.Count;
            output.Add(numObjects);
            foreach(MeshNetworkIdentity m in objectDelta.Keys) {
                byte[] serializedObject = m.GetSerializedBytes();
                output.AddRange(serializedObject);
                output.Add((byte)objectDelta[m]);
            }
            output.AddRange(BitConverter.GetBytes(fullHash));
            return output.ToArray();
        }

        //Deserialize incoming byte data and construct a deserialized DatabaseUpdate
        //Note, this is static
        public static DatabaseUpdate ParseContentAsDatabaseUpdate(byte[] serializedData) {

            Dictionary<Player, StateChange> playerList = new Dictionary<Player, StateChange>();
            Dictionary<MeshNetworkIdentity, StateChange> networkObjects = new Dictionary<MeshNetworkIdentity, StateChange>();

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
                pointer += blobLength;
                StateChange s = (StateChange)rawData[pointer];
                playerList.Add(p, s);
                pointer++;
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
                pointer += MeshNetworkIdentity.NETWORK_IDENTITY_BYTE_SIZE;
                StateChange s = (StateChange)rawData[pointer];
                networkObjects.Add(netid, s);
                pointer++;
                j++;
            }
            ushort hash = BitConverter.ToUInt16(rawData, pointer);
            return new DatabaseUpdate(playerList, networkObjects, hash);
        }


    }

    
    //All networked components must implement this.
    public interface IReceivesPacket<MeshPacket> {
        void ReceivePacket(MeshPacket p);
    }
    public interface IMeshSerializable {
        byte[] GetSerializedBytes();
    }
    public interface INetworked<MeshNetworkIdentity> {
        MeshNetworkIdentity GetIdentity();
        void SetIdentity(MeshNetworkIdentity id);
    }

    public class Testing {

        //Runs some checks to make sure that the serialization
        //systems are running and correctly translating the data.
        //TODO automate checking
        public static void DebugDatabaseSerialization(MeshNetworkIdentity dummy1, MeshNetworkIdentity dummy2) {
            Debug.Log("Creating player named Mary Jane.");
            Player p1 = new Player("Mary Jananee", 2233443, "abcde");
            Debug.Log("Creating player named John Smith");
            Player p2 = new Player("John Smith", 52342342, "12345");

            DatabaseUpdate db = new DatabaseUpdate();
            db.playerDelta.Add(p1, StateChange.Addition);
            db.playerDelta.Add(p2, StateChange.Removal);

            dummy1.SetObjectID(1337);
            dummy1.SetOwnerID(1234);
            dummy2.SetObjectID(4200);
            dummy2.SetOwnerID(4321);

            db.objectDelta.Add(dummy1, StateChange.Change);
            db.objectDelta.Add(dummy2, StateChange.Addition);

            Debug.Log("Total payload length: " + db.GetSerializedBytes().Length);
            //Debug.Log("Database hash: " + NetworkDatabase.GenerateDatabaseChecksum(db.playerDelta, db.objectDelta));
            MeshPacket p = new MeshPacket();
            p.SetPacketType(PacketType.DatabaseUpdate);
            p.SetSourceObjectId((byte)ReservedObjectIDs.DatabaseObject);
            p.SetSourcePlayerId(120);
            p.SetTargetObjectId((byte)ReservedObjectIDs.DatabaseObject);
            p.SetTargetPlayerId((byte)ReservedPlayerIDs.Broadcast);
            p.SetContents(db.GetSerializedBytes());
            
            byte[] transmitData = p.GetSerializedBytes();

            //THIS WOULD GET SENT ACROSS THE NETWORK

            MeshPacket received = new MeshPacket(transmitData);
            Debug.Log("Received packet:");
            Debug.Log("packetType: " + received.GetPacketType());
            Debug.Log("sourceObjectID: " + received.GetSourceObjectId());
            Debug.Log("sourcePlayerID: " + received.GetSourcePlayerId());
            Debug.Log("targetObjectID: " + received.GetTargetObjectId());
            Debug.Log("targetPlayerID: " + received.GetTargetPlayerId());
            Debug.Log("Payload length: " + received.GetContents().Length);

            DatabaseUpdate receivedDB = DatabaseUpdate.ParseContentAsDatabaseUpdate(received.GetContents());
            Debug.Log("Received DatabaseUpdate:");
            //Debug.Log("Database hash: " + NetworkDatabase.GenerateDatabaseChecksum(db.playerDelta, db.objectDelta));
            Debug.Log("Total number of objects: " + receivedDB.objectDelta.Count);
            int i = 1;
            foreach(MeshNetworkIdentity id in receivedDB.objectDelta.Keys) {
                Debug.Log("Object " + i + ": ");
                Debug.Log("objectID: " + id.GetObjectID());
                Debug.Log("prefabID: " + id.GetPrefabID());
                Debug.Log("ownerID : " + id.GetOwnerID());
                i++;
            }
            Debug.Log("Total number of players: " + receivedDB.playerDelta.Count);
            i = 1;
            foreach (Player player in receivedDB.playerDelta.Keys) {
                Debug.Log("Player " + i + ": ");
                Debug.Log("Desanitized Name: " + player.GetNameDesanitized());
                Debug.Log("Sanitized Name: " + player.GetNameSanitized());
                Debug.Log("uniqueID: " + player.GetUniqueID());
                Debug.Log("privateKey: " + player.GetPrivateKey());
                i++;
            }
        }
    }


}


using UnityEngine;
using System.Collections;
using System.Text;
using UnityEngine.Networking;
using Utilities;
using System.Collections.Generic;
using System;




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
    public enum ReservedPlayerIDs {
        Unspecified = 0,
        Broadcast = 1,
        Self = 2
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
        VOIP = 20,
        DatabaseUpdate = 10
    }

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
        private string displayName;
        private byte uniqueID;
        private string address;
        private string privateKey;

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

        public static Player DeserializeFull(byte[] bytes) {
            string s = Encoding.ASCII.GetString(bytes);
            string[] parts = s.Split(':');
            
            Player p = new Player(parts[0], byte.Parse(parts[1]), parts[2], parts[3]);
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
            Buffer.BlockCopy(serializedData, 8, data, 0, data.Length);

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

        public byte[] GetBytes() {
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


    public class Game {
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

    public interface IReceivesPacket<MeshPacket> {
        void ReceivePacket(MeshPacket p);
    }


}


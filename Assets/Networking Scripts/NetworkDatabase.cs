using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

public class NetworkDatabase : MonoBehaviour, IReceivesPacket<MeshPacket> {

    /*
        NetworkDatabase is a collection of information that is a summary of the server-authoritative game state.
        Each player has a local NetworkDatabase, but only one player's NetworkDatabase is truly
        the "server" authority. This authorized NetworkDatabase keeps track of object ownership and
        player status. All non-authorized NetworkDatabases mirror the one authorized NetworkDatabase, and try
        to keep as up to date as possible.

        ObjectId codes:
        0 = undefined object
        1 = NetworkDatabase object
        
        playerList: hashtable between playerID and Player object
        networkObjects: hashtable between objectID and MeshNetworkIdentity component

    */

    public byte authorizedID = (byte)ReservedPlayerIDs.Unspecified;

    byte myId = (byte)ReservedPlayerIDs.Unspecified; //uniqueID of zero indicates nonexistant player
    private Dictionary<byte, Player> playerList = new Dictionary<byte, Player>();
    private Dictionary<ushort, MeshNetworkIdentity> networkObjects = new Dictionary<ushort, MeshNetworkIdentity>();

	// Use this for initialization
	void Start () {
        playerList[myId] = new Player(); //Initialize ourselves. Contains dummy data that will be replaced.
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public Player GetSelf() {
        return playerList[myId];
    }
    
    public Dictionary<byte, Player> GetPlayers() {
        return playerList;
    }
    
    public Dictionary<ushort, MeshNetworkIdentity> GetObjects() {
        return networkObjects;
    }

    public int CalculateDatabaseHash() {
        return networkObjects.GetHashCode();
    }
    
    public int CalculatePlayerHash() {
        return playerList.GetHashCode();
    }

    public void SendDelta() {
        MeshPacket p = new MeshPacket();
        p.SetPacketType(PacketType.DatabaseUpdate);
        p.SetSourcePlayerId(myId);
        p.SetSourceObjectId((ushort)ReservedObjectIDs.DatabaseObject);
        p.SetTargetPlayerId((byte)ReservedPlayerIDs.Broadcast);
        p.SetTargetObjectId((ushort)ReservedObjectIDs.DatabaseObject);
    }

    public void ReceivePacket(MeshPacket p) {
        if(p.GetPacketType() == PacketType.DatabaseUpdate) {
            ProcessUpdate(MeshPacket.ParseContentAsDatabaseUpdate(p.GetData());
        }
        else {

        }

    }

    void ProcessUpdate(DatabaseUpdate update) {
        foreach(byte playerID in update.playerList.Keys) {
            if (playerList.ContainsKey(playerID)) {
                Debug.Log("Warning: Overriding existing player");
                playerList[playerID] = update.playerList[playerID];
            }
            else {
                playerList.Add(playerID, update.playerList[playerID]);
            }
            
        }
    }
}

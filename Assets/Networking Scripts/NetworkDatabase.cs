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



    byte myId = 0; //uniqueID of zero indicates nonexistant player
    private Dictionary<byte, Player> playerList = new Dictionary<byte, Player>();
    private Dictionary<ushort, MeshNetworkIdentity> networkObjects = new Dictionary<ushort, MeshNetworkIdentity>();

	// Use this for initialization
	void Start () {
        playerList[0] = new Player(); //temporary
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public Player GetSelf() {
        return playerList[myId];
    }
    
    public Player LookupPlayer(byte id) { //uses playerList like a hashtable
        return playerList[id];
    }

    public MeshNetworkIdentity RetrieveObject(ushort id) {
        return networkObjects[id];
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
        if(p.GetPacketType() != PacketType.DatabaseUpdate) {
            return;
        }

    }
}

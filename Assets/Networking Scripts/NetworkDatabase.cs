using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;
using Steamworks;

[RequireComponent(typeof(MeshNetworkIdentity))]
public class NetworkDatabase : MonoBehaviour, IReceivesPacket<MeshPacket>, INetworked<MeshNetworkIdentity> {

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
    
    public MeshNetwork meshnet; //MeshNetwork object. Set when MeshNetwork starts up.

    public MeshNetworkIdentity thisObjectIdentity; //Required for INetworked

    public byte authorizedID = (byte)ReservedPlayerIDs.Provider;

    byte myId = (byte)ReservedPlayerIDs.Unspecified; //uniqueID of zero indicates nonexistant player
    
    private Dictionary<byte, Player> playerList = new Dictionary<byte, Player>();
    private Dictionary<ushort, MeshNetworkIdentity> networkObjects = new Dictionary<ushort, MeshNetworkIdentity>();

    
	
	//Entirely destroy the database records.
    //For obvious reasons, try avoid doing this unless you know what you're doing.
	public void DestroyDatabase() {
        myId = (byte)ReservedPlayerIDs.Unspecified;
        playerList = new Dictionary<byte, Player>();
        networkObjects = new Dictionary<ushort, MeshNetworkIdentity>();
    }

    public MeshNetworkIdentity GetIdentity() {
        return thisObjectIdentity;
    }

    public Player GetSelf() {
        return playerList[myId];
    }
    
    public void AddPlayer(Player p) {
        if (playerList.ContainsKey((byte)ReservedPlayerIDs.Provider)) {
            Debug.LogError("Trying to replace the provider on this network! Hotswap not implemented!");
            return;
        }
        playerList.Add(p.GetUniqueID(), p);
    }

    public Player LookupPlayer(byte byteID) {
        if (playerList.ContainsKey(byteID))
            return playerList[byteID];
        else
            return null;
    }

    public Player LookupPlayer(CSteamID steamID) {
        foreach(Player p in playerList.Values) {
            if (p.GetSteamID().Equals(steamID)) {
                return p;
            }
        }
        return null;
    }

    public MeshNetworkIdentity LookupObject(ushort objectID) {
        if (networkObjects.ContainsKey(objectID))
            return networkObjects[objectID];
        else
            return null;

    }

    public Player[] GetAllPlayers() {
        Player[] output = new Player[0];
        playerList.Values.CopyTo(output, 0);
        return output;
    }
    

    public void SetMyID(byte me) {
        myId = me;
    }

    public int CalculateDatabaseHash() {
        return networkObjects.GetHashCode();
    }
    
    public int CalculatePlayerHash() {
        return playerList.GetHashCode();
    }

    public void SendDelta(Dictionary<byte, Player> playerUpdate, Dictionary<ushort, MeshNetworkIdentity> objectUpdate) {
        MeshPacket p = new MeshPacket();
        p.SetPacketType(PacketType.DatabaseUpdate);
        p.qos = EP2PSend.k_EP2PSendReliable;
        p.SetSourcePlayerId(myId);
        p.SetSourceObjectId((ushort)ReservedObjectIDs.DatabaseObject);
        p.SetTargetPlayerId((byte)ReservedPlayerIDs.Broadcast);
        p.SetTargetObjectId((ushort)ReservedObjectIDs.DatabaseObject);

        DatabaseUpdate update = new DatabaseUpdate(playerUpdate, objectUpdate);
        p.SetData(update.GetSerializedBytes());
        meshnet.RoutePacket(p);
    }

    public void ReceivePacket(MeshPacket p) {
        if(p.GetPacketType() == PacketType.DatabaseUpdate) {
            ProcessUpdate(DatabaseUpdate.ParseContentAsDatabaseUpdate(p.GetData()));
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

    public byte RequestAvailableID() {
        for(byte i = (byte)ReservedPlayerIDs.FirstAvailable; i < byte.MaxValue; i++) {
            if (!playerList.ContainsKey(i)) {
                return i;
            }
        }
        return (byte)ReservedPlayerIDs.Unspecified;
    }
}

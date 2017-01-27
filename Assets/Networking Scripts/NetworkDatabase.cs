using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;
using Steamworks;

[RequireComponent(typeof(MeshNetworkIdentity))]
public class NetworkDatabase : MonoBehaviour, IReceivesPacket<MeshPacket>, INetworked<MeshNetworkIdentity> {

    /*
        NetworkDatabase is a collection of information that is a summary of the server-authoritative game state.
        The clever part of this is that the NetworkDatabase is an INetworked object just
        like everything else in the game. This means that only one of this object actually
        exists across the network! All the other NetworkDatabases on the client computers
        are "shadows" of the real NetworkDatabase, just like all of the props and objects.
        The MeshNetworkIdentity that is attached to this component contains the ownerID of the
        database, and this is the ID that determines the provider of the real database
        information. Just like a coffee cup has an owner, and that owner has the definitive
        information on that coffee cup, the database also has an owner.
        
        playerList: hashtable between playerID and Player object
        objectList: hashtable between objectID and MeshNetworkIdentity component

    */

    public bool UseFullUpdates = false; //Should the network database send the entire database every time something changes?
    public MeshNetwork meshnet; //MeshNetwork object. Set when MeshNetwork starts up
    public MeshNetworkIdentity thisObjectIdentity; //Required for INetworked

    //Serialized below here.
    private Dictionary<ulong, Player> playerList = new Dictionary<ulong, Player>();
    private Dictionary<ushort, MeshNetworkIdentity> objectList = new Dictionary<ushort, MeshNetworkIdentity>();

    private Dictionary<ulong, Player> playerListDelta = new Dictionary<ulong, Player>();
    private Dictionary<ushort, MeshNetworkIdentity> objectListDelta = new Dictionary<ushort, MeshNetworkIdentity>();


    //Entirely destroy the database records.
    //For obvious reasons, try avoid doing this unless you know what you're doing.
    public void DestroyDatabase() {
        playerList = new Dictionary<ulong, Player>();
        objectList = new Dictionary<ushort, MeshNetworkIdentity>();
    }

    public MeshNetworkIdentity GetIdentity() {
        return thisObjectIdentity;
    }
    
    public void AddPlayer(Player p) {
        if (playerList.ContainsKey(p.GetUniqueID())) {
            Debug.LogError("User already exists!");
            return;
        }
        if(p.GetUniqueID() == GetIdentity().GetOwnerID() && p.GetUniqueID() != (ulong)ReservedPlayerIDs.Unspecified) {
            Debug.LogError("New user trying to override current provider. Hotswap not yet implemented.");
        }
        playerList.Add(p.GetUniqueID(), p);
        FlagChange(p);
    }

    public Player LookupPlayer(byte byteID) {
        if (playerList.ContainsKey(byteID))
            return playerList[byteID];
        else
            return null;
    }

    public Player LookupPlayer(ulong id) {
        foreach(Player p in playerList.Values) {
            if (p.GetUniqueID().Equals(id)) {
                return p;
            }
        }
        return null;
    }

    public MeshNetworkIdentity LookupObject(ushort objectID) {
        if (objectList.ContainsKey(objectID))
            return objectList[objectID];
        else
            return null;

    }

    public Player[] GetAllPlayers() {
        Player[] output = new Player[0];
        playerList.Values.CopyTo(output, 0);
        return output;
    }
    
    private void FlagChange(Player p) {
        playerListDelta.Add(p.GetUniqueID(), p);
    }
    private void FlagChange(MeshNetworkIdentity i) {
        objectListDelta.Add(i.GetObjectID(), i);
    }
    public void ProcessUpdate() {
        SendDelta(playerListDelta, objectListDelta);
    }
    
    public static ushort GenerateDatabaseChecksum(Dictionary<ulong, Player> playerList,
        Dictionary<ushort, MeshNetworkIdentity> objectList) {

        DatabaseUpdate container = new DatabaseUpdate(playerList, objectList);
        byte[] data = container.GetSerializedBytes();
        ushort checksum = 0;
        foreach (byte cur_byte in data) {
            checksum = (ushort)(((checksum & 0xFFFF) >> 1) + ((checksum & 0x1) << 15)); // Rotate the accumulator
            checksum = (ushort)((checksum + cur_byte) & 0xFFFF);                        // Add the next chunk
        }
        return checksum;
    }

    private void SendDelta(Dictionary<ulong, Player> playerUpdate, Dictionary<ushort, MeshNetworkIdentity> objectUpdate) {
        MeshPacket p = new MeshPacket();
        p.SetPacketType(PacketType.DatabaseUpdate);
        p.qos = EP2PSend.k_EP2PSendReliable;
        p.SetSourcePlayerId(GetIdentity().GetOwnerID());
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
    
}

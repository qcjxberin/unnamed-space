using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;
using Steamworks;

[RequireComponent(typeof(IdentityContainer))]
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
    public Player[] threadsafePlayerList = new Player[0];
    public MeshNetworkIdentity[] threadSafeObjectList = new MeshNetworkIdentity[0];

    //Entirely destroy the database records.
    //For obvious reasons, try avoid doing this unless you know what you're doing.
    public void DestroyDatabase() {
        playerList = new Dictionary<ulong, Player>();
        objectList = new Dictionary<ushort, MeshNetworkIdentity>();
    }

    public MeshNetworkIdentity GetIdentity() {
        return thisObjectIdentity;
    }
    public void SetIdentity(MeshNetworkIdentity id) {
        thisObjectIdentity = id;
    }

    public bool GetAuthorized() {
        if (meshnet.GetSteamID() == GetIdentity().GetOwnerID()) {
            return true;
        }
        else {
            return false;
        }
    }

    public void AddPlayer(Player p) {

        if (playerList.ContainsKey(p.GetUniqueID())) {
            Debug.LogError("User already exists!");
            return;
        }
        if (p.GetUniqueID() == GetIdentity().GetOwnerID() && p.GetUniqueID() != (ulong)ReservedPlayerIDs.Unspecified) {
            if (!GetAuthorized()) {
                Debug.Log("Unauthorized user trying to override provider.");
            }

        }
        ulong uniqueID = p.GetUniqueID();
        playerList.Add(uniqueID, p);
        if (GetAuthorized()) {
            Debug.Log("Sending player update");
            SendPlayerUpdate(p, StateChange.Addition);
        }
    }

    public void RemovePlayer(Player p) {
        if(playerList.ContainsKey(p.GetUniqueID()) == false) {
            Debug.LogError("User that was requested to be removed does not exist!");
            return;
        }
        if (p.GetUniqueID() == GetIdentity().GetOwnerID() && p.GetUniqueID() != (ulong)ReservedPlayerIDs.Unspecified) {
            Debug.LogError("Trying to delete provider. This definitely isn't supposed to happen.");
            return;
        }
        playerList.Remove(p.GetUniqueID());
        if (GetAuthorized()) {
            SendPlayerUpdate(p, StateChange.Removal);
        }
    }

    public void ChangePlayer(Player p) {
        if(playerList.ContainsKey(p.GetUniqueID()) == false) {
            Debug.LogError("Trying to modify player object that doesn't exist here!");
            return;
        }
        if (p.GetUniqueID() == GetIdentity().GetOwnerID() && p.GetUniqueID() != (ulong)ReservedPlayerIDs.Unspecified) {
            Debug.LogError("Trying to modify provider. This isn't supposed to happen.");
            return;
        }
        playerList[p.GetUniqueID()] = p;
        if (GetAuthorized()) {
            SendPlayerUpdate(p, StateChange.Change);
        } 
    }

    public void AddObject(MeshNetworkIdentity i) {
        if (objectList.ContainsKey(i.GetObjectID())) {
            Debug.LogError("Object already exists!");
            return;
        }
        if(i.GetObjectID() == (ushort)ReservedObjectIDs.DatabaseObject) {
            Debug.Log("Creating database object. This should only happen once!");
        }
        objectList.Add(i.GetObjectID(), i);
        if (GetAuthorized()) {
            SendObjectUpdate(i, StateChange.Addition);
        }
    }
    public void RemoveObject(MeshNetworkIdentity i) {
        if(objectList.ContainsKey(i.GetObjectID()) == false) {
            Debug.LogError("Object that was requested to be removed does not exist!");
        }
        if(i.GetObjectID() == (ushort)ReservedObjectIDs.DatabaseObject) {
            Debug.LogError("Tried to remove database. Bad idea.");
            return;
        }
        objectList.Remove(i.GetObjectID());
        if (GetAuthorized()) {
            SendObjectUpdate(i, StateChange.Removal);
        }
    }
    public void ChangeObject(MeshNetworkIdentity i) {
        if (objectList.ContainsKey(i.GetObjectID()) == false) {
            Debug.LogError("Object that was requested to be changed does not exist!");
        }
        if (i.GetObjectID() == (ushort)ReservedObjectIDs.DatabaseObject) {
            Debug.LogError("Tried to change database. This action is prohibited.");
            return;
        }
        objectList.Remove(i.GetObjectID());
        if (GetAuthorized()) {
            SendObjectUpdate(i, StateChange.Change);
        }
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
        Debug.Log("getting all players");
        Player[] output = new Player[playerList.Count];
        playerList.Values.CopyTo(output, 0);
        return output;
        
    }
    
    private void SendPlayerUpdate(Player p, StateChange s) {
        Dictionary<Player, StateChange> playerListDelta = new Dictionary<Player, StateChange>();
        Dictionary<MeshNetworkIdentity, StateChange> objectListDelta = new Dictionary<MeshNetworkIdentity, StateChange>();
        playerListDelta.Add(p, s);
        SendDelta(playerListDelta, objectListDelta);
    }
    private void SendObjectUpdate(MeshNetworkIdentity id, StateChange s) {
        Dictionary<Player, StateChange> playerListDelta = new Dictionary<Player, StateChange>();
        Dictionary<MeshNetworkIdentity, StateChange> objectListDelta = new Dictionary<MeshNetworkIdentity, StateChange>();
        objectListDelta.Add(id, s);
        SendDelta(playerListDelta, objectListDelta);
    }

    public static ushort GenerateDatabaseChecksum(Dictionary<ulong, Player> playerList,
        Dictionary<ushort, MeshNetworkIdentity> objectList) {

        //Always use zero for the hash when we create this dummy container.
        //Otherwise we'd be hashing a hash!
        Dictionary<Player, StateChange> fakePlayerDelta = new Dictionary<Player, StateChange>();
        foreach(Player p in playerList.Values) {
            fakePlayerDelta.Add(p, StateChange.Change);
        }
        Dictionary<MeshNetworkIdentity, StateChange> fakeObjectDelta = new Dictionary<MeshNetworkIdentity, StateChange>();
        foreach(MeshNetworkIdentity m in objectList.Values) {
            fakeObjectDelta.Add(m, StateChange.Change);
        }
        DatabaseUpdate container = new DatabaseUpdate(fakePlayerDelta, fakeObjectDelta, 0);
        byte[] data = container.GetSerializedBytes();
        ushort checksum = 0;
        foreach (byte cur_byte in data) {
            checksum = (ushort)(((checksum & 0xFFFF) >> 1) + ((checksum & 0x1) << 15)); // Rotate the accumulator
            checksum = (ushort)((checksum + cur_byte) & 0xFFFF);                        // Add the next chunk
        }
        return checksum;
    }

    private void SendDelta(Dictionary<Player, StateChange> playerUpdate, Dictionary<MeshNetworkIdentity, StateChange> objectUpdate) {
        MeshPacket p = new MeshPacket();
        p.SetPacketType(PacketType.DatabaseUpdate);
        p.qos = EP2PSend.k_EP2PSendReliable;
        p.SetSourcePlayerId(GetIdentity().GetOwnerID());
        p.SetSourceObjectId((ushort)ReservedObjectIDs.DatabaseObject);
        p.SetTargetPlayerId((byte)ReservedPlayerIDs.Broadcast);
        p.SetTargetObjectId((ushort)ReservedObjectIDs.DatabaseObject);

        ushort hash = GenerateDatabaseChecksum(playerList, objectList);
        DatabaseUpdate update = new DatabaseUpdate(playerUpdate, objectUpdate, hash);
        p.SetContents(update.GetSerializedBytes());
        meshnet.RoutePacket(p);
    }

    public void ReceivePacket(MeshPacket p) {
        if(p.GetPacketType() == PacketType.DatabaseUpdate) {
            if(p.GetSourcePlayerId() == GetIdentity().GetOwnerID()) { //if the sender is authorized to make changes
                ReceiveUpdate(DatabaseUpdate.ParseContentAsDatabaseUpdate(p.GetContents()));
            }
        }
        else {

        }

    }
    
    //This is called when the authorized database sends an update to this database.
    //If this object is the authorized database, this should never be called.
    public void ReceiveUpdate(DatabaseUpdate dbup) {
        foreach(Player p in dbup.playerDelta.Keys) {
            if(dbup.playerDelta[p] == StateChange.Addition) {
                AddPlayer(p);
            }else if(dbup.playerDelta[p] == StateChange.Removal) {
                RemovePlayer(p);
            }else if(dbup.playerDelta[p] == StateChange.Change) {
                ChangePlayer(p);
            }
        }
        foreach (MeshNetworkIdentity i in dbup.objectDelta.Keys) {
            if (dbup.objectDelta[i] == StateChange.Addition) {
                AddObject(i);
            }
            else if (dbup.objectDelta[i] == StateChange.Removal) {
                RemoveObject(i);
            }
            else if (dbup.objectDelta[i] == StateChange.Change) {
                ChangeObject(i);
            }
        }
    }
    
}

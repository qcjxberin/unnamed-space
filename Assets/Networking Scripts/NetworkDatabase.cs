using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;
using Steamworks;

[RequireComponent(typeof(IdentityContainer))]
public class NetworkDatabase : MonoBehaviour, IReceivesPacket<MeshPacket>, INetworked<MeshNetworkIdentity> {

    /*
        NetworkDatabase.cs
        Copyright 2017 Finn Sinclair

        NetworkDatabase is a collection of information that is a summary of the server-authoritative game state.
        The clever part of this is that the NetworkDatabase is an INetworked object just
        like everything else in the game. This means that only one of this object actually
        exists across the network! All the other NetworkDatabases on the client computers
        are "shadows" of the real NetworkDatabase, just like all of the props and objects.
        The MeshNetworkIdentity that is attached to this component contains the ownerID of the
        database, and this is the ID that determines the provider of the real database
        information. Just like a coffee cup has an owner, and that owner has the definitive
        information on that coffee cup, the database also has an owner.

        This does NOT yet support hot-swapping authorized users. Each game session must have
        one and only one provider, and it must not change throughout the session. TODO: implement
        code for SteamMatchmaking that terminates the session when the provider leaves. Otherwise,
        SteamMatchmaking will just set somebody to be the authorized user, which will break everything.
        
        playerList: hashtable between playerID and Player object
        objectList: hashtable between objectID and MeshNetworkIdentity component

    */

    public bool UseFullUpdates = false; //Should the network database send the entire database every time something changes?
    public MeshNetworkIdentity thisObjectIdentity; //Required for INetworked
    UnityEngine.UI.Text debugText;

    //Serialized below here.
    private Dictionary<ulong, Player> playerList = new Dictionary<ulong, Player>();
    private Dictionary<ushort, MeshNetworkIdentity> objectList = new Dictionary<ushort, MeshNetworkIdentity>();

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

    //Hunt down some relevant gameobjects to keep track of
    public void OnEnable() {
        GameObject debug = GameObject.FindGameObjectWithTag("DatabaseDebug");
        if(debug != null) {
            UnityEngine.UI.Text t = debug.GetComponent<UnityEngine.UI.Text>();
            if(t != null) {
                debugText = t;
                Debug.Log("Succesfully set debug text");
            }
            else {
                Debug.Log("Couldn't find text component");
            }
        }
        else {
            Debug.Log("Couldn't find debug text.");
        }
        GameObject mn = GameObject.FindGameObjectWithTag("DatabaseDebug");
        if (debug != null) {
            UnityEngine.UI.Text t = debug.GetComponent<UnityEngine.UI.Text>();
            if (t != null) {
                debugText = t;
                Debug.Log("Succesfully set debug text");
            } else {
                Debug.Log("Couldn't find text component");
            }
        } else {
            Debug.Log("Couldn't find debug text.");
        }
    }

    //If we have debug readout, update the readout
    public void Update() {
        if(debugText != null) {
            string s = "";
            s += "Players: ";
            foreach(Player p in playerList.Values) {
                s += p.GetNameSanitized() + ":" + p.GetUniqueID();
                s += "\n";
            }
            s += "\nObjects: ";
            foreach (MeshNetworkIdentity i in objectList.Values) {
                s += i.GetPrefabID() + ":" + i.GetOwnerID();
                s += "\n";
            }
            debugText.text = s;
        }
    }

    //Check with MeshNetwork to see if we are the authorized user in this lobby/game/etc
    public bool GetAuthorized() {
        return GetIdentity().IsLocallyOwned();
    }



    //Add a player to the database, and readies the change for sending to peers.
    public void AddPlayer(Player p) {
        if (!GetAuthorized()) {
            Debug.LogError("Database change not authorized.");
            return;
        }
            
        Debug.Log("Adding player");
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
        if (!GetAuthorized()) {
            Debug.LogError("Database change not authorized.");
            return;
        }
        if (playerList.ContainsKey(p.GetUniqueID()) == false) {
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
        if (!GetAuthorized()) {
            Debug.LogError("Database change not authorized.");
            return;
        }
        if (playerList.ContainsKey(p.GetUniqueID()) == false) {
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
        if (!GetAuthorized()) {
            Debug.LogError("Database change not authorized.");
            return;
        }
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
        if (!GetAuthorized()) {
            Debug.LogError("Database change not authorized.");
            return;
        }
        if (objectList.ContainsKey(i.GetObjectID()) == false) {
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
        if (!GetAuthorized()) {
            Debug.LogError("Database change not authorized.");
            return;
        }
        if (objectList.ContainsKey(i.GetObjectID()) == false) {
            Debug.LogError("Object that was requested to be changed does not exist!");
        }
        if (i.GetObjectID() == (ushort)ReservedObjectIDs.DatabaseObject) {
            Debug.LogError("Tried to change database. This action is prohibited."); //maybe not in the future...
            return;
        }
        objectList[i.GetObjectID()] = i;
        if (GetAuthorized()) {
            SendObjectUpdate(i, StateChange.Change);
        }
    }
    

    public Player LookupPlayer(ulong id) {

        if (playerList.ContainsKey(id)) {
            return playerList[id]; //Hash table enables very fast lookup
        }
        else {
            Debug.LogError("LookupPlayer() cannot find indicated playerID" + id);
            return null;
        }
    }

    public MeshNetworkIdentity LookupObject(ushort objectID) {
        if (objectList.ContainsKey(objectID)) {
            return objectList[objectID]; //Hash table enables very fast lookup
        }
        else {
            Debug.LogError("LookupObject() cannot find indicated playerID" + objectID);
            return null;
        }
    }

    public Player[] GetAllPlayers() {
        
        Player[] output = new Player[playerList.Count];
        playerList.Values.CopyTo(output, 0);
        return output;
        
    }
    
    private void SendPlayerUpdate(Player p, StateChange s) {
        Dictionary<Player, StateChange> playerListDelta = new Dictionary<Player, StateChange>();
        Dictionary<MeshNetworkIdentity, StateChange> objectListDelta = new Dictionary<MeshNetworkIdentity, StateChange>();
        playerListDelta.Add(p, s);
        SendDelta(playerListDelta, objectListDelta, (ulong)ReservedPlayerIDs.Broadcast);
    }
    private void SendObjectUpdate(MeshNetworkIdentity id, StateChange s) {
        Dictionary<Player, StateChange> playerListDelta = new Dictionary<Player, StateChange>();
        Dictionary<MeshNetworkIdentity, StateChange> objectListDelta = new Dictionary<MeshNetworkIdentity, StateChange>();
        objectListDelta.Add(id, s);
        SendDelta(playerListDelta, objectListDelta, (ulong)ReservedPlayerIDs.Broadcast);
    }

    
    

    public static ushort GenerateDatabaseChecksum(Dictionary<ulong, Player> players,
        Dictionary<ushort, MeshNetworkIdentity> objects) {

        ushort hash = 0x0;

        foreach(Player p in players.Values) {
            Dictionary<Player, StateChange> fakePlayerDelta = new Dictionary<Player, StateChange>();
            Dictionary<MeshNetworkIdentity, StateChange> fakeObjectDelta = new Dictionary<MeshNetworkIdentity, StateChange>();
            fakePlayerDelta.Add(p, StateChange.Change);
            DatabaseUpdate fakeUpdate = new DatabaseUpdate(fakePlayerDelta, fakeObjectDelta, 0);
            byte[] data = fakeUpdate.GetSerializedBytes();
            ushort checksum = 0;
            foreach (byte cur_byte in data) {
                checksum = (ushort)(((checksum & 0xFFFF) >> 1) + ((checksum & 0x1) << 15)); // Rotate the accumulator
                checksum = (ushort)((checksum + cur_byte) & 0xFFFF);                        // Add the next chunk
            }

            hash = (ushort)(hash ^ checksum);
        }
        foreach (MeshNetworkIdentity i in objects.Values) {
            Dictionary<Player, StateChange> fakePlayerDelta = new Dictionary<Player, StateChange>();
            Dictionary<MeshNetworkIdentity, StateChange> fakeObjectDelta = new Dictionary<MeshNetworkIdentity, StateChange>();
            fakeObjectDelta.Add(i, StateChange.Change);
            DatabaseUpdate fakeUpdate = new DatabaseUpdate(fakePlayerDelta, fakeObjectDelta, 0);
            byte[] data = fakeUpdate.GetSerializedBytes();
            ushort checksum = 0;
            foreach (byte cur_byte in data) {
                checksum = (ushort)(((checksum & 0xFFFF) >> 1) + ((checksum & 0x1) << 15)); // Rotate the accumulator
                checksum = (ushort)((checksum + cur_byte) & 0xFFFF);                        // Add the next chunk
            }

            hash = (ushort)(hash ^ checksum);
        }

        return hash;


    }

    private void SendDelta(Dictionary<Player, StateChange> playerUpdate, Dictionary<MeshNetworkIdentity, StateChange> objectUpdate, ulong targetPlayerID) {
        if(objectList.ContainsValue(GetIdentity()) == false ||
            playerList.ContainsKey(GetIdentity().GetOwnerID()) == false){
            Debug.Log("Trying to send delta when database is not yet fully set up. Skipping");
            return;
        }


        MeshPacket p = new MeshPacket();
        p.SetPacketType(PacketType.DatabaseUpdate);
        p.qos = EP2PSend.k_EP2PSendReliable;
        p.SetSourcePlayerId(GetIdentity().GetOwnerID());
        p.SetSourceObjectId((ushort)ReservedObjectIDs.DatabaseObject);
        p.SetTargetPlayerId(targetPlayerID);
        p.SetTargetObjectId((ushort)ReservedObjectIDs.DatabaseObject);
        
        //Check if the database is included in the delta
        bool flag = false;
        foreach(MeshNetworkIdentity i in objectUpdate.Keys) {
            if(i.GetPrefabID() == GetIdentity().GetPrefabID()) {
                flag = true;
            }
        }
        //If not, add database to delta. (The database should always be included.)
        if (flag == false) {
            objectUpdate[GetIdentity()] = StateChange.Addition;
        }
        //Check if the owner is included in the delta
        flag = false;
        foreach (Player pl in playerUpdate.Keys) {
            if (pl.GetUniqueID() == GetIdentity().GetOwnerID()) {
                flag = true;
            }
        }
        //If not, add owner to delta. (The owner should always be included.)
        if (flag == false) {
            
            playerUpdate[playerList[GetIdentity().GetOwnerID()]] = StateChange.Addition;
        }
        ushort hash = GenerateDatabaseChecksum(playerList, objectList);
        DatabaseUpdate update = new DatabaseUpdate(playerUpdate, objectUpdate, hash);
        p.SetContents(update.GetSerializedBytes());
        GetIdentity().RoutePacket(p);
    }

    public void ReceivePacket(MeshPacket p) {
        if(p.GetPacketType() == PacketType.DatabaseUpdate) {
            if(p.GetSourcePlayerId() == GetIdentity().GetOwnerID()) { //if the sender is authorized to make changes
                ReceiveUpdate(DatabaseUpdate.ParseContentAsDatabaseUpdate(p.GetContents()));
            }
        }else if(p.GetPacketType() == PacketType.FullUpdateRequest){
            SendFullUpdate(p.GetSourcePlayerId());
        } else if(p.GetPacketType() == PacketType.DatabaseChangeRequest) {
            ConsiderChangeRequest(p);
        }

    }
    
    public void SendFullUpdate(ulong sourceID) {
        Debug.Log("Sending full update.");
        Dictionary<Player, StateChange> playerUpdate = new Dictionary<Player, StateChange>();
        Dictionary<MeshNetworkIdentity, StateChange> objectUpdate = new Dictionary<MeshNetworkIdentity, StateChange>();

        foreach(Player p in playerList.Values) {
            playerUpdate[p] = StateChange.Override;
        }
        foreach(MeshNetworkIdentity i in objectList.Values) {
            objectUpdate[i] = StateChange.Override;
        }
        SendDelta(playerUpdate, objectUpdate, sourceID); //This should contain the database, so the delta algorithm shouldn't add it in
    }

    
    //This is called when the authorized database sends an update to this database.
    //If this object is the authorized database, this should never be called.
    public void ReceiveUpdate(DatabaseUpdate dbup) {
        foreach (Player p in dbup.playerDelta.Keys) {
            if (dbup.playerDelta[p] == StateChange.Addition) {
                playerList.Add(p.GetUniqueID(), p);
            } else if (dbup.playerDelta[p] == StateChange.Removal) {
                playerList.Remove(p.GetUniqueID());
            } else if (dbup.playerDelta[p] == StateChange.Change) {
                playerList[p.GetUniqueID()] = p;
            } else if (dbup.playerDelta[p] == StateChange.Override) { //Probably coming from a FullUpdate
                playerList.Remove(p.GetUniqueID());
                playerList.Add(p.GetUniqueID(), p);
            }
        }
        foreach (MeshNetworkIdentity i in dbup.objectDelta.Keys) {
            if (dbup.objectDelta[i] == StateChange.Addition) {
                objectList.Add(i.GetObjectID(), i);
            }
            else if (dbup.objectDelta[i] == StateChange.Removal) {
                objectList.Remove(i.GetObjectID());
            }
            else if (dbup.objectDelta[i] == StateChange.Change) {
                objectList[i.GetObjectID()] = i;
            } else if (dbup.objectDelta[i] == StateChange.Override) { //Probably coming from a FullUpdate
                objectList.Remove(i.GetObjectID());
                objectList.Add(i.GetObjectID(), i);
            }
        }

        ushort check = GenerateDatabaseChecksum(playerList, objectList);
        if(check != dbup.fullHash) {
            Debug.Log("Database checksum doesn't match: " + check + " vs " + dbup.fullHash + ". Requesting full update.");
            MeshPacket p = new MeshPacket(new byte[0], PacketType.FullUpdateRequest,
                GetIdentity().meshnetReference.GetSteamID(),
                GetIdentity().GetOwnerID(),
                GetIdentity().GetObjectID(),
                GetIdentity().GetObjectID());
            GetIdentity().RoutePacket(p);
        }else {
            Debug.Log("Delta successful, hash matches");
        }
    }

    public void RequestStateChange(MeshNetworkIdentity i, StateChange c) {

        if (GetAuthorized()) {
            Debug.LogError("'Requesting' to change database entry, but already authorized.");
            return;
        }
        Dictionary<MeshNetworkIdentity, StateChange> objectDict = new Dictionary<MeshNetworkIdentity, StateChange>();
        objectDict.Add(i, c);
        DatabaseUpdate dbup = new DatabaseUpdate(new Dictionary<Player, StateChange>(), objectDict, 0);
        MeshPacket p = new MeshPacket(dbup.GetSerializedBytes(),
            PacketType.DatabaseChangeRequest,
            GetIdentity().meshnetReference.GetSteamID(),
            GetIdentity().GetOwnerID(),
            GetIdentity().GetObjectID(),
            GetIdentity().GetObjectID());

        GetIdentity().RoutePacket(p);
    }

    public void ConsiderChangeRequest(MeshPacket p) {
        if(GetAuthorized() == false) {
            Debug.LogError("Being asked to change the database when not authorized!");
            return;
        }
    }
    
}

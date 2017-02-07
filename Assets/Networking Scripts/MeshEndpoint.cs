using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Utilities;
using Steamworks;

public class MeshEndpoint:MonoBehaviour {

    /*
        ServerManager.cs
        Copyright 2017 Finn Sinclair

        ServerManager is a container of many nodes and the main window into
        the mesh network. All packets come and go through this script. It is
        in charge of routing packets from various objects to the correct target
        player, and routing packets from remote users to the correct local objects.
        The NetworkDatabase uses the ServerManager to send and receive database
        updates, and the NetworkCoordinator gives the ServerManager the information
        to create the necessary hosts and peer connections.
    */

    public string shout;
    public MeshNetwork meshnet;

    List<MeshPacket> failedPackets = new List<MeshPacket>();
    Dictionary<MeshPacket, int> packetRetries = new Dictionary<MeshPacket, int>();

    //Checks for packets from all servers.


    public void Update() {
        Receive();
    }
    public void Receive() {
        if (failedPackets.Count > 0) {
            MeshPacket p = failedPackets[0];
            failedPackets.RemoveAt(0);
            ParseData(p);
        }


        uint bufferLength = 0;
        if (SteamNetworking.IsP2PPacketAvailable(out bufferLength)) {
            Debug.Log("Receiving Packet");
            byte[] destBuffer = new byte[bufferLength];
            UInt32 bytesRead = 0;
            CSteamID remoteID;
            SteamNetworking.ReadP2PPacket(destBuffer, bufferLength, out bytesRead, out remoteID);

            ParseData(new MeshPacket(destBuffer));
        }
        
        
    }

    
    void ParseData(MeshPacket incomingPacket) {

        if(incomingPacket.GetSourcePlayerId() == SteamUser.GetSteamID().m_SteamID) {
            Debug.Log("Discarding packet from self");
            return;
        }


        if(incomingPacket.GetPacketType() == PacketType.PlayerJoin) {
            if(meshnet.database == null) {
                Debug.LogError("Database not intialized yet!");
                return;
            }
            if(meshnet.database.GetAuthorized() == false) {
                Debug.Log("I'm not the provider. Discarding PlayerJoin packet");
                return;
            }
            CSteamID sID = new CSteamID(incomingPacket.GetSourcePlayerId());
            Player p = meshnet.ConstructPlayer(sID);
            meshnet.database.AddPlayer(new Player());
            return;

        }else if(incomingPacket.GetPacketType() == PacketType.DatabaseUpdate) {
            if(meshnet.database == null) {
                Debug.Log("Received first database update, no database to send it to.");
                Debug.Log("Rerouting to MeshNetwork.");
                meshnet.InitializeDatabaseClientside(incomingPacket);
                return;
            }
        }
        //if the packet is neither a PlayerJoin or a DatabaseUpdate

        Player source = meshnet.database.LookupPlayer(incomingPacket.GetSourcePlayerId()); //retrieve which player sent this packet
        if (source == null) { //hmmm, the NBD can't find the player
            Debug.LogError("Player from which packet originated does not exist on local NDB.");
            return;
        }

        MeshNetworkIdentity targetObject = meshnet.database.LookupObject(incomingPacket.GetTargetObjectId());
        if (targetObject == null) {
            Debug.LogError("Packet's target object doesn't exist on the database!");
            return;
        }

        targetObject.ReceivePacket(incomingPacket);


    }

    public void SendDirectToSteamID(MeshPacket packet, CSteamID id) {
        byte[] data = packet.GetSerializedBytes();
        SteamNetworking.SendP2PPacket(id, data, (uint)data.Length, EP2PSend.k_EP2PSendReliable);
    }

    public void Send(MeshPacket packet) {
        if(meshnet.database == null) {
            Debug.LogError("Trying to send packet when database does not exist.");
        }
        byte[] data = packet.GetSerializedBytes();
        Player[] allPlayers = meshnet.database.GetAllPlayers();
        if (packet.GetTargetPlayerId() == (byte)ReservedPlayerIDs.Broadcast) {
            foreach (Player p in allPlayers) {
                SteamNetworking.SendP2PPacket(new CSteamID(p.GetUniqueID()), data, (uint)data.Length, packet.qos);
            }
        }
        else {
            Player target = meshnet.database.LookupPlayer(packet.GetTargetPlayerId());
            SteamNetworking.SendP2PPacket(new CSteamID(target.GetUniqueID()), data, (uint)data.Length, packet.qos);
        }
        
        
    }

    
    

}

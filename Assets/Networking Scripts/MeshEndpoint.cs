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
    public NetworkDatabase ndb;

    List<MeshPacket> failedPackets = new List<MeshPacket>();
    Dictionary<MeshPacket, int> packetRetries = new Dictionary<MeshPacket, int>();

    //Checks for packets from all servers.
    public void Receive() {
        if (failedPackets.Count > 0) {
            MeshPacket p = failedPackets[0];
            failedPackets.RemoveAt(0);
            ParseData(p);
        }


        uint bufferLength = 0;
        if (SteamNetworking.IsP2PPacketAvailable(out bufferLength)) {
            byte[] destBuffer = new byte[bufferLength];
            UInt32 bytesRead = 0;
            CSteamID remoteID;
            SteamNetworking.ReadP2PPacket(destBuffer, bufferLength, out bytesRead, out remoteID);

            ParseData(new MeshPacket(destBuffer));
        }
        
        
    }

    
    void ParseData(MeshPacket incomingPacket) {
        
        if(incomingPacket.GetPacketType() == (byte)PacketType.Generic){ //Generic game packet, we have to examine this
            
            Player source = ndb.LookupPlayer(incomingPacket.GetSourcePlayerId()); //retrieve which player sent this packet
            if(source == null) { //hmmm, the NBD can't find the player
                Debug.LogError("Player from which packet originated does not exist on local NDB.");
                return;
            }

            MeshNetworkIdentity targetObject = ndb.LookupObject(incomingPacket.GetTargetObjectId());
            if(targetObject == null) {
                Debug.LogError("Packet's target object doesn't exist on the database!");
                return;
            }

            targetObject.ReceivePacket(incomingPacket);
            
        }
    }

    
    public void Send(MeshPacket packet) {
        byte[] data = packet.GetSerializedBytes();
        if (packet.GetTargetPlayerId() == (byte)ReservedPlayerIDs.Broadcast) {
            foreach (Player p in ndb.GetAllPlayers()) {
                SteamNetworking.SendP2PPacket(p.GetSteamID(), data, (uint)data.Length, packet.qos);
            }
        }
        else {
            Player target = ndb.LookupPlayer(packet.GetTargetPlayerId());
            SteamNetworking.SendP2PPacket(target.GetSteamID(), data, (uint)data.Length, packet.qos);
        }
        
        
    }

    
    

}

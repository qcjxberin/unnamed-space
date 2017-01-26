using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Utilities;
using Steamworks;

public class MeshEndpoint {

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
    
    

    //Checks for packets from all servers.
    public void Receive() {
        uint bufferLength = 0;
        if (SteamNetworking.IsP2PPacketAvailable(out bufferLength)) {
            byte[] destBuffer = new byte[bufferLength];
            UInt32 bytesRead = 0;
            CSteamID remoteID;
            SteamNetworking.ReadP2PPacket(destBuffer, bufferLength, out bytesRead, out remoteID);
            ParseData(destBuffer);
        }
    }

    
    void ParseData(byte[] data) {

        MeshPacket incomingPacket = new MeshPacket(data);
        if(data[0] == 1){ //Generic game packet, we have to examine this
            byte playerID = data[1];
            Player source = ndb.LookupPlayer(playerID); //retrieve which player sent this packet
            if(source == null) { //hmmm, the NBD can't find the player
                Debug.LogError("Player from which packet originated does not exist on local NDB.");
                return;
            }

            byte typeData = data[2]; //typeByte describes what kind of packet this is
            switch (typeData) {
                case 20: //VOIP packet
                Debug.Log("Found an audio packet");
                byte[] trimmed = new byte[data.Length - 3];
                Buffer.BlockCopy(data, 2, trimmed, 0, trimmed.Length);
                break;

                case 7: //network ping
                GameObject.FindObjectOfType<indicator>().Ping();
                break;

                default:
                Debug.Log("Unknown packet type, header " + typeData);
                break;
            }


            
        }
    }

    
    public void Broadcast(MeshPacket packet) {
        byte[] data = packet.GetSerializedBytes();
        foreach (Player p in ndb.GetAllPlayers()) {       
            SteamNetworking.SendP2PPacket(p.GetSteamID(), data, (uint)data.Length, packet.qos);
        }
    }

    
    

}

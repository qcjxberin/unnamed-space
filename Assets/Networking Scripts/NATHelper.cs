using UnityEngine;
using RakNet;
using System;
using System.Collections;
using Utilities;
/**
 * Uses RakNet to perform NAT Punchthrough using an externally hosted Facilitator.
 * Once a hole is punched a connection can be made via UNet HLAPI.
 * Requires RakNet.dll and c# wrappers. Also requires the NATCompleteServer from 
 * the RakNet samples to be running on an external server somewhere.
 */
public class NATHelper : MonoBehaviour
{
    // IP Addres and port of the server running NATCompleteServer
    public string facilitatorIP = "50.57.111.104"; 
    public ushort facilitatorPort = 61111;

    [NonSerialized]
    public string externalIP;
    [NonSerialized]
    public bool isReady;
    [NonSerialized]
    public string guid;

    NatPunchthroughClient natPunchthroughClient;
    SystemAddress facilitatorSystemAddress;
    bool firstTimeConnect = true;
    //Action<int, string> onHolePunched;
    RakPeerInterface rakPeer;

    public NATStatus mode = NATStatus.Uninitialized;
    

    //public string testAddress;

    void Start()
    {
        
        //natPunchthroughClient = null;
        //firstTimeConnect = true;

        Debug.Log("NATHelper Starting Up");
        rakPeer = RakPeerInterface.GetInstance();

        StartCoroutine(connectToNATFacilitator());
    }

    public NATStatus GetMode() {
        return mode;
    }

    /**
     * Connect to the externally hosted NAT Facilitator (NATCompleteServer from the RakNet samples)
     * This is called initially and also after each succesfull punchthrough received on the server.
     */
    IEnumerator connectToNATFacilitator()
    {
        mode = NATStatus.ConnectingToFacilitator;
        Debug.Log("Beginning connectToNATFacilitator(), waiting for all connections to disconnect");
        //yield return new WaitForSeconds(5);
        //rakPeer.Shutdown(0);
        // Start the RakNet interface listening on a random port
        // We never need more than 2 connections, one to the Facilitator and one to either the server or the latest incoming client
        // Each time a client connects on the server RakNet is shut down and restarted with two fresh connections
        StartupResult startResult = rakPeer.Startup(2, new SocketDescriptor(), 1);
        if (startResult != StartupResult.RAKNET_STARTED)
        {
            Debug.Log("Failed to initialize network interface: " + startResult.ToString());
            yield break;
        }
        
        

        // Connecting, wait for response
        Packet packet;
        while (true) {
            // Connect to the Facilitator
            ConnectionAttemptResult connectResult = rakPeer.Connect(facilitatorIP, (ushort)facilitatorPort, null, 0);
            if (connectResult != ConnectionAttemptResult.CONNECTION_ATTEMPT_STARTED) {
                Debug.Log("Failed to initialize connection to NAT Facilitator: " + connectResult.ToString());
                yield break;
            }

            while ((packet = rakPeer.Receive()) == null) yield return new WaitForEndOfFrame();

            // Was the connection accepted?
            if (!(packet.data[0] == (byte)DefaultMessageIDTypes.ID_CONNECTION_REQUEST_ACCEPTED)) {
                if (packet.data[0] == (byte)DefaultMessageIDTypes.ID_ALREADY_CONNECTED) {
                    Debug.Log("Says it's already connected. Trying again in 0.5s.");
                    
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }
                Debug.Log("Failed to connect to NAT Facilitator: " + ((DefaultMessageIDTypes)packet.data[0]));
                yield break;
            }
            else {
                //we have connected!
                break;
            }
        }

        // Success, we are connected to the Facilitator
        guid = rakPeer.GetMyGUID().g.ToString();
        Debug.Log("Connected: " + guid);

        // We store this for later so that we can tell which incoming messages are coming from the facilitator
        facilitatorSystemAddress = packet.systemAddress;
        Debug.Log("Facilitator System Address = " + facilitatorSystemAddress);
        // Now that we have an external connection we can get the externalIP
        externalIP = rakPeer.GetExternalID(packet.systemAddress).ToString(false);
        Debug.Log("After connecting to Facilitator, externalIP = " + externalIP);

        if (firstTimeConnect)
        {
            firstTimeConnect = false;

            // Attach RakNet punchthrough client
            // This is really what does all the heavy lifting
            natPunchthroughClient = new NatPunchthroughClient();
            rakPeer.AttachPlugin(natPunchthroughClient);
            // Punchthrough can't happen until RakNet is done finding router port stride
            // so we start it asap. If we didn't call this here RakNet would handle it
            // when we actually try and punch through.
            natPunchthroughClient.FindRouterPortStride(facilitatorSystemAddress);
        }
        else
        {
            // If this is not the first time connecting to the facilitor it means the server just received
            // a successful punchthrough and it reconnecting to prepare for more punching. We can start
            // listening immediately.
            //StartCoroutine(waitForIncomingNATPunchThroughOnServer(onHolePunched));
        }
        
        isReady = true;
        mode = NATStatus.Idle;
    }
    
    /**
     * Called on the server to start listening for clients trying to punch through
     */
    public void startListeningForPunchthrough(Action<int, string> onHolePunched)
    {
        mode = NATStatus.Listening;
        //this.onHolePunched = onHolePunched;
        rakPeer.SetMaximumIncomingConnections(2);
        StartCoroutine(waitForIncomingNATPunchThroughOnServer(onHolePunched));
    }

    /**
     * Wait for an incoming punchthrough from a client. Once a hole is punched
     * RakNet is shut down so that the NetworkManager can create a new UNet Server
     * listening on the port that the puncthrough arrived on.
     */
    IEnumerator waitForIncomingNATPunchThroughOnServer(Action<int,  string> onHolePunched)
    {
        Debug.Log("waitForIncomingNATPunchThroughOnServer");
        ushort natListenPort = 0;
        while (true)
        {
            yield return new WaitForEndOfFrame();

            // Check for incoming packet
            Packet packet = rakPeer.Receive();

            // No packet, maybe next time
            if (packet == null) continue;

            // Got a packet, see what it is
            RakNet.DefaultMessageIDTypes messageType = (DefaultMessageIDTypes)packet.data[0];
            switch (messageType)
            {
                case DefaultMessageIDTypes.ID_NAT_PUNCHTHROUGH_SUCCEEDED:
                    bool weAreTheSender = packet.data[1] == 1;
                    if (!weAreTheSender)
                    {
                        // Someone successfully punched through to us
                        // natListenPort is the port that the server should listen on
                        natListenPort = rakPeer.GetInternalID().GetPort();
                        // Now we're waiting for the client to try and connect to us
                        Debug.Log("Received punch through from " + rakPeer.GetMyGUID() + " " + packet.systemAddress.ToString());
                        
                    }
                    break;

                case DefaultMessageIDTypes.ID_NEW_INCOMING_CONNECTION:
                    // Cool we've got a connection. The client now knows which port to connect from / to
                    Debug.Log("Received incoming RakNet connection.");
                    // Close the connection to the client
                    rakPeer.CloseConnection(packet.guid, true);
                    // And also to the Facilitator
                    rakPeer.CloseConnection(facilitatorSystemAddress, true);
                    break;

                case DefaultMessageIDTypes.ID_DISCONNECTION_NOTIFICATION:
                    // Once the Facilitator has disconnected we shut down raknet
                    // so that UNet can listen on the port that RakNet is currently listening on
                    // At this point the hole is succesfully punched
                    // RakNet is then reconnected to the facilitator on a new random port.
                    if (packet.systemAddress == facilitatorSystemAddress)
                    {
                        rakPeer.Shutdown(0);

                        // Hole is punched, UNet can start listening
                        //isReady = false;
                        onHolePunched(natListenPort, externalIP);
                        //Debug.Log("waitForIncomingNATPunchThroughOnServer is reconnecting to facilitator");
                        // Reconnect to Facilitator for next punchthrough
                        //StartCoroutine(connectToNATFacilitator());
                        
                        yield break; // Totally done
                    }
                    break;

                default:
                    Debug.Log(((DefaultMessageIDTypes)packet.data[0]).ToString());
                    break;
            }
        }
    }

    /**
     * Punch a hole form a client to the server identified by hostGUID
     * Once the hole is punched onHolePunched will be called with the ports to use to connect
     */
    public void punchThroughToServer(
        float timeout,
        Action<int, ushort, OutboundPunchContainer> onHolePunched, 
        Action<OutboundPunchContainer> onPunchFail,
        OutboundPunchContainer pc) {

        mode = NATStatus.Punching;
        RakNetGUID hostGUID = new RakNetGUID(ulong.Parse(pc.serverGUID));
        natPunchthroughClient.OpenNAT(hostGUID, facilitatorSystemAddress);
        StartCoroutine(waitForResponseFromServer(timeout, onHolePunched, onPunchFail, pc));
    }

    /**
     * Wait for a NAT Punchthrough responses from the server. Once a hole is punched
     * RakNet is shutdown so the NetworkManager can connect via UNet
     */
    IEnumerator waitForResponseFromServer(float timeout,
        Action<int, ushort, OutboundPunchContainer> onHolePunched, 
        Action<OutboundPunchContainer> onPunchFail,
        OutboundPunchContainer pc) {

        float startTime = Time.time;

        ushort natListenPort = 0, natConnectPort = 0;
        while (true)
        {
            if(Time.time - startTime > timeout) {
                //mode = NATStatus.Idle;
                
                onPunchFail(pc);
                yield break;
            }   
                
            //Debug.Log("About to get packet");
            Packet packet = rakPeer.Receive();
            
            if (packet != null)
            {
                Debug.Log("Found a packet.");
                DefaultMessageIDTypes messageType = (DefaultMessageIDTypes)packet.data[0];

                switch (messageType)
                {
                    case DefaultMessageIDTypes.ID_NAT_PUNCHTHROUGH_SUCCEEDED:
                        // A hole has been punched but we're not done yet. We need to actually connect (via RakNet)
                        // to the server so that we can get the port to connect from
                        // natConnectPort is the external port of the server to connect to
                        natConnectPort = packet.systemAddress.GetPort();
                        rakPeer.Connect(packet.systemAddress.ToString(false), natConnectPort, "", 0);
                        Debug.Log("Hole punched! Port: " + packet.systemAddress.GetPort());
                        break;

                    case DefaultMessageIDTypes.ID_CONNECTION_REQUEST_ACCEPTED:
                        // Ok, we connected, we can now use GetExternalID to get the port to connect from.
                        natListenPort = rakPeer.GetExternalID(packet.systemAddress).GetPort();
                        Debug.Log("RakNet Connected (ID_CONNECTION_REQUEST_ACCEPTED), waiting for remote disconnect");
                        // Now we wait for the server to disconnect us so that we know it is ready for the UNet connection
                        break;

                    case DefaultMessageIDTypes.ID_DISCONNECTION_NOTIFICATION:
                        Debug.Log("Recieved remote disconnect, shutting down RakNet and performing callback");
                        // Server has disconnected us. We are ready to connect via UNet

                        // Shut down RakNet
                        rakPeer.Shutdown(0);
                        //rakPeer.CloseConnection(packet.guid, false);
                        //isReady = false;
                        // Hole is punched!
                        
                        //mode = NATStatus.AfterPunching;
                        onHolePunched(natListenPort, natConnectPort, pc);
                        Debug.Log("WaitForResponseFromServer has finished performing callback.");
                        yield break; // Totally done

                    default:
                        Debug.Log("RakNet Client received unexpected message type: " + ((DefaultMessageIDTypes)packet.data[0]).ToString()
                            + "but we'll keep trying.");
                        punchThroughToServer(timeout - (Time.time - startTime), onHolePunched, onPunchFail, pc);
                        yield break;
                }
            }
            rakPeer.DeallocatePacket(packet);
            yield return new WaitForEndOfFrame();
        }
    }

    public void RebootNAT() { //Returns NAT to idle.
        NATStatus previousMode = mode;
        mode = NATStatus.Rebooting;
        Debug.Log("<<< REBOOT NAT >>>, current status = " + mode);
        Debug.Log("Previous mode " + previousMode);
        
        //return;
        switch (previousMode) {
            case NATStatus.Uninitialized:
            StopAllCoroutines();
            if(rakPeer != null)
                rakPeer.Shutdown(0);
            StartCoroutine(connectToNATFacilitator());
            return;

            case NATStatus.Idle: //already connected to facilitator, but nothing else is going on
            mode = previousMode; //why did you even reboot?
            break;

            case NATStatus.ConnectingToFacilitator: //let it do its thing
            mode = previousMode;
            break;

            case NATStatus.Listening:
            StopAllCoroutines();
            rakPeer.Shutdown(0);
            StartCoroutine(connectToNATFacilitator());
            return;

            case NATStatus.Punching:
            StopAllCoroutines();
            rakPeer.Shutdown(0);
            StartCoroutine(connectToNATFacilitator());
            return;

            default:                //nuke this entire site from orbit
            StopAllCoroutines();    //it's the only way to be sure
            rakPeer.Shutdown(0);
            StartCoroutine(connectToNATFacilitator());
            return;

        }

        //mode = previousMode; //restore previous status

        //rakPeer.Shutdown(0);
        //StartCoroutine(connectToNATFacilitator());
    }

    
    public void PrepNAT(NATStatus newMode) {
        
        Debug.Log("Prepping NAT for " + newMode);
        Debug.Log("Previous mode " + mode);
        

        if(newMode == mode) {
            return;
        }
        else {
            RebootNAT();
        }
        
    }
    
    /*
    IEnumerator PurgeAndReconnect() {
        //rakPeer.CloseConnection(facilitatorSystemAddress, true);
        float start = Time.time;
        ushort natListenPort = 0;
        while (true) {
            
            
            yield return new WaitForEndOfFrame();
            if(Time.time - start > 5) {
                Debug.Log("PurgeAndReconnect timed out, reconnecting to Facilitator");
                rakPeer.Shutdown(0);
                StartCoroutine(connectToNATFacilitator());
                yield break;
            }
            // Check for incoming packet
            Packet packet = rakPeer.Receive();

            // No packet, maybe next time
            if (packet == null) continue;

            // Got a packet, see what it is
            RakNet.DefaultMessageIDTypes messageType = (DefaultMessageIDTypes)packet.data[0];
            switch (messageType) {
                case DefaultMessageIDTypes.ID_DISCONNECTION_NOTIFICATION:
                if (packet.systemAddress == facilitatorSystemAddress) {
                    Debug.Log("PurgeAndDisconnect has detected a facilitator disconnect");
                    rakPeer.Shutdown(0);

                    StartCoroutine(connectToNATFacilitator());
                    yield break; // Totally done
                }
                break;
            }
        }
    }
    */
    void OnApplicationQuit()
    {
        rakPeer.Shutdown(0);
    }
}

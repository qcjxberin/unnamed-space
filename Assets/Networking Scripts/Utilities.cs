﻿using UnityEngine;
using System.Collections;

public class PeerInfo {

    public int connectionId;
    
    public PeerInfo(int id) {
        connectionId = id;
    }
}


namespace Utilities {
    public enum NATStatus { Uninitialized, Idle, ConnectingToFacilitator, Listening, Punching };
}


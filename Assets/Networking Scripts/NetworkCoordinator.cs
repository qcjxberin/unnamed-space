using UnityEngine;
using System.Collections;

public class NetworkCoordinator : MonoBehaviour {

    NATHelper nath;
    CarrierPigeon pigeon;

	// Use this for initialization
	void Start () {
        nath = gameObject.GetComponent<NATHelper>();
        pigeon = gameObject.AddComponent<CarrierPigeon>();
	}
	
	public void CoordinatorHostGame() {
        if(pigeon.ready && nath.isReady)
            pigeon.HostGame(nath.guid, nath.externalIP);
    }
    public void CoordinatorJoinGame() {
        if (pigeon.ready && nath.isReady)
            pigeon.JoinGame(OnInfoAcquired);
    }
    public void OnInfoAcquired(string serverExternalIP, string serverInternalIP, string serverGUID) {
        nath.punchThroughToServer(serverGUID, )
    }
    public void EnterPunchingMode() {
        nath.StopAllCoroutines();
    }
}

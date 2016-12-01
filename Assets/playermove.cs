using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class playermove : NetworkBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        if (GetComponent<NetworkIdentity>().clientAuthorityOwner != connectionToClient) {
            return;
        }
        var x = Input.GetAxis("Horizontal") * 0.1f;
        var z = Input.GetAxis("Vertical") * 0.1f;

        transform.Translate(x, 0, z);

    }
}

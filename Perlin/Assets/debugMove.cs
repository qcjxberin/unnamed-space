using UnityEngine;
using System.Collections;
using Valve.VR;
public class debugMove : MonoBehaviour {
    public GameObject inputObject;
    SteamVR_TrackedObject controllerObject;
    SteamVR_Controller.Device controller { get { return SteamVR_Controller.Input((int)controllerObject.index); } }
    private EVRButtonId trigger = EVRButtonId.k_EButton_SteamVR_Trigger;

    public Transform playSpace;



    // Use this for initialization
    void Start () {
        controllerObject = inputObject.GetComponent<SteamVR_TrackedObject>();
    }
	
	// Update is called once per frame
	void Update () {
        playSpace.Translate(transform.forward * (controller.GetAxis(trigger).x / 2));
	}
}

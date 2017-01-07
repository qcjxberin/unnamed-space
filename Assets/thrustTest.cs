using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class thrustTest : MonoBehaviour {
    public Rigidbody ship;
    public GameObject inputObject;
    SteamVR_TrackedObject controllerObject;
    SteamVR_Controller.Device controller { get { return SteamVR_Controller.Input((int)controllerObject.index); } }
    private EVRButtonId pad = EVRButtonId.k_EButton_SteamVR_Touchpad;
    private EVRButtonId trigger = EVRButtonId.k_EButton_SteamVR_Trigger;

    Vector3 localZero;
    // Use this for initialization
    void Start() {
        controllerObject = inputObject.GetComponent<SteamVR_TrackedObject>();
    }
    void Update() {
        if (controller.GetPressUp(pad)) {
            localZero = inputObject.transform.localPosition;
        }
    }
    // Update is called once per frame
    void FixedUpdate () {
		if(controller.GetAxis(trigger).x > 0.05f) {
            ship.AddForce(ship.transform.TransformVector(inputObject.transform.localPosition - localZero) * 30 * controller.GetAxis(trigger).x);
        }
	}
}

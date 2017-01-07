using UnityEngine;
using System.Collections;

public class JoystickIK : MonoBehaviour {

    public Transform Arm1, Arm1end;
    public Transform Arm2;
    public Transform hand;
    public Transform CameraRig;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        float handDistance = Vector3.Distance(transform.localPosition, hand.localPosition + CameraRig.localPosition);
        if (handDistance > 1.2f)
            return;
        float angle = Mathf.Acos((handDistance * 0.5f) / 0.6f);
       
        Arm1.localRotation = Quaternion.Euler(new Vector3(Mathf.Rad2Deg * angle, 0, 0));
        Arm2.localPosition = Arm1end.localPosition;
        Arm2.localRotation = Quaternion.Euler(new Vector3(Mathf.Rad2Deg * -angle, 0, 0));
        transform.localRotation = Quaternion.LookRotation(hand.localPosition + CameraRig.localPosition - transform.localPosition);
	}
}

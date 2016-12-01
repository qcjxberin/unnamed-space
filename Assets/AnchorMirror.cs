using UnityEngine;
using System.Collections;

public class AnchorMirror : MonoBehaviour {
    public Transform target;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        transform.localPosition = target.localPosition;
	}
}

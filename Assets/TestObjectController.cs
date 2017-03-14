using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestObjectController : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void OnEnable () {
        transform.position = new Vector3(0, 4, 0);
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class indicator : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
    public void Ping() {
        StartCoroutine(flash());
    }

    IEnumerator flash()
    {
        GetComponent<MeshRenderer>().material.color = Color.green;
        yield return new WaitForSeconds(0.5f);
        GetComponent<MeshRenderer>().material.color = Color.white;
        yield break;
    }
}

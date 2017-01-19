using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VoipStatusLight : MonoBehaviour {
    public AudioSource v;
    Image i;
	// Use this for initialization
	void Start () {
        i = gameObject.GetComponent<Image>();
	}
	
	// Update is called once per frame
	void Update () {
        if (v.isPlaying) {
            i.color = Color.green;
        }
        else {
            i.color = Color.red;
        }
	}
}

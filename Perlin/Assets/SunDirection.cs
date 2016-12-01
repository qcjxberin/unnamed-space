using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class SunDirection : MonoBehaviour {
    public Transform sunTransform;
    Material mat;
	// Use this for initialization
	void Start () {
        mat = GetComponent<MeshRenderer>().sharedMaterial;
	}
	
	// Update is called once per frame
	void Update () {
        mat.SetVector("_LightDir", sunTransform.forward);
	}
}

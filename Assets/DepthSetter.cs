using UnityEngine;
using System.Collections;
[ExecuteInEditMode]
public class DepthSetter : MonoBehaviour {

	void Awake() {
        GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;
    }
}

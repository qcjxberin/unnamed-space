using UnityEngine;
using System.Collections;
[ExecuteInEditMode]
public class AtmosphereFog : MonoBehaviour {
    public Material mat;
    public RenderTexture AtmosphereTexture;
    void Awake() {
        GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;
    }
	// Use this for initialization
	void OnRenderImage(RenderTexture source, RenderTexture destination) {
       
        Graphics.Blit(source, destination, mat);
    }
}

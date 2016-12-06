using UnityEngine;
using System.Collections;
[ExecuteInEditMode]
public class ImageEffectApplier : MonoBehaviour {
    public Material mat;
    void Awake() {
        GetComponent<Camera>().depthTextureMode = DepthTextureMode.DepthNormals;
    }
	// Use this for initialization
	void OnRenderImage(RenderTexture source, RenderTexture destination) {
        Graphics.Blit(source, destination, mat);
    }
}

using UnityEngine;
using System.Collections;
[ExecuteInEditMode]
public class ImageEffectApplier : MonoBehaviour {
    public Material mat;
	// Use this for initialization
	void OnRenderImage(RenderTexture source, RenderTexture destination) {
        Graphics.Blit(source, destination, mat);
    }
}

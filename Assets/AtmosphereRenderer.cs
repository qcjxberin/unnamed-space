using UnityEngine;
using System.Collections;
[ExecuteInEditMode]
public class AtmosphereRenderer : MonoBehaviour {
    public AtmosphereFog af;
    public RenderTexture test;
    public Material testMaterial;
    void OnRenderImage(RenderTexture src, RenderTexture dest) {
        //Debug.Log("hi");
        //af.AtmosphereTexture = src;
        testMaterial.mainTexture = src;
        Graphics.Blit(src, af.AtmosphereTexture);
    }
}
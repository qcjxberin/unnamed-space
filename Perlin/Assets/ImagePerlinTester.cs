using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class ImagePerlinTester : MonoBehaviour {
    public RawImage img;
    Texture2D tex;
    Perlin perl;
    void Start() {
        tex = new Texture2D(1024, 1024);
        perl = new Perlin(); //no repeat yet

        for(int x = 0; x < tex.width; x++) {
            for(int y = 0; y < tex.height; y++) {
                double value = perl.OctavePerlin(x * 0.01d, y * 0.01d, 0.01d, 3, 0.5d);
                Color c = new Color((float)value, (float)value, (float)value);
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        img.texture = tex;


        Debug.Log(perl.perlin(0.1d, 0.1d, 0.1d));
        Debug.Log(perl.perlin(0.2d, 0.2d, 0.2d));
        Debug.Log(perl.perlin(1000.2d, 1500.12d, 0.1d));
    }
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
[ExecuteInEditMode]
public class Daylight : MonoBehaviour {
    Light thislight;
    public Color sunsetColor;
    public Color daylightColor;
    float[] buffer = new float[1000];
    // Use this for initialization
    void Awake() {
        thislight = GetComponent<Light>();
    }

    // Update is called once per frame
    void Update() {
        if (thislight == null) {
            Awake();
        }
        thislight.color = Color.Lerp(daylightColor, sunsetColor, 1 - Mathf.Pow(Vector3.Dot(transform.forward, Vector3.down), 1f));
        //thislight.intensity = Mathf.Pow(Vector3.Dot(transform.forward, Vector3.down), 0.5f);
        float i = Mathf.Clamp01(Mathf.Pow((Vector3.Angle(transform.forward, Vector3.up)/90)-1, 0.5f) * 1.5f);
        if (float.IsNaN(i)) {
            thislight.intensity = 0;
        }
        else {
            thislight.intensity = i;

        }
    }

    
    
}

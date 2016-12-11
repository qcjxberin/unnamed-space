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
        thislight.color = Color.Lerp(sunsetColor, daylightColor, Mathf.Pow(Vector3.Dot(transform.forward, Vector3.down), 5f));
        //thislight.intensity = Mathf.Pow(Vector3.Dot(transform.forward, Vector3.down), 0.5f);
        float i = Mathf.Pow((Vector3.Angle(transform.forward, Vector3.up)/180 - 0.4f) *2, 0.5f);
        if (float.IsNaN(i)) {
            thislight.intensity = 0;
        }
        else {
            thislight.intensity = i;

        }
    }

    
    
}

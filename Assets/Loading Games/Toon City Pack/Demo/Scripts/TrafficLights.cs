using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LightColors { Red, Yellow, Green, None }
public class TrafficLights : MonoBehaviour {

    public LightColors activeLight;
    private MeshRenderer mr;
    private Shader defShader, unlitShader;

    private void Start() {
        mr = GetComponent<MeshRenderer>();
        defShader = Shader.Find("Standard");
        unlitShader = Shader.Find("Unlit/Color");
        SetLight(activeLight);
    }

    public void SetLight(LightColors color) {
        // mat 1 : green, mat 2 : yellow, mat 3 : red
        int activeIndex = 0;
        switch (color) {
            case LightColors.Green:
                activeIndex = 1;
                break;

            case LightColors.Yellow:
                activeIndex = 2;
                break;

            case LightColors.Red:
                activeIndex = 3;
                break;
        }

        for(int i = 1; i < 4; i++) {
            mr.materials[i].shader = activeIndex == i ? unlitShader : defShader;
        }
    }

}

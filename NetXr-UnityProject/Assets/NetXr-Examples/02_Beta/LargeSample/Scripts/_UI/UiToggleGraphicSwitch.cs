//============= Copyright (c) Unknown ==============
//
// Purpose: 
//
//=============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class UiToggleGraphicSwitch : MonoBehaviour {
    public Graphic graphicOn;
    public Graphic graphicOff;

    void Start() {
        GetComponent<Toggle>().onValueChanged.AddListener((value) => {
            OnValueChanged(value);
        });
        OnValueChanged(GetComponent<Toggle>().isOn);
    }

    void OnValueChanged(bool value) {
        if (graphicOn != null) {
            graphicOn.enabled = value;
        }
        if (graphicOff != null) {
            graphicOff.enabled = !value;
        }
    }
}


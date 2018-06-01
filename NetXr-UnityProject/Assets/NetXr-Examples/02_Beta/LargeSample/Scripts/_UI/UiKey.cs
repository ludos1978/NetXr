//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiKey : MonoBehaviour {
    public string key;
    [TextArea(3, 10)]
    public string label;
    bool isCapital = false;
    Button button;
    Text buttonText;
    UiKeyboard keyboard;

    public void Awake () {
        keyboard = GetComponentInParent<UiKeyboard>();
        button = GetComponent<Button>();
        button.onClick.AddListener(SendKey);
        buttonText = GetComponentInChildren<Text>();
    }

    public void SendKey () {
        if (isCapital)
            keyboard.AddChar(key.ToUpper());
        else
            keyboard.AddChar(key);
    }

    public void SetCapital (bool state) {
        isCapital = state;
        if (label.Length > 0) {
            buttonText.text = label;
        } else { 
            if (isCapital)
                buttonText.text = key.ToUpper();
            else
                buttonText.text = key;
        }
    }
}

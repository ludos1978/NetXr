//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UiKeyboard : MonoBehaviour {
    public InputField inputfield;
    UiKey[] uiKeys;
    bool prevCapital = false;
    private string text;

    public class UnityStringEvent : UnityEvent<string> { }
    public UnityStringEvent textChangeEvent = new UnityStringEvent();
    public UnityStringEvent keyboardEnterEvent = new UnityStringEvent();

    public void Awake () {
        uiKeys = GetComponentsInChildren<UiKey>();
        Debug.Log("UiKeyboard.Awake: found " + uiKeys.Length + " keys");
    }

    public void AddChar (string keyPress) {
        Debug.Log("UiKeyboard.AddChar: char: " + keyPress);
        switch (keyPress.ToLower()) {
            case "return":
                keyboardEnterEvent.Invoke(text);
                //inputfield.text = "";
                break;
            case "backspace":
                text = text.Remove(text.Length - 1);
                textChangeEvent.Invoke(text);
                break;
            case "space":
                text += " ";
                textChangeEvent.Invoke(text);
                break;
            case "shift":
                prevCapital = !prevCapital;
                SetCapital(prevCapital);
                break;
            default:
                text += keyPress;
                textChangeEvent.Invoke(text);
                // reset capital after a key input
                if (prevCapital) {
                    prevCapital = false;
                    SetCapital(false);
                }
                break;
        }
        Debug.Log("UiKeyboard.AddChar: text: " + text);
    }

    public void SetCapital (bool state) {
        foreach (UiKey uiKey in uiKeys) {
            uiKey.SetCapital(state);
        }
    }
}

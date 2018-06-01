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

public class SymbolKeyboard : MonoBehaviour {
    public class UnityIntegerEvent : UnityEvent<int> { }
    public UnityIntegerEvent symbolChangeEvent = new UnityIntegerEvent();

    public void Awake () {
    }

    public void SymbolPressed (int symbolValue) {
        symbolChangeEvent.Invoke(symbolValue);
    }

    public void DestroyPressed () {
        symbolChangeEvent.Invoke(-1);
    }
}

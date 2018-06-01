//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(UiKeyboardLabeling))]
public class UiKeyboardLabelingInspector : Editor {
    public override void OnInspectorGUI() {
        UiKeyboardLabeling myTarget = (UiKeyboardLabeling)target;

        if (GUILayout.Button("Update Keys")) {
            UpdateKeys(myTarget);
        }

        DrawDefaultInspector();
    }

    public void UpdateKeys(UiKeyboardLabeling myTarget) {
        if (myTarget.keyTemplate == null) {
            Debug.LogError("UiKeyboardLabelingInspector.UpdateKeys: no key template defined");
        }

        // destroy all childs except keytemplate
        for (int i=myTarget.transform.childCount-1; i>=0; i--) {
            Transform child = myTarget.transform.GetChild(i);
            if (child.gameObject == myTarget.keyTemplate) {
                continue;
            } else { 
                DestroyImmediate(child.gameObject);
            }
        }

        HandleRow(myTarget, myTarget.keylabelsRow0);
        HandleRow(myTarget, myTarget.keylabelsRow1);
        HandleRow(myTarget, myTarget.keylabelsRow2);
        HandleRow(myTarget, myTarget.keylabelsRow3);
    }

    public void HandleRow (UiKeyboardLabeling myTarget, char[] keys) {
        foreach (char key in keys) { 
            GameObject keyGo = Instantiate(myTarget.keyTemplate) as GameObject;
            keyGo.name = key.ToString();
            keyGo.GetComponentInChildren<Text>().text = key.ToString();
            keyGo.SetActive(true);
            keyGo.transform.SetParent(myTarget.transform, false);
            keyGo.transform.localScale = Vector3.one;
            keyGo.GetComponent<UiKey>().key = key.ToString();
        }
    }
}
#endif


[ExecuteInEditMode]
public class UiKeyboardLabeling : MonoBehaviour {
    public GameObject keyTemplate;

    public char[] keylabelsRow0;
    public char[] keylabelsRow1;
    public char[] keylabelsRow2;
    public char[] keylabelsRow3;
}

//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(DestroyOnAwake))]
public class DestroyOnAwakeInspector : Editor {
    public override void OnInspectorGUI() {
        DestroyOnAwake script = (DestroyOnAwake)target;
        EditorGUILayout.TextArea("This Script destroys this gameobject and all childrens on run");
    }
}
#endif

public class DestroyOnAwake : MonoBehaviour {
    void Awake() {
        Destroy(gameObject);
    }
}
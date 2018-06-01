//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

namespace NetXr {

    using System;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using UnityEditorInternal;

    public class NetXrSettings : EditorWindow {


        bool leapAvailable;
        bool steamVrAvailable;

        [MenuItem ("Window/NetXrSettings")]
        public static void ShowWindow () {
            EditorWindow.GetWindow (typeof (NetXrSettings));
        }

        public void OnGUI () {
            if (GUILayout.Button ("Read Values from System")) {
                ReadFromSystem ();
            }

            leapAvailable = EditorGUILayout.Toggle ("Leap Lib Available", leapAvailable);
            steamVrAvailable = EditorGUILayout.Toggle ("SteamVR Lib Available", steamVrAvailable);

            if (GUILayout.Button ("Update Precompile Symbols")) {
                UpdatePrecompileSymbols ();
            }
        }

        private void ReadFromSystem () {
            leapAvailable = NamespaceExists ("Leap");
#if UNITY_2017_1_OR_NEWER
            steamVrAvailable = true;
#else
            steamVrAvailable = ClassExists ("SteamVR_Controller");
#endif
        }

        private void UpdatePrecompileSymbols () {
            DefinePrecompileSymbol ("NETXR_LEAP_ACTIVE", leapAvailable);
            DefinePrecompileSymbol ("NETXR_STEAMVR_ACTIVE", steamVrAvailable);
        }

        // method3
        private bool ClassExists (string className) {
            Type t = System.Reflection.Assembly.GetExecutingAssembly ().GetType (className, false);
            return (t != null);
        }

        private bool NamespaceExists (string desiredNamespace) {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies ()) {
                foreach (Type type in assembly.GetTypes ()) {
                    if (type.Namespace == desiredNamespace)
                        return true;
                }
            }
            return false;
        }

        private void DefinePrecompileSymbol (string newSymbol, bool newState) {
            //bool currentActive = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone).Contains(newSymbol);
            List<String> allCurrentSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup (BuildTargetGroup.Standalone).Split (';').ToList ();
            if (allCurrentSymbols.Contains (newSymbol) != newState) {
                if (newState) {
                    allCurrentSymbols.Add (newSymbol);
                    // add the symbol
                    string newSymbols = string.Join (";", allCurrentSymbols.ToArray ());
                    Debug.LogWarning ("WorldspaceInputModuleInspector.DefineCompileSymbol: adding " + newSymbol + " to compile preprocessors -> " + newSymbols);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup (BuildTargetGroup.Standalone, newSymbols);
                } else {
                    allCurrentSymbols.Remove (newSymbol);
                    // remove the symbol
                    string newSymbols = string.Join (";", allCurrentSymbols.ToArray ());
                    Debug.LogWarning ("WorldspaceInputModuleInspector.DefineCompileSymbol: removing " + newSymbol + " from compile preprocessors -> " + newSymbols);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup (BuildTargetGroup.Standalone, newSymbols);
                }
            }
        }
    }

}
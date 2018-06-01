//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace NetXr {
    #if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(NetworkInteractableObject))]
    public class NetworkInteractableObjectInspector : InteractableObjectInspector {

        public override void OnInspectorGUI() {
            NetworkInteractableObject myTarget = (NetworkInteractableObject)target;
            this.serializedObject.Update();

            // draw all fields commonly used
            DrawDefaultFields(myTarget);

            // show the standard inspector for debugging
            showDefault = EditorGUILayout.Foldout(showDefault, "Default Inspector (Debugging)");
            if (showDefault) {
                DrawDefaultInspector();
            }

            this.serializedObject.ApplyModifiedProperties();
        }
    }
    #endif

    public class NetworkInteractableObject : InteractableObject {
        [Header("local client authority assigning")]
        public UnityEvent onStartAuthorityCallback;
        public UnityEvent onStopAuthorityCallback;
        [Header("client start callbacks")]
        public UnityEvent onStartClientCallback;
        public UnityEvent onStartServerCallback;

        protected override void Awake() {
            base.Awake();
        }

        public override void OnStartClient() {
            base.OnStartClient();
            onStartClientCallback.Invoke();
        }

        public override void OnStartServer() {
            base.OnStartServer();
            onStartServerCallback.Invoke();
        }

        public override void OnStartAuthority() {
            base.OnStartAuthority();
            onStartAuthorityCallback.Invoke();
            //Debug.Log ("InteractableObject.OnStartAuthority");
        }

        public override void OnStopAuthority() {
            base.OnStopAuthority();
            onStopAuthorityCallback.Invoke();
            Debug.Log("InteractableObject.OnStopAuthority: " + this.gameObject.name);
        }

        #region controller funtions
        public override void DoAttachToController(InputDeviceData deviceData)
        {
            Debug.LogWarning("DoAttachTocontroller.GetAuth");
            if (grabOnlyFirst) {
                if (isFirstHovered(deviceData)) {
                    GetAuthority();
                    base.DoAttachToController(deviceData);
                }
                else {
                    // not attaching
                }
            }
            else {
                GetAuthority();
                base.DoAttachToController(deviceData);
            }
        }
        public override void DoDetachFromController(InputDeviceData deviceData) {
            base.DoDetachFromController(deviceData);
            DropAuthority();
        }
        #endregion

        [Client]
        public static bool HasLocalPlayerAuthority(GameObject go) {
            NetworkIdentity networkIdentity = go.GetComponent<NetworkIdentity>();
            if (networkIdentity.isServer) {
                // if this is the server, we may have the authority just because a client has returned the authority of the object
                //playerControllerId ? what is this?
                NetworkConnection currentOwner = networkIdentity.clientAuthorityOwner;
                string _currentOwner = (currentOwner != null) ? currentOwner.address : "null";
                return (_currentOwner == "localClient");
                //NetworkConnection currentOwner = objectNetworkIdentity.clientAuthorityOwner;
            }
            // if this is the client the authority works correctly
            return networkIdentity.hasAuthority;
        }

        [Client]
        public static bool HasServerAuthority(GameObject go) {
            NetworkIdentity networkIdentity = go.GetComponent<NetworkIdentity>();
            if (networkIdentity.isServer) {
                NetworkConnection currentOwner = networkIdentity.clientAuthorityOwner;
                string currentOwnerAddress = (currentOwner != null) ? currentOwner.address : "null";
                Debug.Log("HasServerAuthority: currentOwner " + currentOwner + " currentOwnerAddress " + currentOwnerAddress);
                return currentOwner == null;
            }
            Debug.Log("HasServerAuthority: playerControllerId " + networkIdentity.playerControllerId);
            return !networkIdentity.hasAuthority;
        }

        [Client]
        public void ToggleAuthority () {
            if (NetworkPlayerController.LocalInstance)
                NetworkPlayerController.LocalInstance.CmdToggleAuthority(netId);
            else
                Debug.Log("NetworkInputDevice.GrabAuthority: NetworkPlayerController.LocalInstance undefined!");
        }

        [Client]
        public void GetAuthority() {
            // request authority over player
            if (NetworkPlayerController.LocalInstance)
                NetworkPlayerController.LocalInstance.CmdGetAuthority(netId);
            else
                Debug.Log("NetworkInputDevice.GrabAuthority: NetworkPlayerController.LocalInstance undefined!");
        }

        [Client]
        public void DropAuthority() {
            // request authority over player
            if (NetworkPlayerController.LocalInstance)
                NetworkPlayerController.LocalInstance.CmdDropAuthority(netId);
            else
                Debug.Log("NetworkInputDevice.GrabAuthority: NetworkPlayerController.LocalInstance undefined!");
        }

    }
}
//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using UnityEngine;
using UnityEngine.Networking;

namespace NetXr {
    public class NetworkInputDevice : NetworkBehaviour {

        // set by NetworkPlayerController on creation
        [SyncVar]
        public NetworkInstanceId ownerId;
        [SyncVar]
        public int inputDeviceId = -1;
        [SyncVar]
        public InputDeviceType deviceType;
        [SyncVar]
        public InputDeviceHand deviceHand;

        /// <summary>
        /// is setup the client (when a local device is found)
        /// </summary>
        protected InputDevice inputDevice;

        // changed if the controller is found or lost
        [SyncVar (hook = "OnVisibleStateChanged")]
        public bool visibleStateSync;

        protected new Rigidbody rigidbody;

        // the transform the controller gets it's position from
        protected Transform trackedTransform;

        // list to initialize on start containing all renderers and colliders (so we can enable, disable them without changing our chidrens)
        private Renderer[] controllerRenderers;
        private Collider[] controllerColliders;

        public bool enableColliders = false;

        public virtual void OnEnable () {
            //Debug.Log("NetworkInputDevices.OnEnable: " + name);
            rigidbody = GetComponent<Rigidbody> ();
            controllerRenderers = GetComponentsInChildren<Renderer> ();
            controllerColliders = GetComponentsInChildren<Collider> ();
        }
        public virtual void OnDisable () { }

        public override void OnStartClient() {
            base.OnStartClient();
            Debug.Log("NetworkInputDevice.OnStartClient");
            //NetworkServer.FindLocalObject(ownerId).GetComponent<NetworkPlayerController>().AddedDevice(this);
            ClientScene.FindLocalObject(ownerId).GetComponent<NetworkPlayerController>().AddedDevice(this);
        }

        /// <summary>
        /// When NetworkIdentity.AssignClientAuthority is called on the server, this will be called on the client that owns the object.
        /// When an object is spawned with NetworkServer.SpawnWithClientAuthority, this will be called on the client that owns the object.
        /// </summary>
        [Client]
        public override void OnStartAuthority () {
            //Debug.Log("NetworkInputDevices.OnStartAuthority");
            base.OnStartAuthority ();

            // attach the controller model to the tracked controller object on the local client
            if (hasAuthority) {
                HideController ();

                inputDevice = InputDeviceManager.Instance.GetInputDeviceFromId (inputDeviceId);
                // device found, attach callbacks
                if (inputDevice != null) {
                    inputDevice.onSetDeviceActiveEvent.AddListener (OnLocalDeviceEnableEvent);
                    inputDevice.onSetDeviceInactiveEvent.AddListener (OnLocalDeviceDisableEvent);
                    if (inputDevice.deviceActive) {
                        OnLocalDeviceEnableEvent (inputDevice);
                    } else {
                        OnLocalDeviceDisableEvent (inputDevice);
                    }
                    SetupNetworkControllerCallbacks (inputDevice);
                }
                // device not found error
                else {
                    Debug.LogError ("NetworkInputDevice.OnStartAuthority: input device with id " + inputDeviceId + " not found, maybe it's not local?");
                }
            } else {
                Debug.LogError ("NetworkInputDevices.OnStartAuthority: has no authority? " + isClient + " " + isServer);
            }
        }

        [Client]
        public override void OnStopAuthority () {
            //Debug.Log("NetworkInputDevices.OnStopAuthority");
            if (inputDevice != null) {
                inputDevice.onSetDeviceActiveEvent.RemoveListener (OnLocalDeviceEnableEvent);
                inputDevice.onSetDeviceInactiveEvent.RemoveListener (OnLocalDeviceDisableEvent);
                //inputDevice.inputController.onSetDeviceActiveEvent.RemoveListener(OnLocalDeviceEnableEvent);
                //inputDevice.inputController.onSetDeviceInactiveEvent.RemoveListener(OnLocalDeviceDisableEvent);
            }

            base.OnStopAuthority ();
            ownerId = NetworkInstanceId.Invalid;
            inputDevice = null;
        }

        /// <summary>
        /// is called when the real local controller is found
        /// </summary>
        [Client]
        public void OnLocalDeviceEnableEvent (InputDevice inputDevice) {
            //Debug.Log("NetworkInputDevice.OnLocalDeviceEnableEvent: "+inputDevice.ToString());
            // show controller and enable colliders
            ShowController ();
            // set networked visualisation state
            CmdSetVisibleState (true);
            // add networked objects to tracking
            TryAttachToParent ();
            //
            OnLocalDeviceModifyEvent(inputDevice);
        }

        /// <summary>
        /// is called when the real local controller is lost
        /// </summary>
        [Client]
        public void OnLocalDeviceDisableEvent (InputDevice inputDevice) {
            //Debug.Log("NetworkInputDevice.OnLocalDeviceDisableEvent: "+inputDevice.ToString());
            // hide controller and disable colliders
            HideController ();
            // set networked visualisation state
            CmdSetVisibleState (false);
            // remove networked objects from tracking
            UnsetTrackedTransform ();
            //
            OnLocalDeviceModifyEvent(inputDevice);
        }

        [Client]
        public void OnLocalDeviceModifyEvent(InputDevice inputDevice) {
            CmdDeviceModifyEvent(inputDevice.deviceType, inputDevice.deviceHand);
        }
        [Command]
        public void CmdDeviceModifyEvent(InputDeviceType newDeviceType, InputDeviceHand newDeviceHand) {
            deviceType = newDeviceType;
            deviceHand = newDeviceHand;
        }

        /// <summary>
        /// set the visible state on the server, so it's updated on all clients
        /// </summary>
        /// <param name="newState"></param>
        [Command]
        public void CmdSetVisibleState (bool newState) {
            visibleStateSync = newState;
        }

        /// <summary>
        /// network callback when visibleStateSync is changed
        /// </summary>
        [Client]
        public void OnVisibleStateChanged (bool newState) {
            //Debug.Log("NetworkInputDevice.OnVisibleStateChanged: " + newState);
            // apply value (might make loop?)
            visibleStateSync = newState;

            if (newState) {
                ShowController ();
            } else {
                HideController ();
            }
        }

        /// <summary>
        /// move the rigidbody to the position of the tracked transform
        /// </summary>
        [Client]
        public virtual void LateUpdate () {
            if ((inputDevice != null) && (trackedTransform != null)) {
                //if (IsTracked() && hasAuthority) {
                if (rigidbody) {
                    //rigidbody.position = trackedTransform.position;
                    rigidbody.MovePosition (trackedTransform.position);
                    //rigidbody.rotation = trackedTransform.rotation;
                    rigidbody.MoveRotation (trackedTransform.rotation);
                } else {
                    transform.position = trackedTransform.position;
                    transform.rotation = trackedTransform.rotation;
                }
            }
        }

        /// <summary>
        /// function to be overridden by the child class, should call SetTrackedTransform to attach to the parent
        /// </summary>
        [Client]
        protected virtual void TryAttachToParent () {
            Debug.LogError ("NetworkInputDevice.TryAttachToParent: this must be overloaded!");
        }

        /// <summary>
        /// Set which transform this object follows (we dont attach it as child because of networking issues (NetworkTransformChild only works with networkstransforms at root level))
        /// </summary>
        [Client]
        public virtual void SetTrackedTransform (Transform _trackedTransform) {
                trackedTransform = _trackedTransform;
            }
            [Client]
        public virtual void UnsetTrackedTransform () {
            trackedTransform = null;
        }
        public virtual void SetupNetworkControllerCallbacks (InputDevice inputDevice) {
            throw new System.NotImplementedException ("NetworkInputDevice.SetupCallbacks must be overloaded by childrens");
        }

        /// <summary>
        /// Activate the controllers renderer and colliders
        /// </summary>
        protected virtual void ShowController () {
            if ((controllerRenderers == null) || (controllerColliders == null)) {
                Debug.LogError ("NetworkInputDevice.ShowController: setup missing?: " + controllerRenderers + " " + controllerColliders);
                return;
            }
            foreach (Renderer r in controllerRenderers) { //GetComponentsInChildren<Renderer>()) {
                r.enabled = true;
            }
            foreach (Collider c in controllerColliders) { //GetComponentsInChildren<Collider>()) {
                c.enabled = true && enableColliders;
            }
        }

        /// <summary>
        /// Deactivate the controllers renderer and colliders
        /// </summary>
        protected virtual void HideController () {
            if ((controllerRenderers == null) || (controllerColliders == null)) {
                Debug.LogError ("NetworkInputDevice.HideController: setup missing?: " + controllerRenderers + " " + controllerColliders);
                return;
            }
            foreach (Renderer r in controllerRenderers) { // GetComponentsInChildren<Renderer>()) {
                r.enabled = false;
            }
            foreach (Collider c in controllerColliders) { // GetComponentsInChildren<Collider>()) {
                c.enabled = false;
            }
        }

        /// <summary>
        /// some way to check for real ownership of the object
        /// </summary>
        public string objectOwner;
        [Client]
        public void LocalObjectOwner () {
            NetworkIdentity networkIdentity = GetComponent<NetworkIdentity> ();
            if (isServer) {
                // if this is the server, we may have the authority just because a client has returned the authority of the object
                //playerControllerId ? what is this?
                NetworkConnection netConn = networkIdentity.clientAuthorityOwner;
                objectOwner = (netConn != null) ? netConn.address : "null";
            } else {
                if (hasAuthority) {
                    objectOwner = "localClient";
                } else {
                    objectOwner = "remoteClient";
                }
            }
        }
    }
}
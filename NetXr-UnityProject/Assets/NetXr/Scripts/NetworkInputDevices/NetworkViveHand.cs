//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System;
using UnityEngine;
using UnityEngine.Networking;
using NetXr;

namespace NetXr {
    public class NetworkViveHand : NetworkInputDevice {

        /// <summary>
        /// this is called as long as we have not set a trackedTransform for this controller 
        /// </summary>
        protected override void TryAttachToParent () {
            //Debug.Log("NetworkViveHand.TryAttachToParent: attaching to " + inputDeviceId);
            if (inputDevice != null) {
                gameObject.name = "ViveHand-" + inputDeviceId.ToString ();
                SetTrackedTransform (inputDevice.controller);
            } else {
                Debug.LogError ("NetworkViveHand.TryAttachtoParent: inputDevice undefined! " + inputDeviceId + " " + trackedTransform + " " + inputDevice.ToString ());
            }
        }

        public override void SetTrackedTransform (Transform _trackedTransform) {
            // attach the controller model to the tracked controller object on the local client
            base.SetTrackedTransform (_trackedTransform);
        }

        public override void SetupNetworkControllerCallbacks (InputDevice inputDevice) {
            // setup callbacks only used in networking (usually none)
        }
    }
}
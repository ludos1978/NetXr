//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using UnityEngine;
using UnityEngine.Networking;

namespace NetXr {
    [RequireComponent (typeof (NetworkIdentity))]
    [RequireComponent (typeof (NetworkTransform))]
    public class NetworkMouseHand : NetworkInputDevice {
        [Client]
        public void Awake () { }

        [Client]
        protected override void TryAttachToParent () {
            if (inputDevice != null) { } else {
                Debug.LogError ("NetworkMouseHand.TryAttachtoParent: inputDevice undefined!");
            }

            if (!hasAuthority) {
                return;
            }

            SetTrackedTransform (Camera.main.transform);
        }

        [Client]
        public override void SetupNetworkControllerCallbacks (InputDevice inputDevice) {
            // setup callbacks only used in networking (usually none)
        }
    }
}
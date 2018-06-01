//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System;
#if WSNIO_LEAP_ACTIVE
using Leap;
using Leap.Unity;
using Leap.Unity.Attachments;
#endif
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using NetXr;

namespace NetXr {
    public class PosRot {
        public Vector3 pos;
        public Quaternion rot;
    }

    public class NetworkLeapHand : NetworkInputDevice {
#if WSNIO_LEAP_ACTIVE
        //public LeapHandSide side;
        public GameObject[] syncGameObjects;

        [SyncVar] public Transform palmTransform;
        [SyncVar] public Transform armTransform;
        [SyncVar] public Transform thumbTransform;
        [SyncVar] public Transform pinchpointTransform;
        [SyncVar] public Transform indexTransform;
        [SyncVar] public Transform middleTransform;
        [SyncVar] public Transform ringTransform;
        [SyncVar] public Transform pinkyTransform;
        [SyncVar] public Transform grabpointTransform;
        public List<Transform> transformList;

        private List<string> childNames;

        public override void OnStartClient () {
            base.OnStartClient ();
        }

        public void Awake () {
            //Debug.Log("NetworkLeapHand.Awake: " + name);

            childNames = new List<string> () {
                "Palm",
                "Arm",
                "Thumb",
                "PinchPoint",
                "Index",
                "Middle",
                "Ring",
                "Pinky",
                "GrabPoint"
            };
            transformList = new List<Transform> () {
                palmTransform,
                armTransform,
                thumbTransform,
                pinchpointTransform,
                indexTransform,
                middleTransform,
                ringTransform,
                pinkyTransform,
                grabpointTransform
            };

            for (int i = 0; i < childNames.Count; i++) {
                string childName = childNames[i];
                Transform child = transform.Find (childName);
                transformList[i] = child;
                CustomNetworkTransformChild netC = null;
                try {
                    netC = gameObject.AddComponent<CustomNetworkTransformChild> ();
                } catch { //(Exception e)
                    //
                }
                if (netC != null) {
                    netC.target = child;
                    netC.enabled = true;
                } else {
                    Destroy (netC);
                }
            }
        }

        protected override void TryAttachToParent () {
            if (inputDevice != null) {
                if (inputDevice.inputController is LeapInputController) {
                    if (((LeapInputController) inputDevice.inputController).leapHand) {
                        // is tracked may be false right now, but should be true within this frame
                        //if (inputDevice.inputController.leapController.leapHand.IsTracked) {
                        //Debug.Log("NetworkLeapHand.TryAttachToParent: attaching to " + inputDeviceId + " " + inputDevice);
                        gameObject.name = "LeapHand-" + inputDeviceId.ToString ();
                        SetTrackedTransform (inputDevice.inputController.transform);

                        foreach (FingerRoot fingerRoot in GetComponentsInChildren<FingerRoot> ()) {
                            fingerRoot.SetLeftHand ((inputDevice.deviceHand == InputDeviceHand.Left));
                        }
                    } else {
                        Debug.LogError ("NetworkLeapHand.TryAttachtoParent: leapHand undefined!");
                    }
                } else {
                    Debug.LogError ("NetworkLeapHand.TryAttachtoParent: the inputController is no LeapInputController");
                }
            } else {
                Debug.LogError ("NetworkLeapHand.TryAttachtoParent: inputDevice undefined!");
            }
        }

        public override void SetupNetworkControllerCallbacks (WorldspaceController.WorldspaceInputDevice inputDevice) {
            // setup callbacks only used in networking (usually none)
        }

        public override void LateUpdate () {
            base.LateUpdate ();

            //if (IsTracked() && hasAuthority) {
            if ((inputDevice != null) && (trackedTransform != null)) {
                for (int i = 0; i < childNames.Count; i++) {
                    Transform t = trackedTransform.Find (childNames[i]).transform;
                    transformList[i].position = t.position;
                    transformList[i].rotation = t.rotation;
                }
            }
        }
#endif
    }
}
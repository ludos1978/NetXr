//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using System.Linq;
using System.Collections;

#if WSNIO_LEAP_ACTIVE
using Leap;
using Leap.Unity;
#endif

namespace NetXr {

#if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(LeapInputController))]
    public class LeapInputControllerInspector : InputControllerInspector {
        public override void OnInspectorGUI() {
            LeapInputController myTarget = (LeapInputController)target;
            //myTarget.leapController = myTarget;

            base.OnInspectorGUI();
        }
    }
#endif

    public class LeapInputController : InputController {
    #if WSNIO_LEAP_ACTIVE
        public Leap.Unity.Attachments.HandAttachments leapHand = null;
        public IHandModel iHandModel;

        void Awake() {
            //leapController = this;

            LeapVRTemporalWarping leapVrTemporalWarping = PlayerSettingsController.Instance.cameraInstance.GetComponentInChildren<LeapVRTemporalWarping>();
            LeapProvider leapProvider = PlayerSettingsController.Instance.cameraInstance.GetComponentInChildren<LeapServiceProvider>();
            if (leapProvider == null) {
                Debug.LogError("PlayerSettingsController.SetupLeapController: leapProvider " + leapProvider + " not found in " + PlayerSettingsController.Instance.cameraInstance);
            } else {
                //Debug.Log("PlayerSettingsController.SetupLeapController: leapProvider found");
            }
            //LeapHandController leapHandController = vrCameraRigInstance.GetComponentInChildren<LeapHandController>();
            LeapHandController leapHandController = PlayerSettingsController.Instance.cameraInstance.GetComponentInChildren<LeapHandController>();
            if (leapHandController == null) {
                Debug.LogError("PlayerSettingsController.SetupLeapController: leapHandController " + leapHandController + " not found in " + PlayerSettingsController.Instance.cameraInstance);
            }

            leapVrTemporalWarping.enabled = true;

            //Debug.Log("LeapInputController.Awake");
            iHandModel = GetComponent<IHandModel>();
            if (iHandModel == null) {
                Debug.LogWarning("LeapInputController.Awake: no iHandModel");
                return;
            }
            iHandModel.OnBegin += HandBegin;
            iHandModel.OnFinish += HandFinish;

            base.Awake();
        }

        void OnDestroy() {
            //Debug.Log("LeapInputController.OnDestroy");
            IHandModel iHandModel = GetComponent<IHandModel>();
            if (iHandModel == null) {
                Debug.LogWarning("LeapInputController.OnDestroy: no iHandModel");
                return;
            }
            iHandModel.OnBegin -= HandBegin;
            iHandModel.OnFinish -= HandFinish;
        }

        //void OnEnable() {
        //    base.OnEnable();
        //    //inputDevice.deviceActive = true;
        //    //base.EnableDevice();
        //}

        //void OnDisable() {
        //    //inputDevice.deviceActive = false;
        //    //base.DisableDevice();
        //    base.OnDisable();
        //}

        #region LEAP Controller controlled
        /// <summary>
        /// is called by leap when a new hand is detected by leap motion
        /// </summary>
        protected void HandBegin() {
            leapHand = gameObject.GetComponent<Leap.Unity.Attachments.HandAttachments>();
            controllerActive = true;
        }

        protected void HandFinish() {
            controllerActive = false;
            leapHand = null;
        }
        #endregion

        #region Enable / Disable of the Controller Handling
        internal override void OnEnableController() {
            base.OnEnableController();
        }
        internal override void OnDisableController() {
            base.OnDisableController();
        }
        #endregion

        #region SETUP
        internal override bool SetupController() {
            if (leapHand != null) {
                leapHand = gameObject.GetComponent<Leap.Unity.Attachments.HandAttachments>();
                Transform controller = gameObject.transform;
                Transform uiRaySource = FindChildNamed(leapHand.transform, uiRaySourceTransformName);
                Transform physicsRaySource = FindChildNamed(leapHand.transform, physicsRaySourceTransformName);
                Transform sphereCastPoint = FindChildNamed(leapHand.transform, sphereCastObjectTransformName);
                Transform grabAttachementPoint = FindChildNamed(leapHand.transform, grabAttachementPointTransformName);
                //Transform raySource = FindChildNamed(leapHand.Index, "RaySource"); // leapHand.Index
                //Transform sphereCastPoint = FindChildNamed(leapHand.GrabPoint, "SphereCastPoint");
                //Transform grabAttachementPoint = FindChildNamed(leapHand.GrabPoint, "Attachpoint");

                InputDeviceHand thisDeviceHand = InputDeviceHand.Undefined;
                if (leapHand.GetLeapHand().IsRight) {
                    thisDeviceHand = InputDeviceHand.Right;
                }
                if (leapHand.GetLeapHand().IsLeft) {
                    thisDeviceHand = InputDeviceHand.Left;
                }

                if (uiRaySource == null) {
                    Debug.LogError("LeapInputController.SetupController: uiRaySource is null!");
                }
                if (physicsRaySource == null) {
                    Debug.LogError("LeapInputController.SetupController: physicsRaySource is null!");
                }
                if (grabAttachementPoint == null) {
                    Debug.LogError("LeapInputController.SetupController: grabAttachementPoint is null! " + controller.name);
                }

                inputDevice = new WorldspaceInputDevice() {
                    controller = controller,
                    uiRaySource = uiRaySource,
                    physicsRaySource = physicsRaySource,
                    sphereCastPoint = sphereCastPoint,
                    grabAttachementPoint = grabAttachementPoint,

                    uiActivationMethod = ActivationMethod.TouchDistance,
                    deviceType = InputDeviceType.Leap,
                    deviceHand = thisDeviceHand,
                    inputController = this,
                    physicsCastType = physicsCastTypeDefault,

                    showUiRaySetting = showUiRaySettingDefault,
                    showPhysicsRaySetting = showPhysicsRaySettingDefault,
                    uiCastActive = uiCastActiveDefault,
                    physicsCastActive = physicsCastActiveDefault,

                    physicRayColors = WorldspaceInputDeviceManager.Instance.defaultPhysicsRayGradient,
                    physicsRayMaterial = WorldspaceInputDeviceManager.Instance.defaultPhysicsRayMaterial,
                    uiRayColors = WorldspaceInputDeviceManager.Instance.defaultUiRayGradient,
                    uiRayMaterial = WorldspaceInputDeviceManager.Instance.defaultUiRayMaterial
                };
                WorldspaceInputDeviceManager.Instance.AddDevice(inputDevice);
                inputDevice.deviceActive = true;
                return true;
            }
            return false;
        }

        internal override void SetupCallbacks(WorldspaceInputDevice inputDevice) {
            // default callbacks are now set in inputController
        }

        /// <summary>
        /// callback called when the local player has connected
        /// </summary>
        internal override void SetupNetworkController(NetworkBehaviour netBehaviour) {
            Debug.Log("LeapInputController.SetupNetworkController: called from " + netBehaviour + " creating net leap " + inputDevice.deviceId);
            NetworkPlayerController.LocalInstance.ClientCreateLeapNetworkController(inputDevice.deviceId);
        }
        #endregion
    #endif
    }
}
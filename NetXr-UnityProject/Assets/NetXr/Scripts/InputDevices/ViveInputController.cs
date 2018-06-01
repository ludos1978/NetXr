//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System;
using UnityEngine;
using UnityEngine.Networking;
#if WSNIO_STEAMVR_ACTIVE
using Valve.VR;
#endif

namespace NetXr {

    #if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(ViveInputController))]
    public class ViveInputControllerInspector : InputControllerInspector {
        public override void OnInspectorGUI() {
            ViveInputController myTarget = (ViveInputController)target;
            //myTarget.viveController = myTarget;

            base.OnInspectorGUI();
        }
    }
    #endif


    /// <summary>
    /// this script must be on every vive controller
    /// </summary>
    public class ViveInputController : InputController {
#if NETXR_STEAMVR_ACTIVE
        public SteamVR_Controller.Device steamController = null;
        private int steamControllerIdCheck = -1;

        #region UNITY FUNCTIONS
        void Awake() {
            base.Awake();

            // is likely duplicate functionality, needs testing
            SteamVR_Events.DeviceConnected.AddListener(DeviceConnectedEvent);
            SteamVR_Events.System(Valve.VR.EVREventType.VREvent_TrackedDeviceRoleChanged).Listen(OnTrackedDeviceRoleChanged);
        }

        protected override void Update () {
            base.Update();
            UpdateHapticFeedback();
        }

        private void OnDestroy() {
            SteamVR_Events.DeviceConnected.RemoveListener(DeviceConnectedEvent);
            SteamVR_Events.System(Valve.VR.EVREventType.VREvent_TrackedDeviceRoleChanged).Remove(OnTrackedDeviceRoleChanged);
        }
        #endregion

        #region Unity Device Id Change Handler
        private void OnTrackedDeviceRoleChanged(Valve.VR.VREvent_t arg0) {
            Debug.Log("ViveInputController.OnTrackedDeviceRoleChanged: " + arg0.trackedDeviceIndex+" "+arg0.eventType+" "+arg0.eventAgeSeconds+" "+arg0.data);
            DeviceIdChangeEvent();
        }

        private void DeviceConnectedEvent(int arg0, bool arg1) {
            Debug.Log("ViveInputController.DeviceConnectedEvent: " + arg0 + " " + arg1);
            DeviceIdChangeEvent();
        }

        internal void DeviceIdChangeEvent(bool forceUpdate = false) {
            int steamControllerId = (int)GetComponent<SteamVR_TrackedObject>().index;
            if ((steamControllerIdCheck != steamControllerId) || (forceUpdate)) {
                Debug.LogWarning("ViveInputController.DeviceConnectedEvent: device index changed!");
                UpdateSteamControllerId();
            }
        }

        private void UpdateSteamControllerId () {
            int steamControllerId = (int)GetComponent<SteamVR_TrackedObject>().index;
            steamController = SteamVR_Controller.Input(steamControllerId);
            steamControllerIdCheck = steamControllerId;
        }
        #endregion

        #region STEAM Controller controlled
        // moved to base
        //public void OnEnable()
        //{
        //    base.OnEnable();
        //}

        // moved to base
        //public void OnDisable()
        //{
        //    base.OnDisable();
        //}
        #endregion

        #region Enable / Disable of the Controller
        internal override void OnEnableController() {
            base.OnEnableController();
            DeviceIdChangeEvent();
        }
        internal override void OnDisableController() {
            base.OnDisableController();
        }
        #endregion

        #region SETUP
        internal override bool SetupController() {
            // check if the controller is available so we can initialize it, also calls the callback of worldspaceinputdevicemanager
            if (!controllerInitialized) {
                //int steamControllerId = (int)GetComponent<SteamVR_TrackedObject>().index;
                //steamController = SteamVR_Controller.Input(steamControllerId);
                UpdateSteamControllerId();

                if (steamController.connected) {
                    //Debug.LogError("ViveInputController.SearchSteamVrController: controller connected " + this.ToString() + " " + controller);

                    // when connected, initialize the device
                    InputDeviceHand thisDeviceHand = InputDeviceHand.Undefined;
                    if (gameObject.name.Contains("left")) {
                        thisDeviceHand = InputDeviceHand.Left;
                    } else if (gameObject.name.Contains("right")) {
                        thisDeviceHand = InputDeviceHand.Right;
                    } else {
                        Debug.LogError("ViveInputController.SearchSteamVrController: unable to determine controller handedness from name " + this.ToString());
                    }

                    //Debug.Log("ViveControllerRayEnabler.Update: initialize vive input controller " + name);
                    Transform controller = transform;
                    Transform uiRaySource = FindChildNamed(controller, uiRaySourceTransformName);
                    Transform physicsRaySource = FindChildNamed(controller, physicsRaySourceTransformName);
                    Transform sphereCastPoint = FindChildNamed(controller, sphereCastObjectTransformName);
                    Transform grabAttachementPoint = FindChildNamed(controller, grabAttachementPointTransformName);

                    if (uiRaySource == null) {
                        Debug.LogError("ViveInputController.SetupController: uiRaySource is null!");
                    }
                    if (physicsRaySource == null) {
                        Debug.LogError("ViveInputController.SetupController: physicsRaySource is null!");
                    }
                    if (grabAttachementPoint == null) {
                        Debug.LogError("ViveInputController.SetupController: grabAttachementPoint is null!");
                    }

                    inputDevice = new InputDevice() {
                        controller = controller,
                        uiRaySource = uiRaySource,
                        physicsRaySource = physicsRaySource,
                        sphereCastPoint = sphereCastPoint,
                        grabAttachementPoint = grabAttachementPoint,

                        uiActivationMethod = ActivationMethod.ExternalInput,
                        deviceType = InputDeviceType.Vive,
                        deviceHand = thisDeviceHand,
                        inputController = this,
                        physicsCastType = physicsCastTypeDefault,

                        showUiRaySetting = showUiRaySettingDefault,
                        showPhysicsRaySetting = showPhysicsRaySettingDefault,
                        uiCastActive = uiCastActiveDefault,
                        physicsCastActive = physicsCastActiveDefault,

                        physicRayColors = InputDeviceManager.Instance.defaultPhysicsRayGradient,
                        physicsRayMaterial = InputDeviceManager.Instance.defaultPhysicsRayMaterial,
                        uiRayColors = InputDeviceManager.Instance.defaultUiRayGradient,
                        uiRayMaterial = InputDeviceManager.Instance.defaultUiRayMaterial
                    };
                    InputDeviceManager.Instance.AddDevice(inputDevice);
                    inputDevice.deviceActive = true;
                    return true;
                }
            }
            return false;
        }

        internal override void SetupCallbacks(InputDevice inputDevice) {
            // default callbacks are now set in inputController
        }

        /// <summary>
        /// callback called when the local player has connected
        /// </summary>
        internal override void SetupNetworkController(NetworkBehaviour netBehaviour) {
            Debug.Log("ViveInputController.SetupNetworkController: called from " + netBehaviour);
            // create network view of the vive controller
            NetworkPlayerController.LocalInstance.ClientCreateViveNetworkController(inputDevice.deviceId);
        }
        #endregion

        #region Haptic Pulse
        public override bool HasHapticFeedback () {
            return true;
        }
        /// <summary>
        /// handle haptic pulse
        /// </summary>
        internal override void UpdateHapticFeedback () {
            float pulseTime = Time.time - hapticPulseStart;
            if (pulseTime < hapticPulseLength) {
                if (hapticPulseAnimationCurve != null) {
                    steamController.TriggerHapticPulse((ushort)Mathf.Lerp(0, 3999, hapticPulseStrength * hapticPulseAnimationCurve.Evaluate(pulseTime)));
                } else {
                    steamController.TriggerHapticPulse((ushort)Mathf.Lerp(0, 3999, hapticPulseStrength));
                }
            }
            base.UpdateHapticFeedback();
        }
        #endregion
#endif
    }
}
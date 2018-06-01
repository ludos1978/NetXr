//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

namespace NetXr {
    using System;
    using System.Reflection;
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine.Events;

    [CustomEditor(typeof(MouseController))]
    public class MouseControllerInspector : InputControllerInspector {
        public override void OnInspectorGUI() {
            MouseController myTarget = (MouseController)target;
            //myTarget.mouseController = myTarget;

            base.OnInspectorGUI();
        }
    }
    #endif


    public class MouseController : InputController {
        #region Singleton
        private static MouseController instance = null;
        public static MouseController Instance {
            get {
                if (instance == null) {
                    instance = (MouseController)FindObjectOfType(typeof(MouseController));
                }
                return instance;
            }
        }
        #endregion

        //public bool mouseCanLock = true;
        public bool mouseCameraControlRequiresLock = true;

        protected override void Awake() {
            base.Awake();
        }

        #region SETUP
        internal override bool SetupController () {
            if (PlayerSettingsController.Instance.mouseControllerEnabled) {
                Debug.Log("MouseController.SetupController: start mouse");
                Transform controller = PlayerSettingsController.Instance.headTransform;
                Transform uiRaySource = FindChildNamed(controller, uiRaySourceTransformName);
                Transform physicsRaySource = FindChildNamed(controller, physicsRaySourceTransformName);
                Transform sphereCastPoint = FindChildNamed(controller, sphereCastObjectTransformName);
                Transform grabAttachementPoint = FindChildNamed(controller, grabAttachementPointTransformName);
                inputDevice = new InputDevice() {
                    controller = controller,
                    uiRaySource = uiRaySource,
                    physicsRaySource = physicsRaySource,
                    sphereCastPoint = sphereCastPoint,
                    grabAttachementPoint = grabAttachementPoint,

                    uiActivationMethod = ActivationMethod.ExternalInput,
                    deviceType = InputDeviceType.Mouse,
                    inputController = this,
                    physicsCastType = physicsCastTypeDefault,

                    showUiRaySetting = showUiRaySettingDefault,
                    showPhysicsRaySetting = showPhysicsRaySettingDefault,
                    uiCastActive = uiCastActiveDefault,
                    physicsCastActive = physicsCastActiveDefault,
                    uiPointerOffsetMode = PointerOffsetMode.screenCenter,

                    physicRayColors = InputDeviceManager.Instance.defaultPhysicsRayGradient,
                    physicsRayMaterial = InputDeviceManager.Instance.defaultPhysicsRayMaterial,
                    uiRayColors = InputDeviceManager.Instance.defaultUiRayGradient,
                    uiRayMaterial = InputDeviceManager.Instance.defaultUiRayMaterial
                };
                InputDeviceManager.Instance.AddDevice(inputDevice);
                inputDevice.deviceActive = true;
            }
            return true;
        }

        internal override void SetupCallbacks (InputDevice inputDevice) {
            // default callbacks are now set in inputController
        }

        internal override void SetupNetworkController (NetworkBehaviour netBehaviour) {}
        #endregion

        protected override void Update() {
            base.Update();
        }


        public bool MouseControlsCamera () {
            // is mouse locked? 
            return ((MouseLock.Instance.GetLocked() && mouseCameraControlRequiresLock) || !mouseCameraControlRequiresLock);
        }
    }
}
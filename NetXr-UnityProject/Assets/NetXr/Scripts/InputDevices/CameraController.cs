//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace NetXr {

#if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor (typeof (CameraController))]
    public class CameraControllerInspector : InputControllerInspector {
        public override void OnInspectorGUI () {
            CameraController myTarget = (CameraController) target;
            //myTarget.mouseController = myTarget;

            base.OnInspectorGUI ();
        }
    }
#endif

    public class CameraController : InputController {
        #region Singleton
        private static CameraController instance = null;
        public static CameraController Instance {
            get {
                if (instance == null) {
                    instance = (CameraController) FindObjectOfType (typeof (CameraController));
                }
                return instance;
            }
        }
        #endregion

        public ActivationMethod cameraUiActivationMethod;
        //public KeyCode cameraUiInteractionKey = KeyCode.F2;

        protected override void Awake () {
            base.Awake ();
            InitializeFade ();
        }

        protected override void Update () {
            base.Update ();

            //if (Input.GetKeyDown(cameraUiInteractionKey)) {
            //    inputDevice.externalPressed = true;
            //}
            //if (Input.GetKeyUp(cameraUiInteractionKey)) {
            //    inputDevice.externalReleased = true;
            //}
        }

        protected override void LateUpdate () {
            //GetComponentInParent<PlayerPhysics>().vrControlledHeight = false;
            //GetComponentInParent<PlayerPhysics>().playerHeight = 1.5f;
        }

        #region SETUP
        internal override bool SetupController () {
            // other controllers do the init in activation classes (LeapInputController & ViveControllerRayEnabler)
            if (PlayerSettingsController.Instance.gazeControllerEnabled) {
                Debug.Log ("CameraController.SetupController: start gaze");
                Transform controller = PlayerSettingsController.Instance.headTransform;
                Transform uiRaySource = FindChildNamed (controller.parent, uiRaySourceTransformName);
                Transform physicsRaySource = FindChildNamed (controller.parent, physicsRaySourceTransformName);
                Transform sphereCastPoint = FindChildNamed (controller.parent, sphereCastObjectTransformName);
                Transform grabAttachementPoint = FindChildNamed (controller.parent, grabAttachementPointTransformName);

                if (uiRaySource == null) {
                    Debug.LogError ("MouseController.SetupController: uiRaySource is null!");
                }
                if (physicsRaySource == null) {
                    Debug.LogError ("MouseController.SetupController: physicsRaySource is null!");
                }
                if (grabAttachementPoint == null) {
                    Debug.LogError ("MouseController.SetupController: grabAttachementPoint is null!");
                }

                //Debug.Log("PlayerSettingsController.CreatePlayerCamera: add gaze input controller");
                inputDevice = new InputDevice () {
                    controller = controller,
                    uiRaySource = uiRaySource,
                    physicsRaySource = physicsRaySource,
                    sphereCastPoint = sphereCastPoint,
                    grabAttachementPoint = grabAttachementPoint,

                    uiActivationMethod = cameraUiActivationMethod,
                    deviceType = InputDeviceType.Camera,
                    inputController = this,
                    physicsCastType = physicsCastTypeDefault,

                    showUiRaySetting = showUiRaySettingDefault,
                    showPhysicsRaySetting = showPhysicsRaySettingDefault,
                    uiCastActive = uiCastActiveDefault,
                    physicsCastActive = physicsCastActiveDefault,

                    physicRayColors = InputDeviceManager.Instance.defaultPhysicsRayGradient,
                    uiRayColors = InputDeviceManager.Instance.defaultUiRayGradient
                };
                InputDeviceManager.Instance.AddDevice (inputDevice);
                inputDevice.deviceActive = true;
                //} else {

            }
            return true;
        }

        internal override void SetupCallbacks (InputDevice inputDevice) {
            // default callbacks are now set in inputController
        }

        internal override void SetupNetworkController (NetworkBehaviour netBehaviour) { }
        #endregion

        #region Fade Out Visual stuff
        //SteamVR_Fade steamVrFade;
        IEnumerator fadeCoroutine;
        internal void InitializeFade () { }
        internal void FadeToColor (float _fadeDuration, Color color) {
#if NETXR_STEAMVR_ACTIVE
            if (fadeCoroutine != null) {
                StopCoroutine (fadeCoroutine);
                SteamVR_Fade.Start (Color.clear, 0f);
                fadeCoroutine = null;
            }
            fadeCoroutine = FadeCoroutine (_fadeDuration, color);
            StartCoroutine (fadeCoroutine);
#endif
        }
        // IEnumerator fadeCoroutine = null;
        internal IEnumerator FadeCoroutine (float _fadeDuration, Color color) {
#if NETXR_STEAMVR_ACTIVE
            float halfDuration = _fadeDuration / 2;
            // fade in
            // set start color
            SteamVR_Fade.Start (Color.clear, 0f);
            // set and start fade to
            SteamVR_Fade.Start (Color.black, halfDuration);
            yield return new WaitForSeconds (halfDuration);
            // fade out
            // set start color
            SteamVR_Fade.Start (color, 0);
            // set and start fade to
            SteamVR_Fade.Start (Color.clear, halfDuration);
#endif
            yield return null;
        }

        #endregion
    }
}
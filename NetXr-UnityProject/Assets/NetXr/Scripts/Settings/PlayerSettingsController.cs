//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System.Collections;
using Leap;
using UnityEngine;
using UnityEngine.VR;
#if WSNIO_LEAP_ACTIVE
using Leap.Unity;
using Leap.Unity.InputModule;
#endif
using UnityEngine.EventSystems;
using NetXr;
using UnityEngine.Networking;

namespace NetXr {
    public class PlayerSettingsController : MonoBehaviour {
        #region Singleton
        private static PlayerSettingsController instance = null;
        public static PlayerSettingsController Instance {
            get {
                if (instance == null) {
                    instance = ((PlayerSettingsController) FindObjectOfType (typeof (PlayerSettingsController)));
                }
                return instance;
            }
        }
        #endregion

        [Header("use vr headset or screen")]
        public bool vrEnabled = true;
        [Header("use when vr disabled")]
        public bool mouseControllerEnabled = true;
        [Header("ray from headset, activated by mousebuttons")]
        public bool gazeControllerEnabled = true;
        [Header("requires leap orion library")]
        public bool leapControllerEnabled = true;
        [Header("use vr controllers")]
        public bool viveControllerEnabled = true;

        [Header("add prefabs here")]
        public GameObject mouseLeapCameraPrefab;
        public GameObject mouseCameraPrefab;
        public GameObject vrLeapViveCameraPrefab;
        public GameObject vrViveCameraPrefab;
        public GameObject vrLeapCameraPrefab;
        public GameObject vrCameraPrefab;

        public NetworkStartPosition[] SpawnPoints;

        //public GameObject HeadRepresentationPrefab;

        [HideInInspector]
        public GameObject cameraInstance;
        [HideInInspector]
        public Transform headTransform;

        // Use this for initialization
        void Awake () {

            if (!mouseControllerEnabled && !gazeControllerEnabled && !viveControllerEnabled && !leapControllerEnabled) {
                Debug.LogError ("PlayerSettingsController.Awake: You have selected no controller!");
            }

            if (leapControllerEnabled && SystemInfo.operatingSystem.StartsWith ("Mac OS")) {
                Debug.LogWarning ("PlayerSettingsController.Awake: leap orion drivers not available on OSX! Disabling LeapControllers");
                leapControllerEnabled = false;
            }

            UnityEngine.XR.XRSettings.enabled = vrEnabled;
            if (vrEnabled && (UnityEngine.XR.XRSettings.enabled != vrEnabled)) {
                Debug.LogWarning ("PlayerSettingsController.Awake: enabling VRSettings.enabled failed! disabling vr and vive (" + UnityEngine.XR.XRSettings.enabled + ")");
                viveControllerEnabled = false;
                vrEnabled = false;
            }

            if (SystemInfo.operatingSystem.StartsWith ("Mac OS")) {
                Debug.LogWarning ("PlayerSettingsController.Awake: enabling mouse controller on osx");
                mouseControllerEnabled = true;
                if (MouseLock.Instance) {
                    MouseLock.Instance.mouseCanLock = true;
                }
                // MouseController.Instance.mouseCameraControlRequiresLock = true;
            }

            if (gazeControllerEnabled && mouseControllerEnabled) {
                Debug.LogWarning ("PlayerSettingsController.Awake: mouse and gaze active in settings, deactivating mouse!");
                mouseControllerEnabled = false;
            }

            //if ((leapControllerEnabled || viveControllerEnabled) && (mouseControllerEnabled || gazeControllerEnabled)) {
            //    Debug.LogWarning("PlayerSettingsController.Awake: currently leap & vive is incompatible with mouse & gaze: disabling mouse & gaze");
            //    mouseControllerEnabled = false;
            //    gazeControllerEnabled = false;
            //}

            if (MouseLock.Instance) {
                if (mouseControllerEnabled) {
                    MouseLock.Instance.mouseCanLock = true;
                }
                if (gazeControllerEnabled) {
                    MouseLock.Instance.mouseCanLock = true;
                }
            }

            CreatePlayerCamera ();
        }

        // Update is called once per frame
        void Update () {

        }

        public void CreatePlayerCamera () {

            if (Camera.main.gameObject != null)
                DestroyImmediate (Camera.main.gameObject);

            GameObject cameraPrefab = null;
            //Debug.Log ("PlayerSettingsController.CreatePlayerCamera: VrDevice: "+VrDevice()+" HasLeapController: "+HasLeapController());
            // mouse controlled

            // default position for VR
            Vector3 instancePosition = Vector3.zero;

            if (VrDevice () == "none") {
                // position for mouse
                instancePosition = Vector3.up * 1.5f;
                // using leap controller
                if (leapControllerEnabled) {
                    //Debug.Log ("PlayerSettingsController.CreatePlayerCamera: create mouseLeapCameraPrefab");
                    cameraPrefab = mouseLeapCameraPrefab;
                }
                // no leap controller
                else {
                    //Debug.Log ("PlayerSettingsController.CreatePlayerCamera: create mouseCameraPrefab");
                    cameraPrefab = mouseCameraPrefab;
                }
            }
            // vr controlled
            else {
                // using leap & vive controllers
                if (leapControllerEnabled && viveControllerEnabled) {
                    cameraPrefab = vrLeapViveCameraPrefab;
                }
                // using only leap controller
                else if (leapControllerEnabled && !viveControllerEnabled) {
                    cameraPrefab = vrLeapCameraPrefab;
                }
                // using only vive controller
                else if (!leapControllerEnabled && viveControllerEnabled) {
                    //Debug.Log ("PlayerSettingsController.CreatePlayerCamera: create vrCameraPrefab");
                    cameraPrefab = vrViveCameraPrefab;
                }
                // using only mouse
                else {
                    cameraPrefab = vrCameraPrefab;
                }
            }

            //if (SpawnPoints.Length > 0)
            //{
            //    instancePosition += SpawnPoints[0].transform.position;
            //}

            //WorldspaceController.MouseController.Instance.enabled = mouseControllerEnabled || gazeControllerEnabled;

            Transform instanceRoot = null;
            Quaternion instanceRotation = Quaternion.identity;
            //Debug.Log("PlayerSettingsController.CreatePlayerCamera: creating " + cameraPrefab.name + " under " + (instanceRoot != null ? instanceRoot.name : "root"));

            cameraInstance = (GameObject) Instantiate (cameraPrefab, instancePosition, instanceRotation, instanceRoot);
            cameraInstance.SetActive (true);

            if (VrDevice () == "none") {
                headTransform = Camera.main.transform;
            } else {
#if NETXR_STEAMVR_ACTIVE
                GameObject steamVrCamera = cameraInstance.GetComponentInChildren<SteamVR_Camera> ().gameObject;
                headTransform = steamVrCamera.transform;
#else
                Debug.LogError("SteamVR required with VR Device");
#endif
            }

            //SetupLeapController();
            //if (viveControllerEnabled) {
            //}
        }

        //public void SetupLeapController() {
        //    if (HasLeapController()) {
        //        LeapVRTemporalWarping leapVrTemporalWarping = cameraInstance.GetComponentInChildren<LeapVRTemporalWarping>();

        //        //LeapServiceProvider leapServiceProvider = vrCameraRigInstance.GetComponentInChildren<LeapServiceProvider>();
        //        LeapProvider leapProvider = cameraInstance.GetComponentInChildren<LeapServiceProvider>();
        //        if (leapProvider == null) {
        //            Debug.LogError("PlayerSettingsController.SetupLeapController: leapProvider " + leapProvider + " not found in " + cameraInstance);
        //        }
        //        else {
        //            //Debug.Log("PlayerSettingsController.SetupLeapController: leapProvider found");
        //        }
        //        //LeapHandController leapHandController = vrCameraRigInstance.GetComponentInChildren<LeapHandController>();
        //        LeapHandController leapHandController = cameraInstance.GetComponentInChildren<LeapHandController>();
        //        if (leapHandController == null) {
        //            Debug.LogError("PlayerSettingsController.SetupLeapController: leapHandController " + leapHandController + " not found in " + cameraInstance);
        //        }

        //        leapVrTemporalWarping.enabled = true;
        //    }
        //}

        public Transform GetHeadTransform () {
            return headTransform;
        }

        //public bool HasLeapController() {
        //    if (SystemInfo.operatingSystem.StartsWith("Mac OS")) {
        //        return false;
        //    }
        //    else {
        //        if (leapControllerEnabled) {
        //            return true;
        //            // sometimes causes editor crash
        //            //Controller checkLeapController = new Controller ();
        //            //return checkLeapController.IsConnected; 
        //        }
        //        else {
        //            return false;
        //        }
        //    }
        //}

        public string VrDevice () {
            string vrDevice = (UnityEngine.XR.XRSettings.enabled ? UnityEngine.XR.XRDevice.model : "none");
            //Debug.Log("PlayerSettingsController.VrDevice: " + vrDevice + " " + VRSettings.enabled);
            return vrDevice;
        }
    }
}
//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace NetXr {

    [System.Serializable]
    public class UnityIntEvent : UnityEvent<int> { }

    /// <summary>
    /// Keeps track of all WorldspaceInputDevices
    /// </summary>
    public class InputDeviceManager : MonoBehaviour, IEnumerable<InputDevice> {
        #region Singleton
        private static InputDeviceManager instance = null;
        public static InputDeviceManager Instance {
            get {
                if (instance == null) {
                    instance = ((InputDeviceManager)FindObjectOfType(typeof(InputDeviceManager)));
                }
                return instance;
            }
        }
        #endregion

        #region Callbacks
        // stores newly added device id's
        private List<int> newInputDevicesInitialized = new List<int>();
        // initiates callbacks to devices
        public UnityIntEvent newInputDeviceInitializedEvent = new UnityIntEvent();

        /// <summary>
        /// Method to attach to a callback of a input device
        /// </summary>
        public void AddCallbackForInputDevice (UnityAction<int> call) {
            newInputDeviceInitializedEvent.AddListener(call);
            foreach (InputDevice inputDevice in InputDeviceManager.Instance) {
                call(inputDevice.deviceId);
            }
        }
        /// <summary>
        /// Method to remove from a callback of a input device
        /// </summary>
        public void RemoveCallForInputDevice (UnityAction<int> call) {
            newInputDeviceInitializedEvent.RemoveListener(call);
        }
        #endregion

        [Header("add a sprite for the reticle here")]
        public Sprite defaultReticleSprite;
        public Gradient defaultUiRayGradient = new Gradient();
        public Material defaultUiRayMaterial;
        public Gradient defaultPhysicsRayGradient = new Gradient();
        public Material defaultPhysicsRayMaterial;

        [Header("call UpdateReticles after adding a entry @ runtime")]
        [SerializeField]
        private List<InputDevice> inputDevices;
        //private bool newInputDeviceConnected = false;

        public KeyCode viveControllerReinitializeButton = KeyCode.None;

        void Update() {
            if (!InputModule.Instance.IsInitialized()) {
                Debug.LogWarning("WorldspaceInputDeviceManager.Update: WorldspaceInputModule not yet initialized!");
                return;
            }

            for (int i = 0; i < inputDevices.Count; i++) {
                if (!inputDevices[i].isInitialized) {
                    Debug.Log("WorldspaceInputDeviceManager.Update: found not initialized device, doing it now");
                    int deviceId = inputDevices[i].Initialize();
                    newInputDevicesInitialized.Add(deviceId);
                }
            }

            if (newInputDevicesInitialized.Count > 0) {
                foreach (int deviceId in newInputDevicesInitialized) {
                    newInputDeviceInitializedEvent.Invoke(deviceId);
                    Debug.Log(newInputDevicesInitialized.Count);
                }
                newInputDevicesInitialized = new List<int>();
            }

            if (Input.GetKeyDown(viveControllerReinitializeButton)) {
                ViveControllerReinitialize();
            }
        }

        public int AddDevice(InputDevice inputDevice) {
            inputDevices.Add(inputDevice);
            int deviceId = inputDevice.Initialize();
            newInputDevicesInitialized.Add(deviceId);
            return deviceId;
        }

        /// <summary>
        /// debug function, because of rare vive controllers sending events from wrong controller
        /// </summary>
        private void ViveControllerReinitialize () {
            List<InputDevice> viveControllers = new List<InputDevice>();
            viveControllers.AddRange(new List<InputDevice>(GetInputDevicesOfType(InputDeviceType.Vive, InputDeviceHand.Left, true)));
            viveControllers.AddRange(new List<InputDevice>(GetInputDevicesOfType(InputDeviceType.Vive, InputDeviceHand.Right, true)));
            viveControllers.AddRange(new List<InputDevice>(GetInputDevicesOfType(InputDeviceType.Vive, InputDeviceHand.Undefined, true)));
            Debug.LogWarning("WorldspaceInputDeviceManager.Update: resetting "+ viveControllers.Count + " vive controllers");
#if NETXR_STEAMVR_ACTIVE
            for (int i=0; i<viveControllers.Count; i++) {
                (viveControllers[i].inputController as ViveInputController).DeviceIdChangeEvent(true);
            }
#endif
        }

        public int GetDeviceCount () {
            return inputDevices.Count;
        }
        public InputDevice GetDevice (int index) {
            return inputDevices[index];
        }

        /// <summary>
        /// enumarator for this class to iterate over the WorldspaceInputDevice's
        /// </summary>
        /// <returns></returns>
        public IEnumerator<InputDevice> GetEnumerator() {
            for (int i = 0; i < InputDeviceManager.Instance.inputDevices.Count; i++) {
                yield return InputDeviceManager.Instance.inputDevices[i];
            }
        }
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }


        /// <summary>
        /// generates and returns a new device id
        /// </summary>
        private int deviceIdCounter = -1;
        internal int GetDeviceId() {
            deviceIdCounter += 1;
            return deviceIdCounter;
        }

        /// <summary>
        /// search all input devices for a type of device
        /// </summary>
        public InputDevice[] GetInputDevicesOfType(InputDeviceType _deviceType, InputDeviceHand _deviceHand, bool returnInactive = true) {
            //Debug.Log ("WorldspaceInputModule.GetInputDevicesOfType: " + _deviceType + " " + _deviceHand + " " + returnInactive);
            List<InputDevice> foundDevices = new List<InputDevice>();

            for (int i = 0; i < InputDeviceManager.Instance.inputDevices.Count; i++) {
                //Debug.Log("- WorldspaceInputModule.GetInputDevicesOfType: comparing with " + inputDevices[i].deviceType + " " + inputDevices[i].deviceHand + " " + inputDevices[i].deviceActive);
                if ((InputDeviceManager.Instance.inputDevices[i].deviceType == _deviceType) && (InputDeviceManager.Instance.inputDevices[i].deviceHand == _deviceHand)) {
                    if (returnInactive) {
                        foundDevices.Add(InputDeviceManager.Instance.inputDevices[i]);
                    } else {
                        if (InputDeviceManager.Instance.inputDevices[i].deviceActive) {
                            foundDevices.Add(InputDeviceManager.Instance.inputDevices[i]);
                        }
                    }
                }
            }

            return foundDevices.ToArray();
        }

        /// <summary>
        /// return the device with the given identifier
        /// </summary>
        public InputDevice GetInputDeviceFromId(int deviceId) {
            for (int i = 0; i < InputDeviceManager.Instance.inputDevices.Count; i++) {
                if (deviceId == InputDeviceManager.Instance.inputDevices[i].deviceId) {
                    return InputDeviceManager.Instance.inputDevices[i];
                }
            }
            Debug.Log("WorldspaceInputDeviceManager.GetInputDeviceFromId: no input device with id " + deviceId + " in " + InputDeviceManager.Instance.inputDevices.Count);
            return null;
        }
    }
}
//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

#if NETXR_STEAMVR_ACTIVE
using Valve.VR;
#endif
using Leap;
using Leap.Unity;

namespace NetXr {
    using System.Reflection;
    #region UNITY_EDITOR
#if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor (typeof (InputController))]
    public class InputControllerInspector : Editor {
        /// <summary>
        /// must run before WorldspaceInputController (device startup before controller can handle it)
        /// </summary>
        public int executionOrder = -8000;

        // http://mandarin.no/article/list-all-serialized-properties-in-a-custom-inspector/
        private SerializedObject so;
        private SerializedProperty prop;

        public void OnEnable () {
            InputController myTarget = (InputController) target;
            // First you get the MonoScript of your MonoBehaviour
            //MonoScript monoScript = MonoScript.FromMonoBehaviour(myTarget);
            //// Getting the current execution order of that MonoScript
            //int currentExecutionOrder = MonoImporter.GetExecutionOrder(monoScript);
            //if (currentExecutionOrder != executionOrder) {
            //    // Changing the MonoScript's execution order
            //    MonoImporter.SetExecutionOrder(monoScript, executionOrder);
            //}

            so = new SerializedObject (target);
        }

        public override void OnInspectorGUI () {
            InputController myTarget = (InputController) target;
            /*EditorGUILayout.LabelField("Some help", "Some other text");
            myTarget.speed = EditorGUILayout.Slider("Speed", myTarget.speed, 0, 100);*/

            this.serializedObject.Update ();

            GUILayout.Space (10);
            GUILayout.Label ("DEFAULTS");
            ListParametersEndingWith ("Default");

            myTarget.physicsRaySourceTransformName = EditorGUILayout.TextField ("physicsRaySourceTransformName ", myTarget.physicsRaySourceTransformName);
            myTarget.uiRaySourceTransformName = EditorGUILayout.TextField ("uiRaySourceTransformName ", myTarget.uiRaySourceTransformName);
            myTarget.sphereCastObjectTransformName = EditorGUILayout.TextField ("sphereCastObjectTransformName ", myTarget.sphereCastObjectTransformName);
            myTarget.grabAttachementPointTransformName = EditorGUILayout.TextField ("grabAttachementPointTransformName ", myTarget.grabAttachementPointTransformName);

            GUILayout.Space (10);
            GUILayout.Label ("GENERIC");
            ListParametersStartingWith ("generic");

            GUILayout.Space (10);
            if (myTarget is MouseController) {
                GUILayout.Label ("MOUSE");
                ListParametersStartingWith ("mouse");
            } else if (myTarget is LeapInputController) {
                GUILayout.Label ("LEAP");
                ListParametersStartingWith ("leap");
            } else if (myTarget is ViveInputController) {
                GUILayout.Label ("VIVE");
                ListParametersStartingWith ("vive");
            } else if (myTarget is CameraController) {
                GUILayout.Label ("CAMERA");
                ListParametersStartingWith ("camera");
            } else {
                GUILayout.Label ("No Devicetype found");
            }

            if (GUI.changed) {
                so.ApplyModifiedProperties ();
            }
        }

        public void ListParametersStartingWith (string filter, bool listHidden = false) {
            prop = so.GetIterator ();
            prop.NextVisible (true);
            do {
                if (prop.name.StartsWith (filter)) {
                    EditorGUILayout.PropertyField (prop);
                } else {
                    if (listHidden) {
                        GUILayout.Label ("hidden '" + prop.name + "' - " + prop.displayName + "'");
                    }
                }
            } while (prop.NextVisible (false));
        }

        public void ListParametersEndingWith (string filter, bool listHidden = false) {
            prop = so.GetIterator ();
            prop.NextVisible (true);
            do {
                if (prop.name.EndsWith (filter)) {
                    EditorGUILayout.PropertyField (prop);
                } else {
                    if (listHidden) {
                        GUILayout.Label ("hidden '" + prop.name + "' - " + prop.displayName + "'");
                    }
                }
            } while (prop.NextVisible (false));
        }

    }
#endif
    #endregion

    #region UnityEvents for InputController callbacks
    [System.Serializable]
    public class UnityDeviceDataEvent : UnityEvent<InputDeviceData> { }
        [System.Serializable]
    public class UnityDeviceFloatEvent : UnityEvent<InputDeviceData, float> { }
        [System.Serializable]
    public class UnityDeviceIntEvent : UnityEvent<InputDeviceData, int> { }
        [System.Serializable]
    public class UnityDeviceVector2Event : UnityEvent<InputDeviceData, Vector2> { }
        [System.Serializable]
    public class UnityControllerEvent : UnityEvent<InputController> { }
    #endregion

    /// <summary>
    /// the input controller generates events from device inputs
    /// </summary>
    [System.Serializable]
    public class InputController : MonoBehaviour {
        private bool __controllerInitialized = false;
        internal bool controllerInitialized { get { return __controllerInitialized; } }
        private bool __controllerCallbacksInitialized = false;
        internal bool controllerCallbacksInitialized { get { return __controllerCallbacksInitialized; } }
        private bool __controllerNetworkInitialized = false;
        internal bool controllerNetworkInitialized { get { return __controllerNetworkInitialized; } }

        [SerializeField]
        public InputDevice inputDevice;

        public bool uiCastActiveDefault = true;
        public bool physicsCastActiveDefault = true;
        public UiCastVisualizeSetting showUiRaySettingDefault = UiCastVisualizeSetting.Allways;
        public PhysicsCastVisualizeSetting showPhysicsRaySettingDefault = PhysicsCastVisualizeSetting.Allways;

        public string physicsRaySourceTransformName = "RaySource";
        public string uiRaySourceTransformName = "RaySource";
        public string sphereCastObjectTransformName = "SphereCastPoint";
        public string grabAttachementPointTransformName = "Attachpoint";

        public PhysicsCastType physicsCastTypeDefault = PhysicsCastType.ray;

        public UnityControllerEvent onEnableControllerEvent = new UnityControllerEvent ();
        public UnityControllerEvent onDisableControllerEvent = new UnityControllerEvent ();

        private bool _controllerActive = false;
        [HideInInspector]
        public bool controllerActive {
            get {
                return _controllerActive;
            }
            set {
                if (_controllerActive != value) {
                    _controllerActive = value;
                    if (value) {
                        OnEnableController ();
                        onEnableControllerEvent.Invoke (this);
                    } else {
                        OnDisableController ();
                        onDisableControllerEvent.Invoke (this);
                    }
                }
            }
        }

        #region UNITY Functions
        protected virtual void Awake () {
            InputDeviceManager.Instance.newInputDeviceInitializedEvent.AddListener (__SetupCallbacks);
        }
        protected void OnEnable () {
            //inputDevice.deviceActive = true;
        }
        protected void OnDisable () {
            //inputDevice.deviceActive = false;
        }

        protected virtual void Update () {
            // if not initialized setup
            if (!__controllerInitialized) {
                __SetupController ();
                // if still not initialized abort
                if (!__controllerInitialized) {
                    return;
                }
            }

            if ((inputDevice == null) && (inputDevice.deviceData == null)) {
                Debug.LogError ("InputController.Update: cannot send data with events! m " + (this is MouseController) + " l " + (this is LeapInputController) + " v " + (this is ViveInputController) + " i " + inputDevice + " d " + (inputDevice != null ? inputDevice.deviceData.ToString () : "null"));
                return;
            }

            // mouse and keyboard can send events to any inputcontroller
            UpdateMouseInput ();
            UpdateKeyboardInput ();

            // leap and vive require the script to be attached to a specific controller
            if (inputDevice.deviceType == InputDeviceType.Vive)
                UpdateViveInput ();
            if (inputDevice.deviceType == InputDeviceType.Leap)
                UpdateLeapInput ();

            UpdateRepeatEvents ();

            UpdateHapticFeedback ();
        }

        // it would be good to run usual events in late update and ui/physics activation events in update
        // currently you cannot activate physics or ui casting in the same frame and get a result from it
        protected virtual void LateUpdate () {
            //switch (inputDevice.deviceType) {
            //    case InputDeviceType.Mouse:
            //        UpdateMouseInput();
            //        break;
            //    case InputDeviceType.Vive:
            //        UpdateViveInput();
            //        break;
            //    case InputDeviceType.Leap:
            //        UpdateLeapInput();
            //        break;
            //    case InputDeviceType.Gaze:
            //        break;
            //}
        }
        #endregion

        #region Enabling / Disabling of the Input Controller
        /// <summary>
        /// executed when a device is found/enabled/tracked
        /// </summary>
        internal virtual void OnEnableController () {
            Debug.Log ("InputController.EnableDevice");

            if (inputDevice != null) {
                // prevent multiple enable calls
                if (!inputDevice.deviceActive) {
                    inputDevice.deviceActive = true;

                    OnViveEnabled ();
                    OnLeapEnabled ();

                    //deviceEnableEvent.Invoke(inputDevice.deviceData);
                }
            } else {
                Debug.LogWarning ("InputController.OnDisable: inputDevice not defined");
                return;
            }
        }

        /// <summary>
        /// executed when a device is lost/disabled/untracked
        /// </summary>
        internal virtual void OnDisableController () {
            Debug.Log ("InputController.Disable");

            if (inputDevice != null) {
                // prevent multiple disable calls
                if (inputDevice.deviceActive) {
                    inputDevice.deviceActive = false;

                    //deviceDisableEvent.Invoke(inputDevice.deviceData);
                    DisableHapticFeedback ();

                    OnViveDisabled ();
                    OnLeapDisabled ();
                }
            } else {
                Debug.LogWarning ("InputController.OnDisable: inputDevice not defined");
                return;
            }
        }
        #endregion

        #region SETUP
        /// <summary>
        /// internal method that is called in update while not initialized
        /// </summary>
        private void __SetupController () {
            __controllerInitialized = SetupController ();
        }

        /// <summary>
        /// initialize controller, return true on successful init
        /// </summary>
        internal virtual bool SetupController () {
            //Debug.LogError("InputController.SetupController: not implemented for " + this.ToString());
            throw new NotImplementedException ("InputController.SetupController: not implemented for " + this.ToString ());
        }

        //private void __DestroyController() {
        //    __controllerInitialized = false;
        //    __controllerCallbacksInitialized = false;
        //    __controllerNetworkInitialized = false;

        //    inputDevice = null;
        //}

        /// <summary>
        /// internal method that calls child functions to init the callbacks
        /// </summary>
        private void __SetupCallbacks (int inputDeviceId) {
            // if the callback is for our device
            if (inputDeviceId == inputDevice.deviceId) {
                if (!__controllerCallbacksInitialized) {
                    __controllerCallbacksInitialized = true;
                    InputDeviceManager.Instance.newInputDeviceInitializedEvent.RemoveListener (__SetupCallbacks);

                    SetupCallbacks (inputDevice);

                    AddGenericCallbacks ();

                    //if (this is MouseController) {
                    //    AddMouseCallbacks();
                    //}
                    //if (this is LeapInputController) {
                    //    AddLeapCallbacks();
                    //}
                    //if (this is ViveInputController) {
                    //    AddViveCallbacks();
                    //}
                    //if (this is CameraController) {
                    //    AddCameraCallbacks();
                    //}

                    //inputDevice.onSetDeviceActiveEvent.AddListener(OnEnableController);
                    //inputDevice.onSetDeviceInactiveEvent.AddListener(OnDisableController);

                    // depending on connection status create the network device now or on connection
                    //Debug.LogError("ViveInputController.SetupInputEvents: " + this.ToString() + " creating networkInterface " + (NetworkManagerModuleManager.Instance.IsClientConnected() ? "now":"on connect"));
                    if (NetworkManagerModuleManager.Instance.IsClientConnected ()) {
                        __SetupNetworkController (NetworkPlayerController.LocalInstance); //NetworkPlayerController.LocalInstance.connectionToServer);
                    } else {
                        NetworkManagerModuleManager.Instance.onStartLocalPlayerEvent.AddListener (__SetupNetworkController);
                    }
                }
            }
        }

        /// <summary>
        /// initialize callbacks, should be override by childrens
        /// </summary>
        internal virtual void SetupCallbacks (InputDevice inputDevice) {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// internal method that is called to setup the controller
        /// </summary>
        private void __SetupNetworkController (NetworkBehaviour netBehaviour) {
            // it must run now, or it will never run, but give errors if there is something wrong
            if ((!__controllerInitialized) || (!__controllerCallbacksInitialized)) {
                Debug.LogError ("InputController.__SetupNetworkController: should have been initialized already: controller: " + __controllerInitialized + " callbacks: " + __controllerCallbacksInitialized);
            }
            __controllerNetworkInitialized = true;
            SetupNetworkController (netBehaviour);
            NetworkManagerModuleManager.Instance.onStartLocalPlayerEvent.RemoveListener (__SetupNetworkController);
        }

        /// <summary>
        /// must be override by inherited classes to setup the networkcontroller
        /// </summary>
        /// <param name="inputDevice">Input device.</param>
        /// <param name="netConnection">Net connection.</param>
        internal virtual void SetupNetworkController (NetworkBehaviour netBehaviour) {
            throw new NotImplementedException ();
        }

        #endregion

        /// <summary>
        /// should probably be moved to WorldspaceInputDevice, but need to check how this influences the external adding of events
        /// </summary>
        #region INTERACTABLEOBJECT
        public void DoEnableUiCast (InputDeviceData deviceData) {
            //Debug.Log("InputController.EnableUiCast "+inputDevice.ToString());
            inputDevice.uiCastActive = true;
        }
        public void DoDisableUiCast (InputDeviceData deviceData) {
            //Debug.Log("InputController.DisableUiCast "+inputDevice.ToString());
            inputDevice.uiCastActive = false;
        }

        public void DoEnablePhysicsCast (InputDeviceData deviceData) {
            //Debug.Log("InputController.EnablePhysicsCast "+inputDevice.ToString());
            inputDevice.physicsCastActive = true;
        }
        public void DoDisablePhysicsCast (InputDeviceData deviceData) {
            //Debug.Log("InputController.DisablePhysicsCast "+inputDevice.ToString());
            inputDevice.physicsCastActive = false;
        }

        /// <summary>
        /// switch the physics cast type
        /// </summary>
        /// <param name="deviceData"></param>
        public void DoSetRayCastPhysicsCast (InputDeviceData deviceData) {
            inputDevice.physicsCastType = PhysicsCastType.ray;
        }
        public void DoSetParabolaCastPhysicsCast (InputDeviceData deviceData) {
            inputDevice.physicsCastType = PhysicsCastType.parabola;
        }
        public void DoSetSphereCastPhysicsCast (InputDeviceData deviceData) {
            inputDevice.physicsCastType = PhysicsCastType.sphere;
        }

        public void DoUiPressed (InputDeviceData deviceData) {
            //Debug.Log("InputController.OnUiPressed "+inputDevice.ToString());
            inputDevice.externalPressed = true;
        }
        public void DoUiReleased (InputDeviceData deviceData) {
            //Debug.Log("InputController.UiReleased "+inputDevice.ToString());
            inputDevice.externalReleased = true;
        }

        /// <summary>
        /// events are generated and sent to interactableObjects
        /// repeat events are run in update, based on list
        /// start & stop events are generated dynamically from inputs
        /// </summary>
        List<InteractableObject> usedInteractableObjects = new List<InteractableObject> ();
        public void DoUseHoveredAndGrabbedObjectStart (InputDeviceData deviceData) {
            useDown.Invoke(deviceData);
            if (deviceData.grabbedInteractableObject) {
                deviceData.grabbedInteractableObject.__OnUseStart (deviceData);
                usedInteractableObjects.Add (deviceData.grabbedInteractableObject);
            }
            foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
                // dont call the grabbed object twice
                if (interactableObject == deviceData.grabbedInteractableObject)
                    continue;
                interactableObject.__OnUseStart (deviceData);
                usedInteractableObjects.Add (interactableObject);
            }
            foreach (InteractableObject interactableObject in deviceData.extraReceiveEventObjects) {
                if (deviceData.hoveredInteractableObjects.Contains (interactableObject)) {
                    // skip
                } else if (interactableObject == deviceData.grabbedInteractableObject) {
                    // skip
                } else {
                    interactableObject.__OnUseStart (deviceData);
                    usedInteractableObjects.Add (interactableObject);
                }
            }
        }
        public void DoUseRepeat (InputDeviceData deviceData) {
            usePressed.Invoke(deviceData);
            foreach (InteractableObject interactableObject in usedInteractableObjects) {
                interactableObject.__OnUseRepeat (deviceData);
            }
        }
        public void DoUseHoveredAndGrabbedObjectStop (InputDeviceData deviceData) {
            useUp.Invoke(deviceData);
            foreach (InteractableObject interactableObject in usedInteractableObjects) {
                interactableObject.__OnUseStop (deviceData);
            }
            usedInteractableObjects = new List<InteractableObject> ();
            //if (deviceData.grabbedInteractableObject) { 
            //    deviceData.grabbedInteractableObject.__OnUseStop(deviceData);
            //    usedInteractableObjects.Remove(deviceData.grabbedInteractableObject);
            //}
            //foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
            //    // dont call the grabbed object twice
            //    if (interactableObject == deviceData.grabbedInteractableObject)
            //        continue;
            //    interactableObject.__OnUseStop(deviceData);
            //    usedInteractableObjects.Remove(interactableObject);
            //}
        }

        /// <summary>
        /// events are generated and sent to interactableObjects
        /// repeat events are run in update, based on list
        /// start & stop events are generated dynamically from inputs
        /// </summary>
        List<InteractableObject> grabbedInteractableObjects = new List<InteractableObject> ();
        public void DoGrabHoveredObjectStart (InputDeviceData deviceData) {
            grabDown.Invoke(deviceData);
            // TODO: testing might not be useful here
            if (deviceData.grabbedInteractableObject) {
                deviceData.grabbedInteractableObject.__OnGrabStart (deviceData);
                grabbedInteractableObjects.Add (deviceData.grabbedInteractableObject);
            }
            // must be here
            foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
                interactableObject.__OnGrabStart (deviceData);
                grabbedInteractableObjects.Add (interactableObject);
            }
            // New: testing if this is useful
            foreach (InteractableObject interactableObject in deviceData.extraReceiveEventObjects) {
                if (deviceData.hoveredInteractableObjects.Contains (interactableObject)) {
                    // skip
                } else if (interactableObject == deviceData.grabbedInteractableObject) {
                    // skip
                } else {
                    interactableObject.__OnGrabStart (deviceData);
                    grabbedInteractableObjects.Add (interactableObject);
                }
            }
        }
        public void DoGrabRepeat (InputDeviceData deviceData) {
            grabPressed.Invoke(deviceData);
            foreach (InteractableObject interactableObject in grabbedInteractableObjects) {
                interactableObject.__OnGrabRepeat (deviceData);
            }
        }
        public void DoGrabGrabbedObjectStop (InputDeviceData deviceData) {
            grabUp.Invoke(deviceData);
            foreach (InteractableObject interactableObject in grabbedInteractableObjects) {
                interactableObject.__OnGrabStop (deviceData);
            }
            grabbedInteractableObjects = new List<InteractableObject> ();
            //foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
            //    if (interactableObject == deviceData.grabbedInteractableObject)
            //        continue;
            //    interactableObject.__OnGrabStop(deviceData);
            //}
            //if (deviceData.grabbedInteractableObject)
            //    deviceData.grabbedInteractableObject.__OnGrabStop(deviceData);
        }

        /// <summary>
        /// events are generated and sent to interactableObjects
        /// repeat events are run in update, based on list
        /// start & stop events are generated dynamically from inputs
        /// </summary>
        List<InteractableObject> touchedInteractableObjects = new List<InteractableObject> ();
        public void DoTouchHoveredAndGrabbedObjectStart (InputDeviceData deviceData) {
            touchDown.Invoke(deviceData);
            if (deviceData.grabbedInteractableObject) {
                deviceData.grabbedInteractableObject.__OnTouchStart (deviceData);
                touchedInteractableObjects.Add (deviceData.grabbedInteractableObject);
            }
            foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
                // dont call the grabbed object twice
                if (interactableObject == deviceData.grabbedInteractableObject)
                    continue;
                interactableObject.__OnTouchStart (deviceData);
                touchedInteractableObjects.Add (interactableObject);
            }
            foreach (InteractableObject interactableObject in deviceData.extraReceiveEventObjects) {
                if (deviceData.hoveredInteractableObjects.Contains (interactableObject)) {
                    // skip
                } else if (interactableObject == deviceData.grabbedInteractableObject) {
                    // skip
                } else {
                    interactableObject.__OnTouchStart (deviceData);
                    touchedInteractableObjects.Add (interactableObject);
                }
            }
        }
        public void DoTouchRepeat (InputDeviceData deviceData) {
            touchPressed.Invoke(deviceData);
            foreach (InteractableObject interactableObject in touchedInteractableObjects) {
                interactableObject.__OnTouchRepeat (deviceData);
            }
        }
        public void DoTouchHoveredAndGrabbedObjectStop (InputDeviceData deviceData) {
            touchUp.Invoke(deviceData);
            foreach (InteractableObject interactableObject in touchedInteractableObjects) {
                interactableObject.__OnTouchStop (deviceData);
            }
            touchedInteractableObjects = new List<InteractableObject> ();
            //if (deviceData.grabbedInteractableObject)
            //    deviceData.grabbedInteractableObject.__OnTouchStop(deviceData);
            //foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
            //    // dont call the grabbed object twice
            //    if (interactableObject == deviceData.grabbedInteractableObject)
            //        continue;
            //    interactableObject.__OnTouchStop(deviceData);
            //}
        }

        List<InteractableObject> eventOneHoveredInteractableObjects = new List<InteractableObject> ();
        public void DoEventOneHoveredObjectsStart (InputDeviceData deviceData) {
            foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
                interactableObject.__OnEventOneStart (deviceData);
                eventOneHoveredInteractableObjects.Add (interactableObject);
            }
        }
        public void DoEventOneHoveredObjectsStop (InputDeviceData deviceData) {
            foreach (InteractableObject interactableObject in eventOneHoveredInteractableObjects) {
                interactableObject.__OnEventOneStop (deviceData);
            }
            eventOneHoveredInteractableObjects = new List<InteractableObject> ();
            //foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
            //    interactableObject.__OnEventOneStop(deviceData);
            //}
        }

        List<InteractableObject> eventOneGrabbedInteractableObjects = new List<InteractableObject> ();
        public void DoEventOneGrabbedObjectStart (InputDeviceData deviceData) {
            if (deviceData.grabbedInteractableObject) {
                deviceData.grabbedInteractableObject.__OnEventOneStart (deviceData);
                eventOneGrabbedInteractableObjects.Add (deviceData.grabbedInteractableObject);
            }
        }
        public void DoEventOneGrabbedObjectStop (InputDeviceData deviceData) {
            foreach (InteractableObject interactableObject in eventOneGrabbedInteractableObjects) {
                interactableObject.__OnEventOneStop (deviceData);
            }
            eventOneGrabbedInteractableObjects = new List<InteractableObject> ();
            //if (deviceData.grabbedInteractableObject) { 
            //    deviceData.grabbedInteractableObject.__OnEventOneStop(deviceData);
            //}
        }

        List<InteractableObject> eventTwoHoveredInteractableObjects = new List<InteractableObject> ();
        public void DoEventTwoHoveredObjectsStart (InputDeviceData deviceData) {
            foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
                interactableObject.__OnEventTwoStart (deviceData);
                eventTwoHoveredInteractableObjects.Add (interactableObject);
            }
        }
        public void DoEventTwoHoveredObjectsStop (InputDeviceData deviceData) {
            foreach (InteractableObject interactableObject in eventTwoHoveredInteractableObjects) {
                interactableObject.__OnEventTwoStop (deviceData);
            }
            eventTwoHoveredInteractableObjects = new List<InteractableObject> ();
            //foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
            //    interactableObject.__OnEventTwoStop(deviceData);
            //}
        }

        List<InteractableObject> eventTwoGrabbedInteractableObjects = new List<InteractableObject> ();
        public void DoEventTwoGrabbedObjectStart (InputDeviceData deviceData) {
            if (deviceData.grabbedInteractableObject) {
                deviceData.grabbedInteractableObject.__OnEventTwoStart (deviceData);
                eventTwoGrabbedInteractableObjects.Add (deviceData.grabbedInteractableObject);
            }
        }
        public void DoEventTwoGrabbedObjectStop (InputDeviceData deviceData) {
            foreach (InteractableObject interactableObject in eventTwoGrabbedInteractableObjects) {
                interactableObject.__OnEventTwoStop (deviceData);
            }
            eventTwoGrabbedInteractableObjects = new List<InteractableObject> ();
            //if (deviceData.grabbedInteractableObject) { 
            //    deviceData.grabbedInteractableObject.__OnEventTwoStop(deviceData);
            //}
        }

        List<InteractableObject> eventThreeHoveredInteractableObjects = new List<InteractableObject> ();
        public void DoEventThreeHoveredObjectsStart (InputDeviceData deviceData) {
            foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
                interactableObject.__OnEventThreeStart (deviceData);
                eventThreeHoveredInteractableObjects.Add (interactableObject);
            }
        }
        public void DoEventThreeHoveredObjectsStop (InputDeviceData deviceData) {
            foreach (InteractableObject interactableObject in eventThreeHoveredInteractableObjects) {
                interactableObject.__OnEventThreeStop (deviceData);
            }
            eventThreeHoveredInteractableObjects = new List<InteractableObject> ();
            //foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
            //    interactableObject.__OnEventThreeStop(deviceData);
            //}
        }

        List<InteractableObject> eventThreeGrabbedInteractableObjects = new List<InteractableObject> ();
        public void DoEventThreeGrabbedObjectStart (InputDeviceData deviceData) {
            if (deviceData.grabbedInteractableObject) {
                deviceData.grabbedInteractableObject.__OnEventThreeStart (deviceData);
                eventThreeGrabbedInteractableObjects.Add (deviceData.grabbedInteractableObject);
            }
        }
        public void DoEventThreeGrabbedObjectStop (InputDeviceData deviceData) {
            foreach (InteractableObject interactableObject in eventThreeGrabbedInteractableObjects) {
                interactableObject.__OnEventThreeStop (deviceData);
            }
            eventThreeGrabbedInteractableObjects = new List<InteractableObject> ();
            //if (deviceData.grabbedInteractableObject) { 
            //    deviceData.grabbedInteractableObject.__OnEventThreeStop(deviceData);
            //}
        }
        #endregion

        #region DYNAMIC CALLBACK SETUP
        /// <summary>
        /// dynamically add events based on event type and listener / used internally
        /// </summary>
        internal void AddListenerFromEvent (string eventName, UnityAction<InputDeviceData> listener) {
            var eventField = typeof (InputController).GetField (eventName);
            object fldVal = eventField.GetValue (this);
            MethodInfo invokeMethod = fldVal.GetType ().GetMethod ("AddListener", new Type[] { typeof (UnityAction<InputDeviceData>) });
            invokeMethod.Invoke (fldVal, new object[] { listener });
        }
        internal void AddListenerFromEvent (GenericEventType eventType, UnityAction<InputDeviceData> listener) {
            if (eventType != GenericEventType.undefined) {
                AddListenerFromEvent (eventType.ToString (), listener);
            }
        }
        //internal void AddListenerFromEvent(ViveEvent eventType, UnityAction<InputDeviceData> listener) {
        //    if (eventType != ViveEvent.undefined) {
        //        AddListenerFromEvent(eventType.ToString(), listener);
        //    }
        //}
        //internal void AddListenerFromEvent(GenericEvent eventType, UnityAction<InputDeviceData> listener) {
        //    if (eventType != GenericEvent.undefined) {
        //        AddListenerFromEvent(eventType.ToString(), listener);
        //    }
        //}
        //internal void AddListenerFromEvent(GenericEvent eventType, UnityAction<InputDeviceData> listener) {
        //    if (eventType != GenericEvent.undefined) {
        //        AddListenerFromEvent(eventType.ToString(), listener);
        //    }
        //}

        internal void RemoveListenerFromEvent (string eventName, UnityAction<InputDeviceData> listener) {
            var eventField = typeof (InputController).GetField (eventName);
            object fldVal = eventField.GetValue (this);
            MethodInfo invokeMethod = fldVal.GetType ().GetMethod ("RemoveListener", new Type[] { typeof (UnityAction<InputDeviceData>) });
            invokeMethod.Invoke (fldVal, new object[] { listener });
        }
        internal void RemoveListenerFromEvent (GenericEventType eventType, UnityAction<InputDeviceData> listener) {
            if (eventType != GenericEventType.undefined) {
                RemoveListenerFromEvent (eventType.ToString (), listener);
            }
        }
        //internal void RemoveListenerFromEvent(ViveEvent eventType, UnityAction<InputDeviceData> listener) {
        //    if (eventType != ViveEvent.undefined) {
        //        RemoveListenerFromEvent(eventType.ToString(), listener);
        //    }
        //}
        //internal void RemoveListenerFromEvent(GenericEvent eventType, UnityAction<InputDeviceData> listener) {
        //    if (eventType != GenericEvent.undefined) {
        //        RemoveListenerFromEvent(eventType.ToString(), listener);
        //    }
        //}
        //internal void RemoveListenerFromEvent(GenericEvent eventType, UnityAction<InputDeviceData> listener) {
        //    if (eventType != GenericEvent.undefined) {
        //        RemoveListenerFromEvent(eventType.ToString(), listener);
        //    }
        //}
        #endregion

        #region GENERIC
        public enum GenericEventType {
            undefined,
            mouseButton0Down = 100,
            mouseButton0Up,
            mouseButton0Pressed,
            mouseButton1Down,
            mouseButton1Up,
            mouseButton1Pressed,
            mouseButton2Down,
            mouseButton2Up,
            mouseButton2Pressed,

            keyboardButtonF1Down = 200,
            keyboardButtonF1Up,
            keyboardButtonF2Down,
            keyboardButtonF2Up,
            keyboardButtonF3Down,
            keyboardButtonF3Up,
            keyboardButtonF1AndF2Down,
            keyboardButtonF1AndF2Up,

            viveTriggerDown = 300,
            viveTriggerUp,
            viveTriggerPressed,
            viveTouchpadDown,
            viveTouchpadUp,
            viveTouchpadPressed,
            viveGripDown,
            viveGripUp,
            viveGripPressed,

            leapThumbFingerDownGestureStartEvent = 400,
            leapThumbFingerDownGestureStopEvent,
            leapThumbFingerDownGestureRepeatEvent,
            leapIndexFingerDownGestureStartEvent,
            leapIndexFingerDownGestureStopEvent,
            leapIndexFingerDownGestureRepeatEvent,
            leapRingFingerDownGestureStartEvent,
            leapRingFingerDownGestureStopEvent,
            leapRingFingerDownGestureRepeatEvent,

            leapThumbExtendedGestureStartEvent,
            leapThumbExtendedGestureStopEvent,
            //leapThumbExtendedGestureRepeatEvent,

            leapThumbHandDownGestureStartEvent = 430,
            leapThumbHandDownGestureStopEvent,
            leapThumbHandDownGestureRepeatEvent,

            leapIndexPointingIgnoreThumbGestureStartEvent,
            leapIndexPointingIgnoreThumbGestureStopEvent,
            leapIndexPointingIgnoreThumbGestureRepeatEvent,
            //leapIndexPointingTriggerGesturePrepareEvent,
            //leapIndexPointingTriggerGestureStartEvent,
            //leapIndexPointingTriggerGestureStopEvent,
            //leapIndexPointingTriggerGestureRepeatEvent,
            leapGrabGesturePrepareEvent,
            leapGrabGestureStartEvent,
            leapGrabGestureStopEvent,
            leapGrabGestureRepeatEvent,
            leapReleaseGesturePrepareEvent,
            leapReleaseGestureStartEvent,
            leapReleaseGestureStopEvent,
            leapReleaseGestureRepeatEvent,
            leapOpenGestureStartEvent,
            leapOpenGestureStopEvent,
            leapOpenGestureRepeatEvent,
            leapClosedGestureStartEvent,
            leapClosedGestureStopEvent,
            leapClosedGestureRepeatEvent,
        }

        [Header ("Ui raycasting")]
        public GenericEventType genericUiCastEnableEvent = GenericEventType.undefined;
        public GenericEventType genericUiCastDisableEvent = GenericEventType.undefined;

        [Header ("Physics Ray/Sphere-casting")]
        public GenericEventType genericPhysicsCastEnableEvent = GenericEventType.undefined;
        public GenericEventType genericPhysicsCastDisableEvent = GenericEventType.undefined;

        [Header ("Physics Ray/Sphere-casting")]
        public GenericEventType genericSetParabolaCastEvent = GenericEventType.undefined;
        public GenericEventType genericSetRayCastEvent = GenericEventType.undefined;
        public GenericEventType genericSetSphereCastEvent = GenericEventType.undefined;

        [Header ("Ui press Event")]
        public GenericEventType genericUiPressEvent = GenericEventType.undefined;
        public GenericEventType genericUiReleaseEvent = GenericEventType.undefined;

        [Header ("Use Event")]
        public GenericEventType genericUseStartEvent = GenericEventType.undefined;
        public GenericEventType genericUseStopEvent = GenericEventType.undefined;

        [Header ("Attach/Grab Event")]
        public GenericEventType genericAttachStartEvent = GenericEventType.undefined;
        public GenericEventType genericAttachStopEvent = GenericEventType.undefined;

        [Header ("Touch Event")]
        public GenericEventType genericTouchStartEvent = GenericEventType.undefined;
        public GenericEventType genericTouchStopEvent = GenericEventType.undefined;

        [Header ("Event One")]
        public GenericEventType genericEventOneHoveredObjectsStartEvent = GenericEventType.undefined;
        public GenericEventType genericEventOneHoveredObjectsStopEvent = GenericEventType.undefined;
        public GenericEventType genericEventOneGrabbedObjectStartEvent = GenericEventType.undefined;
        public GenericEventType genericEventOneGrabbedObjectStopEvent = GenericEventType.undefined;

        [Header ("Event Two")]
        public GenericEventType genericEventTwoHoveredObjectsStartEvent = GenericEventType.undefined;
        public GenericEventType genericEventTwoHoveredObjectsStopEvent = GenericEventType.undefined;
        public GenericEventType genericEventTwoGrabbedObjectStartEvent = GenericEventType.undefined;
        public GenericEventType genericEventTwoGrabbedObjectStopEvent = GenericEventType.undefined;

        [Header ("Event Three")]
        public GenericEventType genericEventThreeHoveredObjectsStartEvent = GenericEventType.undefined;
        public GenericEventType genericEventThreeHoveredObjectsStopEvent = GenericEventType.undefined;
        public GenericEventType genericEventThreeGrabbedObjectStartEvent = GenericEventType.undefined;
        public GenericEventType genericEventThreeGrabbedObjectStopEvent = GenericEventType.undefined;

        /// <summary>
        /// Add the Callbacks
        /// </summary>
        private void AddGenericCallbacks () {
            AddListenerFromEvent (genericUiCastEnableEvent, DoEnableUiCast);
            AddListenerFromEvent (genericUiCastDisableEvent, DoDisableUiCast);

            AddListenerFromEvent (genericPhysicsCastEnableEvent, DoEnablePhysicsCast);
            AddListenerFromEvent (genericPhysicsCastDisableEvent, DoDisablePhysicsCast);

            AddListenerFromEvent (genericSetParabolaCastEvent, DoSetParabolaCastPhysicsCast);
            AddListenerFromEvent (genericSetRayCastEvent, DoSetRayCastPhysicsCast);
            AddListenerFromEvent (genericSetSphereCastEvent, DoSetSphereCastPhysicsCast);

            AddListenerFromEvent (genericUiPressEvent, DoUiPressed);
            AddListenerFromEvent (genericUiReleaseEvent, DoUiReleased);

            AddListenerFromEvent (genericUseStartEvent, DoUseHoveredAndGrabbedObjectStart);
            AddListenerFromEvent (genericUseStopEvent, DoUseHoveredAndGrabbedObjectStop);

            AddListenerFromEvent (genericAttachStartEvent, DoGrabHoveredObjectStart);
            AddListenerFromEvent (genericAttachStopEvent, DoGrabGrabbedObjectStop);

            AddListenerFromEvent (genericTouchStartEvent, DoTouchHoveredAndGrabbedObjectStart);
            AddListenerFromEvent (genericTouchStopEvent, DoTouchHoveredAndGrabbedObjectStop);

            AddListenerFromEvent (genericEventOneHoveredObjectsStartEvent, DoEventOneHoveredObjectsStart);
            AddListenerFromEvent (genericEventOneHoveredObjectsStopEvent, DoEventOneHoveredObjectsStop);
            AddListenerFromEvent (genericEventOneGrabbedObjectStartEvent, DoEventOneGrabbedObjectStart);
            AddListenerFromEvent (genericEventOneGrabbedObjectStopEvent, DoEventOneGrabbedObjectStop);

            AddListenerFromEvent (genericEventTwoHoveredObjectsStartEvent, DoEventTwoHoveredObjectsStart);
            AddListenerFromEvent (genericEventTwoHoveredObjectsStopEvent, DoEventTwoHoveredObjectsStop);
            AddListenerFromEvent (genericEventTwoGrabbedObjectStartEvent, DoEventTwoGrabbedObjectStart);
            AddListenerFromEvent (genericEventTwoGrabbedObjectStopEvent, DoEventTwoGrabbedObjectStop);

            AddListenerFromEvent (genericEventThreeHoveredObjectsStartEvent, DoEventThreeHoveredObjectsStart);
            AddListenerFromEvent (genericEventThreeHoveredObjectsStopEvent, DoEventThreeHoveredObjectsStop);
            AddListenerFromEvent (genericEventThreeGrabbedObjectStartEvent, DoEventThreeGrabbedObjectStart);
            AddListenerFromEvent (genericEventThreeGrabbedObjectStopEvent, DoEventThreeGrabbedObjectStop);
        }

        /// <summary>
        /// Remove the Callbacks
        /// </summary>
        private void RemoveGenericCallbacks () {
            RemoveListenerFromEvent (genericUiCastEnableEvent, DoEnableUiCast);
            RemoveListenerFromEvent (genericUiCastDisableEvent, DoDisableUiCast);

            RemoveListenerFromEvent (genericPhysicsCastEnableEvent, DoEnablePhysicsCast);
            RemoveListenerFromEvent (genericPhysicsCastDisableEvent, DoDisablePhysicsCast);

            RemoveListenerFromEvent (genericSetParabolaCastEvent, DoSetParabolaCastPhysicsCast);
            RemoveListenerFromEvent (genericSetRayCastEvent, DoSetRayCastPhysicsCast);
            RemoveListenerFromEvent (genericSetSphereCastEvent, DoSetSphereCastPhysicsCast);

            RemoveListenerFromEvent (genericUiPressEvent, DoUiPressed);
            RemoveListenerFromEvent (genericUiReleaseEvent, DoUiReleased);

            RemoveListenerFromEvent (genericUseStartEvent, DoUseHoveredAndGrabbedObjectStart);
            RemoveListenerFromEvent (genericUseStopEvent, DoUseHoveredAndGrabbedObjectStop);

            RemoveListenerFromEvent (genericAttachStartEvent, DoGrabHoveredObjectStart);
            RemoveListenerFromEvent (genericAttachStopEvent, DoGrabGrabbedObjectStop);

            RemoveListenerFromEvent (genericTouchStartEvent, DoTouchHoveredAndGrabbedObjectStart);
            RemoveListenerFromEvent (genericTouchStopEvent, DoTouchHoveredAndGrabbedObjectStop);

            RemoveListenerFromEvent (genericEventOneHoveredObjectsStartEvent, DoEventOneHoveredObjectsStart);
            RemoveListenerFromEvent (genericEventOneHoveredObjectsStopEvent, DoEventOneHoveredObjectsStop);
            RemoveListenerFromEvent (genericEventOneGrabbedObjectStartEvent, DoEventOneGrabbedObjectStart);
            RemoveListenerFromEvent (genericEventOneGrabbedObjectStopEvent, DoEventOneGrabbedObjectStop);

            RemoveListenerFromEvent (genericEventTwoHoveredObjectsStartEvent, DoEventTwoHoveredObjectsStart);
            RemoveListenerFromEvent (genericEventTwoHoveredObjectsStopEvent, DoEventTwoHoveredObjectsStop);
            RemoveListenerFromEvent (genericEventTwoGrabbedObjectStartEvent, DoEventTwoGrabbedObjectStart);
            RemoveListenerFromEvent (genericEventTwoGrabbedObjectStopEvent, DoEventTwoGrabbedObjectStop);

            RemoveListenerFromEvent (genericEventThreeHoveredObjectsStartEvent, DoEventThreeHoveredObjectsStart);
            RemoveListenerFromEvent (genericEventThreeHoveredObjectsStopEvent, DoEventThreeHoveredObjectsStop);
            RemoveListenerFromEvent (genericEventThreeGrabbedObjectStartEvent, DoEventThreeGrabbedObjectStart);
            RemoveListenerFromEvent (genericEventThreeGrabbedObjectStopEvent, DoEventThreeGrabbedObjectStop);
        }

        /// <summary>
        /// run the events that are repeated based on lists of objects we store
        /// </summary>
        private void UpdateRepeatEvents () {
            DoGrabRepeat (inputDevice.deviceData);
            DoUseRepeat (inputDevice.deviceData);
            DoTouchRepeat (inputDevice.deviceData);
        }

        /// <summary>
        /// search for a child with the name under root
        /// </summary>
        protected Transform FindChildNamed (Transform root, string name) {
            foreach (Transform t in root.GetComponentsInChildren<Transform> ()) {
                if (t.name == name)
                    return t;
            }
            Debug.LogWarning ("InputController.FindChildNamed: no child named " + name + " under " + root.name);
            return null;
        }
        #endregion

        #region GENERIC EVENT GENERATION
        public UnityDeviceDataEvent touchDown = new UnityDeviceDataEvent();
        public UnityDeviceDataEvent touchUp = new UnityDeviceDataEvent();
        public UnityDeviceDataEvent touchPressed = new UnityDeviceDataEvent();
        public UnityDeviceDataEvent grabDown = new UnityDeviceDataEvent();
        public UnityDeviceDataEvent grabUp = new UnityDeviceDataEvent();
        public UnityDeviceDataEvent grabPressed = new UnityDeviceDataEvent();
        public UnityDeviceDataEvent useDown = new UnityDeviceDataEvent();
        public UnityDeviceDataEvent useUp = new UnityDeviceDataEvent();
        public UnityDeviceDataEvent usePressed = new UnityDeviceDataEvent();
        #endregion

        #region MOUSE EVENT GENERATION
        /// <summary>
        /// the event buttons from the mouse
        /// </summary>
        public UnityDeviceDataEvent mouseButton0Down = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent mouseButton0Up = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent mouseButton0Pressed = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent mouseButton1Down = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent mouseButton1Up = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent mouseButton1Pressed = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent mouseButton2Down = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent mouseButton2Up = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent mouseButton2Pressed = new UnityDeviceDataEvent ();

        private void UpdateMouseInput () {
            //Debug.Log("InputController.UpdateMouseInput");
            inputDevice.deviceData.inputValueX = Input.mouseScrollDelta.x;
            inputDevice.deviceData.inputValueY = Input.mouseScrollDelta.y;
            // mouse button down
            if (Input.GetMouseButtonDown (0)) {
                mouseButton0Down.Invoke (inputDevice.deviceData);
            }
            if (Input.GetMouseButtonDown (1)) {
                mouseButton1Down.Invoke (inputDevice.deviceData);
            }
            if (Input.GetMouseButtonDown (2)) {
                mouseButton2Down.Invoke (inputDevice.deviceData);
            }
            // mouse button up
            if (Input.GetMouseButtonUp (0)) {
                mouseButton0Up.Invoke (inputDevice.deviceData);
            }
            if (Input.GetMouseButtonUp (1)) {
                mouseButton1Up.Invoke (inputDevice.deviceData);
            }
            if (Input.GetMouseButtonUp (2)) {
                mouseButton2Up.Invoke (inputDevice.deviceData);
            }
            // mouse button pressed
            if (Input.GetMouseButton (0)) {
                mouseButton0Pressed.Invoke (inputDevice.deviceData);
            }
            if (Input.GetMouseButton (1)) {
                mouseButton1Pressed.Invoke (inputDevice.deviceData);
            }
            if (Input.GetMouseButton (2)) {
                mouseButton2Pressed.Invoke (inputDevice.deviceData);
            }
        }
        #endregion

        #region VIVE EVENT GENERATION
        private void OnViveEnabled () {

        }

        private void OnViveDisabled () {
            viveTriggerUp.Invoke (inputDevice.deviceData);
            viveTouchpadUp.Invoke (inputDevice.deviceData);
            viveGripUp.Invoke (inputDevice.deviceData);
        }

        [Header ("Custom (direct trigger events)")]
        public UnityDeviceDataEvent viveTriggerDown = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent viveTriggerUp = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent viveTriggerPressed = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent viveTriggerTouched = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent viveTriggerTouchUp = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent viveTouchpadDown = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent viveTouchpadUp = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent viveTouchpadPressed = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent viveGripDown = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent viveGripUp = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent viveGripPressed = new UnityDeviceDataEvent ();

        private void UpdateViveInput () {
#if NETXR_STEAMVR_ACTIVE
            if (this is ViveInputController) {
                ViveInputController viveController = (ViveInputController) this;
                if ((__controllerInitialized) && (viveController.steamController == null)) {
                    Debug.LogError ("InputController.UpdateViveInput: viveController.steamController not defined!");
                }

                // trigger
                if (viveController.steamController.GetPressDown (EVRButtonId.k_EButton_SteamVR_Trigger)) {
                    Vector2 trigger = viveController.steamController.GetAxis (EVRButtonId.k_EButton_SteamVR_Trigger);
                    inputDevice.deviceData.inputValueX = trigger.x;
                    inputDevice.deviceData.inputValueY = 0;
                    viveTriggerDown.Invoke (inputDevice.deviceData);
                }
                if (viveController.steamController.GetPressUp (EVRButtonId.k_EButton_SteamVR_Trigger)) {
                    Vector2 trigger = viveController.steamController.GetAxis (EVRButtonId.k_EButton_SteamVR_Trigger);
                    inputDevice.deviceData.inputValueX = trigger.x;
                    inputDevice.deviceData.inputValueY = 0;
                    viveTriggerUp.Invoke (inputDevice.deviceData);
                }
                if (viveController.steamController.GetPress (EVRButtonId.k_EButton_SteamVR_Trigger)) {
                    Vector2 trigger = viveController.steamController.GetAxis (EVRButtonId.k_EButton_SteamVR_Trigger);
                    inputDevice.deviceData.inputValueX = trigger.x;
                    inputDevice.deviceData.inputValueY = 0;
                    //viveTriggerPressed.Invoke(inputDevice.deviceData, trigger[0]);
                    viveTriggerPressed.Invoke (inputDevice.deviceData);
                }
                if (viveController.steamController.GetTouch (EVRButtonId.k_EButton_SteamVR_Trigger)) {
                    Vector2 trigger = viveController.steamController.GetAxis (EVRButtonId.k_EButton_SteamVR_Trigger);
                    inputDevice.deviceData.inputValueX = trigger.x;
                    inputDevice.deviceData.inputValueY = 0;
                    //viveTriggerPressed.Invoke(inputDevice.deviceData, trigger[0]);
                    viveTriggerTouched.Invoke (inputDevice.deviceData);
                }
                if (viveController.steamController.GetTouchUp (EVRButtonId.k_EButton_SteamVR_Trigger)) {
                    Vector2 trigger = viveController.steamController.GetAxis (EVRButtonId.k_EButton_SteamVR_Trigger);
                    inputDevice.deviceData.inputValueX = trigger.x;
                    inputDevice.deviceData.inputValueY = 0;
                    //viveTriggerPressed.Invoke(inputDevice.deviceData, trigger[0]);
                    viveTriggerTouchUp.Invoke (inputDevice.deviceData);
                }
                // touchpad
                if (viveController.steamController.GetPressDown (EVRButtonId.k_EButton_SteamVR_Touchpad)) {
                    //Read the touchpad values
                    Vector2 touchpad = viveController.steamController.GetAxis (EVRButtonId.k_EButton_SteamVR_Touchpad);
                    inputDevice.deviceData.inputValueX = touchpad.x;
                    inputDevice.deviceData.inputValueY = touchpad.y;
                    viveTouchpadDown.Invoke (inputDevice.deviceData);
                    //viveTouchpadDown.Invoke(inputDevice.deviceData, touchpad);
                }
                if (viveController.steamController.GetPressUp (EVRButtonId.k_EButton_SteamVR_Touchpad)) {
                    //Read the touchpad values
                    Vector2 touchpad = viveController.steamController.GetAxis (EVRButtonId.k_EButton_SteamVR_Touchpad);
                    inputDevice.deviceData.inputValueX = touchpad.x;
                    inputDevice.deviceData.inputValueY = touchpad.y;
                    viveTouchpadUp.Invoke (inputDevice.deviceData);
                    //viveTouchpadUp.Invoke(inputDevice.deviceData, touchpad);
                }
                if (viveController.steamController.GetPress (EVRButtonId.k_EButton_SteamVR_Touchpad)) {
                    //Read the touchpad values
                    Vector2 touchpad = viveController.steamController.GetAxis (EVRButtonId.k_EButton_SteamVR_Touchpad);
                    inputDevice.deviceData.inputValueX = touchpad.x;
                    inputDevice.deviceData.inputValueY = touchpad.y;
                    //viveTouchpadPressed.Invoke(inputDevice.deviceData, touchpad);
                    viveTouchpadPressed.Invoke (inputDevice.deviceData);
                }
                // grip
                if (viveController.steamController.GetPressDown (EVRButtonId.k_EButton_Grip)) {
                    viveGripDown.Invoke (inputDevice.deviceData);
                }
                if (viveController.steamController.GetPressUp (EVRButtonId.k_EButton_Grip)) {
                    viveGripUp.Invoke (inputDevice.deviceData);
                }
                if (viveController.steamController.GetPress (EVRButtonId.k_EButton_Grip)) {
                    viveGripPressed.Invoke (inputDevice.deviceData);
                }
            }
#endif
        }
        #endregion

        #region LEAP EVENT GENERATION
        internal bool thumbFingerDownState = false;
        internal float lastThumbFingerDownState = 0;
        public UnityDeviceDataEvent leapThumbFingerDownGestureStartEvent;
        public UnityDeviceDataEvent leapThumbFingerDownGestureStopEvent;
        public UnityDeviceDataEvent leapThumbFingerDownGestureRepeatEvent;

        internal bool indexFingerDownState = false;
        internal float lastIndexFingerDownState = 0;
        public UnityDeviceDataEvent leapIndexFingerDownGestureStartEvent;
        public UnityDeviceDataEvent leapIndexFingerDownGestureStopEvent;
        public UnityDeviceDataEvent leapIndexFingerDownGestureRepeatEvent;

        internal bool ringFingerDownState = false;
        internal float lastRingFingerDownState = 0;
        public UnityDeviceDataEvent leapRingFingerDownGestureStartEvent;
        public UnityDeviceDataEvent leapRingFingerDownGestureStopEvent;
        public UnityDeviceDataEvent leapRingFingerDownGestureRepeatEvent;

        internal bool thumbHandDownState = false;
        public UnityDeviceDataEvent leapThumbHandDownGestureStartEvent;
        public UnityDeviceDataEvent leapThumbHandDownGestureStopEvent;
        public UnityDeviceDataEvent leapThumbHandDownGestureRepeatEvent;

        //internal bool indexPointingState = false;
        //private float lastIndexPointingState = 0; // index open, others closed, last time was the case
        internal bool indexIgnoreThumbPointingState = false;
        internal float lastIndexIgnoreThumbPointingState = 0;
        public UnityDeviceDataEvent leapIndexPointingIgnoreThumbGestureStartEvent;
        public UnityDeviceDataEvent leapIndexPointingIgnoreThumbGestureStopEvent;
        public UnityDeviceDataEvent leapIndexPointingIgnoreThumbGestureRepeatEvent;

        //internal GestureState indexPointingTriggerState = GestureState.undefined;
        //internal bool indexThumbPointingState = false;
        //internal float lastIndexThumbPointingState = 0; // thumb and index open, others closed, last time was the case
        //public UnityDeviceDataEvent leapIndexPointingTriggerGesturePrepareEvent;
        //public UnityDeviceDataEvent leapIndexPointingTriggerGestureStartEvent;
        //public UnityDeviceDataEvent leapIndexPointingTriggerGestureRepeatEvent;
        //public UnityDeviceDataEvent leapIndexPointingTriggerGestureStopEvent;

        //internal bool thumbExtendedState = false;
        //internal float lastThumbExtendedState = 0;
        //public UnityDeviceDataEvent leapThumbExtendedGestureStartEvent;
        //public UnityDeviceDataEvent leapThumbExtendedGestureStopEvent;
        //public UnityDeviceDataEvent leapThumbExtendedGestureRepeatEvent;

        internal GestureState handGrabGestureState = GestureState.undefined;
        public UnityDeviceDataEvent leapGrabGesturePrepareEvent;
        public UnityDeviceDataEvent leapGrabGestureStartEvent;
        public UnityDeviceDataEvent leapGrabGestureStopEvent;
        public UnityDeviceDataEvent leapGrabGestureRepeatEvent;

        internal GestureState handReleaseGestureState = GestureState.undefined;
        public UnityDeviceDataEvent leapReleaseGesturePrepareEvent;
        public UnityDeviceDataEvent leapReleaseGestureStartEvent;
        public UnityDeviceDataEvent leapReleaseGestureStopEvent;
        public UnityDeviceDataEvent leapReleaseGestureRepeatEvent;

        internal bool openGestureState = false;
        public UnityDeviceDataEvent leapOpenGestureStartEvent;
        public UnityDeviceDataEvent leapOpenGestureStopEvent;
        public UnityDeviceDataEvent leapOpenGestureRepeatEvent;
        internal bool closedGestureState = false;
#if WSNIO_LEAP_ACTIVE
        private float lastClosedGestureTime = 0; // used for grab & release gesture
        private float lastOpenGestureTime = 0; // used for grab & release gesture
#endif
        public UnityDeviceDataEvent leapClosedGestureStartEvent;
        public UnityDeviceDataEvent leapClosedGestureStopEvent;
        public UnityDeviceDataEvent leapClosedGestureRepeatEvent;

        internal enum GestureState {
            undefined,
            prepared,
            repeating,
        }

        private void OnLeapEnabled () {

        }

        private void OnLeapDisabled () {
            thumbHandDownState = false;
            //leapThumbDownGestureStopEvent.Invoke(inputDevice.deviceData);
            //indexPointingState = false;
            //leapIndexPointingGestureStopEvent.Invoke(inputDevice.deviceData);
            handGrabGestureState = GestureState.undefined;
            leapGrabGestureStopEvent.Invoke (inputDevice.deviceData);
            handReleaseGestureState = GestureState.undefined;
            leapReleaseGestureStopEvent.Invoke (inputDevice.deviceData);
            openGestureState = false;
            leapOpenGestureStopEvent.Invoke (inputDevice.deviceData);
            closedGestureState = false;
            leapClosedGestureStopEvent.Invoke (inputDevice.deviceData);
        }

        // private float gestureChangeTime = 0.2f;
        private void UpdateLeapInput () {
#if WSNIO_LEAP_ACTIVE
            if (this is LeapInputController) {
                LeapInputController leapController = (LeapInputController) this;
                //Debug.Log("LeapInputController.UpdateGesturesCoroutine: update");

                if (leapController.iHandModel != null) {
                    Hand hand = leapController.iHandModel.GetLeapHand ();
                    //Debug.Log("LeapInputController.UpdateGesturesCoroutine: iHandModel ok");
                    if (hand != null) {
                        #region LEAP FINGER STATE DETECTION
                        //Debug.Log("LeapInputController.UpdateGesturesCoroutine: hand ok");
                        FingerPointingUpdate ();
                        bool[] fingerExtendedState = new bool[] {
                            hand.Fingers[0].IsExtended,
                            hand.Fingers[1].IsExtended,
                            hand.Fingers[2].IsExtended,
                            hand.Fingers[3].IsExtended,
                            hand.Fingers[4].IsExtended
                        };
                        bool allFingersExtended = fingerExtendedState.All (t => t);
                        bool allFingersRetracted = fingerExtendedState.All (t => !t);
                        if (allFingersExtended) {
                            lastOpenGestureTime = Time.realtimeSinceStartup;
                        }
                        if (allFingersRetracted) {
                            lastClosedGestureTime = Time.realtimeSinceStartup;
                        }
                        #endregion

                        #region LEAP INDEX POINTING
                        // last time thumb and index were extended
                        //bool newIndexThumbPointingState = (fingerExtendedState[0] &&
                        //    fingerExtendedState[1] &&
                        //    !fingerExtendedState[2] &&
                        //    !fingerExtendedState[3] &&
                        //    !fingerExtendedState[4]);
                        //if (newIndexThumbPointingState)
                        //    lastIndexThumbPointingState = Time.realtimeSinceStartup;

                        // last time only index was extended
                        //bool newIndexPointingState = (!fingerExtendedState[0] &&
                        //    fingerExtendedState[1] &&
                        //    !fingerExtendedState[2] &&
                        //    !fingerExtendedState[3] &&
                        //    !fingerExtendedState[4]);
                        //if (newIndexPointingState)
                        //    lastIndexPointingState = Time.realtimeSinceStartup;

                        // last time only index was extended, ignoring the thumb
                        bool newIndexIgnoreThumbPointingState = (fingerExtendedState[1] &&
                            !fingerExtendedState[2] &&
                            !fingerExtendedState[3] &&
                            !fingerExtendedState[4]);
                        if (newIndexIgnoreThumbPointingState) {
                            lastIndexIgnoreThumbPointingState = Time.realtimeSinceStartup;
                        }
                        if (indexIgnoreThumbPointingState != newIndexIgnoreThumbPointingState) {
                            indexIgnoreThumbPointingState = newIndexIgnoreThumbPointingState;
                            if (indexIgnoreThumbPointingState) {
                                leapIndexPointingIgnoreThumbGestureStartEvent.Invoke (inputDevice.deviceData);
                            } else {
                                leapIndexPointingIgnoreThumbGestureStopEvent.Invoke (inputDevice.deviceData);
                            }
                        } else if ((newIndexIgnoreThumbPointingState == indexIgnoreThumbPointingState) && indexIgnoreThumbPointingState) {
                            leapIndexPointingIgnoreThumbGestureRepeatEvent.Invoke (inputDevice.deviceData);
                        }
                        //bool thumbIgnoreIndexPointingState = newIndexPointingState || newIndexThumbPointingState;
                        //if (indexPointingState != thumbIgnoreIndexPointingState) {
                        //    indexPointingState = thumbIgnoreIndexPointingState;
                        //    if (indexPointingState) {
                        //        leapIndexPointingGestureStartEvent.Invoke(inputDevice.deviceData);
                        //    } else {
                        //        leapIndexPointingGestureStopEvent.Invoke(inputDevice.deviceData);
                        //    }
                        //} else if ((thumbIgnoreIndexPointingState == indexPointingState) && indexPointingState) {
                        //    leapIndexPointingGestureRepeatEvent.Invoke(inputDevice.deviceData);
                        //}
                        #endregion

                        #region thumb finger  down/extended
                        bool newThumbFingerDownState = fingerRetracted[0]; // !fingerExtendedState[0];
                        if (newThumbFingerDownState) {
                            lastThumbFingerDownState = Time.realtimeSinceStartup;
                        }
                        if (thumbFingerDownState != newThumbFingerDownState) {
                            thumbFingerDownState = newThumbFingerDownState;
                            if (thumbFingerDownState) {
                                leapThumbFingerDownGestureStartEvent.Invoke (inputDevice.deviceData);
                            } else {
                                leapThumbFingerDownGestureStopEvent.Invoke (inputDevice.deviceData);
                            }
                        } else if ((thumbFingerDownState == newThumbFingerDownState) && thumbFingerDownState) {
                            leapThumbFingerDownGestureRepeatEvent.Invoke (inputDevice.deviceData);
                        }
                        #endregion

                        #region index finger down/extended
                        bool newIndexFingerDownState = fingerRetracted[1]; // !fingerExtendedState[1];
                        if (newIndexFingerDownState) {
                            lastIndexFingerDownState = Time.realtimeSinceStartup;
                        }
                        if (indexFingerDownState != newIndexFingerDownState) {
                            indexFingerDownState = newIndexFingerDownState;
                            if (indexFingerDownState) {
                                leapIndexFingerDownGestureStartEvent.Invoke (inputDevice.deviceData);
                            } else {
                                leapIndexFingerDownGestureStopEvent.Invoke (inputDevice.deviceData);
                            }
                        } else if ((indexFingerDownState == newIndexFingerDownState) && indexFingerDownState) {
                            leapIndexFingerDownGestureRepeatEvent.Invoke (inputDevice.deviceData);
                        }
                        #endregion

                        #region ring finger down/extended
                        bool newRingFingerDownState = fingerRetracted[2] && fingerRetracted[3] && fingerRetracted[4]; // !fingerExtendedState[4];
                        if (newRingFingerDownState) {
                            lastRingFingerDownState = Time.realtimeSinceStartup;
                        }
                        if (ringFingerDownState != newRingFingerDownState) {
                            ringFingerDownState = newRingFingerDownState;
                            if (ringFingerDownState) {
                                leapRingFingerDownGestureStartEvent.Invoke (inputDevice.deviceData);
                            } else {
                                leapRingFingerDownGestureStopEvent.Invoke (inputDevice.deviceData);
                            }
                        } else if ((ringFingerDownState == newRingFingerDownState) && ringFingerDownState) {
                            leapRingFingerDownGestureRepeatEvent.Invoke (inputDevice.deviceData);
                        }
                        #endregion

                        #region LEAP THUMB DOWN GESTURE
                        // thumb down gesture
                        bool newThumbHandDownState =
                            fingerPointingCameraDown[0] &&
                            fingerExtendedState[0] &&
                            !fingerExtendedState[1] &&
                            !fingerExtendedState[2] &&
                            !fingerExtendedState[3] &&
                            !fingerExtendedState[4];
                        //Debug.Log("LeapThumbDown: " + fingerPointingCameraDown[0] + " " + fingerExtendedState[0]);
                        if (thumbHandDownState != newThumbHandDownState) {
                            thumbHandDownState = newThumbHandDownState;
                            if (thumbHandDownState) {
                                leapThumbHandDownGestureStartEvent.Invoke (inputDevice.deviceData);
                            } else {
                                leapThumbHandDownGestureStopEvent.Invoke (inputDevice.deviceData);
                            }
                        } else if ((newThumbHandDownState == thumbHandDownState) && thumbHandDownState) {
                            leapThumbHandDownGestureRepeatEvent.Invoke (inputDevice.deviceData);
                        }
                        #endregion

                        #region LEAP INDEX DOWN GESTURE

                        #endregion

                        #region LEAP OPEN GESTURE
                        // open gesture
                        bool newOpenGestureState = allFingersExtended;
                        if (openGestureState != newOpenGestureState) {
                            openGestureState = newOpenGestureState;
                            if (openGestureState) {
                                leapOpenGestureStartEvent.Invoke (inputDevice.deviceData);
                            } else {
                                leapOpenGestureStopEvent.Invoke (inputDevice.deviceData);
                            }
                        } else if ((openGestureState != newOpenGestureState) && openGestureState) {
                            leapOpenGestureRepeatEvent.Invoke (inputDevice.deviceData);
                        }
                        #endregion

                        #region LEAP CLOSED GESTURE
                        // closed gesture
                        bool newClosedGestureState = allFingersRetracted;
                        if (closedGestureState != newClosedGestureState) {
                            closedGestureState = newClosedGestureState;
                            if (closedGestureState) {
                                leapClosedGestureStartEvent.Invoke (inputDevice.deviceData);
                            } else {
                                leapClosedGestureStopEvent.Invoke (inputDevice.deviceData);
                            }
                        } else if ((closedGestureState != newClosedGestureState) && closedGestureState) {
                            leapClosedGestureRepeatEvent.Invoke (inputDevice.deviceData);
                        }
                        #endregion

                        //private GestureState indexPointingTrigger = GestureState.undefined;
                        //public UnityDeviceDataEvent leapIndexPointingTriggerGesturePrepareEvent;
                        //public UnityDeviceDataEvent leapIndexPointingTriggerGestureStartEvent;
                        //public UnityDeviceDataEvent leapIndexPointingTriggerGestureRepeatEvent;
                        //public UnityDeviceDataEvent leapIndexPointingTriggerGestureStopEvent;

                        #region LEAP INDEX POINTING AND TRIGGERING
                        //switch (indexPointingTriggerState) {
                        //    case GestureState.undefined:
                        //        // thumb and index streched, all others closed
                        //        //if (newIndexThumbPointingState || newIndexPointingState) {
                        //        if (indexIgnoreThumbPointingState) {
                        //            leapIndexPointingTriggerGesturePrepareEvent.Invoke(inputDevice.deviceData);
                        //            indexPointingTriggerState = GestureState.prepared;
                        //        }
                        //        break;

                        //    case GestureState.prepared:
                        //        // index streched, all others closed within 0.1 seconds since indexThumb open state, we start
                        //        if (((Time.realtimeSinceStartup - lastIndexIgnoreThumbPointingState) < gestureChangeTime) && 
                        //            indexIgnoreThumbPointingState && fingerExtendedState[0]) {
                        //            leapIndexPointingTriggerGestureStartEvent.Invoke(inputDevice.deviceData);
                        //            indexPointingTriggerState = GestureState.repeating;
                        //        }
                        //        // keep this state
                        //        else if (indexIgnoreThumbPointingState) {
                        //        }
                        //        // abort after 0.1 seconds if changed, but not activated within this time
                        //        else if ((Time.realtimeSinceStartup - lastIndexIgnoreThumbPointingState) > gestureChangeTime) {
                        //            leapIndexPointingTriggerGestureStopEvent.Invoke(inputDevice.deviceData);
                        //            indexPointingTriggerState = GestureState.undefined;
                        //        }
                        //        break;

                        //    case GestureState.repeating:
                        //        // index streched, all others closed
                        //        if (indexIgnoreThumbPointingState) {
                        //            leapIndexPointingTriggerGestureRepeatEvent.Invoke(inputDevice.deviceData);
                        //        }
                        //        // thumb streched again, switch to prepared directly
                        //        else if (
                        //            ((Time.realtimeSinceStartup - lastIndexIgnoreThumbPointingState) < gestureChangeTime) &&
                        //            indexIgnoreThumbPointingState && fingerExtendedState[0]) {
                        //            //leapIndexPointingTriggerGestureStopEvent.Invoke(inputDevice.deviceData);
                        //            leapIndexPointingTriggerGesturePrepareEvent.Invoke(inputDevice.deviceData);
                        //            indexPointingTriggerState = GestureState.prepared;
                        //        }
                        //        // changed, but not switched within 0.1 seconds
                        //        else if (
                        //            ((Time.realtimeSinceStartup - lastIndexIgnoreThumbPointingState) > gestureChangeTime) &&
                        //            fingerExtendedState[0]) {
                        //            leapIndexPointingTriggerGestureStopEvent.Invoke(inputDevice.deviceData);
                        //            indexPointingTriggerState = GestureState.undefined;
                        //        }
                        //        break;
                        //}
                        #endregion

                        #region LEAP GRAB GESTURE
                        // grab gesture state machine
                        // check if all fingers from open, to closed state
                        switch (handGrabGestureState) {
                            case GestureState.undefined:
                                // we are prepared when all fingers are extended
                                if (allFingersExtended) {
                                    leapGrabGesturePrepareEvent.Invoke (inputDevice.deviceData);
                                    handGrabGestureState = GestureState.prepared;
                                }
                                //if (allFingersRetracted) {
                                //    leapGrabGesturePrepareEvent.Invoke(inputDevice.deviceData);
                                //    leapGrabGestureStartEvent.Invoke(inputDevice.deviceData);
                                //    handGrabGestureState = GestureState.repeating;
                                //}
                                break;
                            case GestureState.prepared:
                                // when all fingers are closed within 0.2 seconds since open state, we start
                                if (((Time.realtimeSinceStartup - lastOpenGestureTime) < gestureChangeTime) && (allFingersRetracted)) {
                                    leapGrabGestureStartEvent.Invoke (inputDevice.deviceData);
                                    handGrabGestureState = GestureState.repeating;
                                }
                                // keep this state
                                else if (allFingersExtended) { }
                                // abort, if more then 0.2s since open state
                                else if ((Time.realtimeSinceStartup - lastOpenGestureTime) > gestureChangeTime) {
                                    leapGrabGestureStopEvent.Invoke (inputDevice.deviceData); // maybe an abort event?
                                    handGrabGestureState = GestureState.undefined;
                                }
                                break;
                            case GestureState.repeating:
                                if (allFingersRetracted) {
                                    leapGrabGestureRepeatEvent.Invoke (inputDevice.deviceData);
                                } else {
                                    leapGrabGestureStopEvent.Invoke (inputDevice.deviceData);
                                    handGrabGestureState = GestureState.undefined;
                                }
                                break;
                        }
                        #endregion

                        #region LEAP HAND RELEASE GESTURE
                        // grab gesture state machine
                        // check if all fingers from open, to closed state
                        switch (handReleaseGestureState) {
                            case GestureState.undefined:
                                // we are prepared when all fingers are extended
                                if (allFingersRetracted) {
                                    leapReleaseGesturePrepareEvent.Invoke (inputDevice.deviceData);
                                    handReleaseGestureState = GestureState.prepared;
                                }
                                //if (allFingersExtended) {
                                //    leapReleaseGesturePrepareEvent.Invoke(inputDevice.deviceData);
                                //    leapReleaseGestureStartEvent.Invoke(inputDevice.deviceData);
                                //    handReleaseGestureState = GestureState.repeating;
                                //}
                                break;
                            case GestureState.prepared:
                                // when all fingers are closed within 0.2 seconds since open state, we start
                                if (((Time.realtimeSinceStartup - lastClosedGestureTime) < gestureChangeTime) && (allFingersExtended)) {
                                    leapReleaseGestureStartEvent.Invoke (inputDevice.deviceData);
                                    handReleaseGestureState = GestureState.repeating;
                                }
                                // keep this state
                                else if (allFingersRetracted) { }
                                // abort, if more then 0.2s since open state
                                else if ((Time.realtimeSinceStartup - lastClosedGestureTime) > gestureChangeTime) {
                                    leapReleaseGestureStopEvent.Invoke (inputDevice.deviceData); // maybe an abort event?
                                    handReleaseGestureState = GestureState.undefined;
                                }
                                //// when all fingers are closed we start
                                //if (allFingersExtended) {
                                //    LeapReleaseGestureStartEvent.Invoke();
                                //    handReleaseGestureState = GestureState.repeating;
                                //}
                                //// keep this state
                                //else if (allFingersRetracted) {
                                //}
                                //// abort, neighter all fingers extended nor retracted
                                //else {
                                //    LeapReleaseGestureStopEvent.Invoke(); // maybe an abort event?
                                //    handReleaseGestureState = GestureState.undefined;
                                //}
                                break;
                            case GestureState.repeating:
                                if (allFingersExtended) {
                                    leapReleaseGestureRepeatEvent.Invoke (inputDevice.deviceData);
                                } else {
                                    leapReleaseGestureStopEvent.Invoke (inputDevice.deviceData);
                                    handReleaseGestureState = GestureState.undefined;
                                }
                                break;
                        }
                        #endregion
                    }
                }
            }
#endif
        }

#if WSNIO_LEAP_ACTIVE
        private bool[] fingerPointingCameraDown = new bool[5];
        private bool[] fingerPointingCameraUp = new bool[5];
        private bool[] fingerRetracted = new bool[5];
        private float[] fingerRetractedTimer = new float[5];
#endif
        private void FingerPointingUpdate () {
#if WSNIO_LEAP_ACTIVE
            float OnDownAngle = 50;
            float OffDownAngle = 60;

            float OnDefaultRetractedAngle = 90;
            float OffDefaultRetractedAngle = 45;
            float OnThumbRetractedAngle = 60;
            float OffThumbRetractedAngle = 30;
            float retractedActivationDelay = 0.1f;
            for (int f = 0; f < 5; f++) {
                if (this is LeapInputController) {
                    LeapInputController leapController = (LeapInputController) this;
                    Hand hand = leapController.iHandModel.GetLeapHand ();
                    float angleToDown = 180;
                    float angleToUp = 180;
                    if (hand != null) {
                        Vector3 downDirection = selectedDirection (hand.Fingers[f].TipPosition.ToVector3 (), Vector3.down);
                        Vector3 upDirection = selectedDirection (hand.Fingers[f].TipPosition.ToVector3 (), Vector3.up);
                        Vector3 fingerDirection = hand.Fingers[f].Bone (Bone.BoneType.TYPE_DISTAL).Direction.ToVector3 ();
                        angleToDown = Vector3.Angle (fingerDirection, downDirection);
                        angleToUp = Vector3.Angle (fingerDirection, upDirection);
                    }
                    if (leapController.iHandModel.IsTracked && angleToDown <= OnDownAngle) {
                        fingerPointingCameraDown[f] = true;
                    } else if (!leapController.iHandModel.IsTracked || angleToDown >= OffDownAngle) {
                        fingerPointingCameraDown[f] = false;
                    }

                    if (leapController.iHandModel.IsTracked && angleToUp <= OnDownAngle) {
                        fingerPointingCameraUp[f] = true;
                    } else if (!leapController.iHandModel.IsTracked || angleToUp >= OffDownAngle) {
                        fingerPointingCameraUp[f] = false;
                    }

                    int rootBoneId = 1;
                    int tipBoneId = 3;
                    float OnRetractedAngle = OnDefaultRetractedAngle;
                    float OffRetractedAngle = OffDefaultRetractedAngle;
                    // different values for thumb
                    if (f == 0) {
                        OnRetractedAngle = OnThumbRetractedAngle;
                        OffRetractedAngle = OffThumbRetractedAngle;
                    }
                    float fingerRootTipAngle = Vector3.Angle (hand.Fingers[f].bones[rootBoneId].Direction.ToVector3 (), hand.Fingers[f].bones[tipBoneId].Direction.ToVector3 ());
                    if (leapController.iHandModel.IsTracked && fingerRootTipAngle >= OnRetractedAngle) {
                        fingerRetractedTimer[f] += Time.deltaTime;
                    } else if (!leapController.iHandModel.IsTracked || fingerRootTipAngle <= OffRetractedAngle) {
                        fingerRetractedTimer[f] = 0;
                    }
                    if (fingerRetractedTimer[f] > retractedActivationDelay) {
                        fingerRetracted[f] = true;
                    } else {
                        fingerRetracted[f] = false;
                    }
                    //Debug.Log("finger "+f+ " : " + fingerRetracted[f] + " : fingerRootTipAngle " + fingerRootTipAngle);
                }
            }
#endif
        }

        private Vector3 selectedDirection (Vector3 tipPosition, Vector3 PointingDirection) {
            return PlayerSettingsController.Instance.cameraInstance.transform.TransformDirection (PointingDirection);
            //return Camera.main.transform.TransformDirection(PointingDirection);
            /*switch (PalmDirectionDetector.PointingType) {
                case PointingType.RelativeToHorizon:
                    Quaternion cameraRot = Camera.main.transform.rotation;
                    float cameraYaw = cameraRot.eulerAngles.y;
                    Quaternion rotator = Quaternion.AngleAxis(cameraYaw, Vector3.up);
                    return rotator * PointingDirection;
                case PointingType.RelativeToCamera:
                    return Camera.main.transform.TransformDirection(PointingDirection);
                case PointingType.RelativeToWorld:
                    return PointingDirection;
                case PointingType.AtTarget:
                    return TargetObject.position - tipPosition;
                default:
                    return PointingDirection;
            }*/
        }
        #endregion

        #region KEYBOARD EVENT GENERATION
        /// <summary>
        /// the event buttons from the camera
        /// </summary>
        public UnityDeviceDataEvent keyboardButtonF1Down = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent keyboardButtonF1Up = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent keyboardButtonF1Pressed = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent keyboardButtonF2Down = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent keyboardButtonF2Up = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent keyboardButtonF2Pressed = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent keyboardButtonF3Down = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent keyboardButtonF3Up = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent keyboardButtonF3Pressed = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent keyboardButtonF1AndF2Down = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent keyboardButtonF1AndF2Up = new UnityDeviceDataEvent ();
        public UnityDeviceDataEvent keyboardButtonF1AndF2Pressed = new UnityDeviceDataEvent ();

        private bool currentf1AndF2Pressed = false;
        private bool prevf1AndF2Pressed = false;
        internal void UpdateKeyboardInput () {
            // button down
            if (Input.GetKeyDown (KeyCode.F1)) {
                keyboardButtonF1Down.Invoke (inputDevice.deviceData);
            }
            if (Input.GetKeyDown (KeyCode.F2)) {
                keyboardButtonF2Down.Invoke (inputDevice.deviceData);
            }
            if (Input.GetKeyDown (KeyCode.F3)) {
                keyboardButtonF3Down.Invoke (inputDevice.deviceData);
            }
            // button up
            if (Input.GetKeyUp (KeyCode.F1)) {
                keyboardButtonF1Up.Invoke (inputDevice.deviceData);
            }
            if (Input.GetKeyUp (KeyCode.F2)) {
                keyboardButtonF2Up.Invoke (inputDevice.deviceData);
            }
            if (Input.GetKeyUp (KeyCode.F3)) {
                keyboardButtonF3Up.Invoke (inputDevice.deviceData);
            }
            // button pressed
            if (Input.GetKey (KeyCode.F1)) {
                keyboardButtonF1Pressed.Invoke (inputDevice.deviceData);
            }
            if (Input.GetKey (KeyCode.F2)) {
                keyboardButtonF2Pressed.Invoke (inputDevice.deviceData);
            }
            if (Input.GetKey (KeyCode.F3)) {
                keyboardButtonF3Pressed.Invoke (inputDevice.deviceData);
            }

            // F1&F2 down, up, pressed
            currentf1AndF2Pressed = (Input.GetKey (KeyCode.F1) && Input.GetKey (KeyCode.F2));
            if (currentf1AndF2Pressed != prevf1AndF2Pressed) {
                if (currentf1AndF2Pressed) {
                    keyboardButtonF1AndF2Down.Invoke (inputDevice.deviceData);
                } else {
                    keyboardButtonF1AndF2Up.Invoke (inputDevice.deviceData);
                }
            } else {
                if (currentf1AndF2Pressed) {
                    keyboardButtonF1AndF2Pressed.Invoke (inputDevice.deviceData);
                }
            }
            prevf1AndF2Pressed = currentf1AndF2Pressed;
        }
        #endregion

        #region HAPTIC FEEDBACK
        internal float hapticPulseStrength = 0f;
        internal float hapticPulseLength = 0f;
        internal float hapticPulseStart = 0f;
        internal AnimationCurve hapticPulseAnimationCurve = null;
        public virtual bool HasHapticFeedback () {
            return true;
        }
        public virtual void EnableHapticFeedbackCurve (float strength, float length, AnimationCurve curve) {
            hapticPulseStrength = strength;
            hapticPulseLength = length;
            hapticPulseStart = Time.time;
            hapticPulseAnimationCurve = curve;
        }
        /// <summary>
        /// start a haptic pulse with strength 0..1 and duration in seconds
        /// </summary>
        /// <param name="strength"></param>
        /// <param name="length"></param>
        public virtual void EnableHapticFeedback (float strength, float length) {
            hapticPulseStrength = strength;
            hapticPulseLength = length;
            hapticPulseStart = Time.time;
            hapticPulseAnimationCurve = null;
        }
        /// <summary>
        /// force stop the haptic pulse immediately
        /// </summary>
        public virtual void DisableHapticFeedback () {
            hapticPulseStrength = 0f;
            hapticPulseLength = 0f;
            hapticPulseAnimationCurve = null;
        }
        /// <summary>
        /// handle haptic pulse
        /// </summary>
        internal virtual void UpdateHapticFeedback () {
            float pulseTime = Time.time - hapticPulseStart;
            if (pulseTime < hapticPulseLength) {
                // it's running
            } else {
                DisableHapticFeedback ();
            }
        }

        #endregion

        #region DEBUG
        public override string ToString () {
            string inputDeviceType = "undefined";
            if (this is MouseController) {
                inputDeviceType = "mouse";
            }
            if (this is LeapInputController) {
                inputDeviceType = "leap";
            }
            if (this is ViveInputController) {
                inputDeviceType = "vive";
            }
            return inputDeviceType + " " + gameObject.name;
        }
        #endregion

        #region SAMPLE
        /// <summary>
        /// JUST AN EXAMPLE HOW TO CALL A FUNCTION WITH A PARAMETER
        /// </summary>
        private void Testing () {
            viveTriggerPressed.AddListener ((InputDeviceData deviceData) => { Debug.Log ("InputController.Testing.viveTriggerPressed " + deviceData.uiHit + " " + deviceData.inputValueX + " " + gameObject.name); });
            viveTriggerPressed.AddListener (SampleViveTriggerPressed);
            viveTriggerDown.AddListener ((InputDeviceData deviceData) => { Debug.Log ("InputController.Testing.viveTriggerDown " + deviceData.uiHit + " " + gameObject.name); });
            viveTouchpadPressed.AddListener ((InputDeviceData deviceData) => { Debug.Log ("InputController.Testing.viveTouchpadPressed " + deviceData.uiHit + " " + deviceData.inputValueX + " " + gameObject.name); });
            mouseButton0Down.AddListener ((InputDeviceData deviceData) => { Debug.Log ("InputController.Testing.mouseButton0Down " + deviceData.uiHit + " " + gameObject.name); });

            if (this is LeapInputController) {
                leapThumbHandDownGestureStartEvent.AddListener ((InputDeviceData deviceData) => { Debug.Log ("LeapInputController.LeapThumbDownGestureStartEvent:"); });
                leapThumbHandDownGestureStopEvent.AddListener ((InputDeviceData deviceData) => { Debug.Log ("LeapInputController.LeapThumbDownGestureStopEvent:"); });
                leapThumbHandDownGestureRepeatEvent.AddListener ((InputDeviceData deviceData) => { Debug.Log ("LeapInputController.LeapThumbDownGestureRepeatEvent:"); });

                leapIndexPointingIgnoreThumbGestureStartEvent.AddListener ((InputDeviceData deviceData) => { Debug.Log ("LeapInputController.LeapIndexPointingGestureStartEvent:"); });
                leapIndexPointingIgnoreThumbGestureStopEvent.AddListener ((InputDeviceData deviceData) => { Debug.Log ("LeapInputController.LeapIndexPointingGestureStopEvent:"); });
                leapIndexPointingIgnoreThumbGestureRepeatEvent.AddListener ((InputDeviceData deviceData) => { Debug.Log ("LeapInputController.LeapIndexPointingGestureRepeatEvent:"); });

                leapGrabGestureStartEvent.AddListener ((InputDeviceData deviceData) => { Debug.Log ("LeapInputController.LeapGrabGestureStartEvent:"); });
                leapGrabGestureStopEvent.AddListener ((InputDeviceData deviceData) => { Debug.Log ("LeapInputController.LeapGrabGestureStopEvent:"); });
                leapGrabGestureRepeatEvent.AddListener ((InputDeviceData deviceData) => { Debug.Log ("LeapInputController.LeapGrabGestureRepeatEvent:"); });

                leapReleaseGestureStartEvent.AddListener ((InputDeviceData deviceData) => { Debug.Log ("LeapInputController.LeapReleaseGestureStartEvent:"); });
                leapReleaseGestureStopEvent.AddListener ((InputDeviceData deviceData) => { Debug.Log ("LeapInputController.LeapReleaseGestureStopEvent:"); });
                leapReleaseGestureRepeatEvent.AddListener ((InputDeviceData deviceData) => { Debug.Log ("LeapInputController.LeapReleaseGestureRepeatEvent:"); });
            }
        }
        private void Sample (InputDeviceData deviceData, string parameter) {
            Debug.Log ("InputController.Sample " + deviceData.uiHit + " " + parameter + " " + gameObject.name);
        }
        private void SampleViveTriggerPressed (InputDeviceData deviceData) {
            Debug.Log ("InputController.Testing.SampleViveTriggerPressed " + deviceData.uiHit + " " + deviceData.inputValueX + " " + gameObject.name);
        }
        #endregion
    }
}
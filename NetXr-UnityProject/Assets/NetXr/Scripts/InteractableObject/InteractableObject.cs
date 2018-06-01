//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System;

// must inherit from MonoBehaviour and iInteractableObject
// because this makes it very difficult to create the networkedInteractiableObject
// we instead inherit from NetworkBehaviour
namespace NetXr {

    #if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(InteractableObject))]
    public class InteractableObjectInspector : Editor {

        protected static bool showAttach = false;
        protected static bool showUse = false;
        protected static bool showTouch = false;
        protected static bool showNumbered = false;
        protected static bool showHover = false;
        protected static bool showDrag = false;
        protected static bool showDefault = false;
        protected static bool showCustomizeGrab = false;

        public override void OnInspectorGUI() {
            InteractableObject myTarget = (InteractableObject)target;
            this.serializedObject.Update();

            // draw all fields commonly used
            DrawDefaultFields(myTarget);

            // show the standard inspector for debugging
            showDefault = EditorGUILayout.Foldout(showDefault, "Default Inspector (Debugging)");
            if (showDefault) {
                DrawDefaultInspector();
            }

            this.serializedObject.ApplyModifiedProperties();
        }

        protected void DrawDefaultFields(InteractableObject myTarget) {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Drag Movement", GUILayout.Width(120));
            EditorGUILayout.LabelField("X", GUILayout.Width(15));
            myTarget.moveXByDrag = GUILayout.Toggle(myTarget.moveXByDrag, "", GUILayout.ExpandWidth(false));
            EditorGUILayout.LabelField("Y", GUILayout.Width(15));
            myTarget.moveYByDrag = GUILayout.Toggle(myTarget.moveYByDrag, "", GUILayout.ExpandWidth(false));
            EditorGUILayout.LabelField("Z", GUILayout.Width(15));
            myTarget.moveZByDrag = GUILayout.Toggle(myTarget.moveZByDrag, "", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Drag Rotation", GUILayout.Width(120));
            EditorGUILayout.LabelField("X", GUILayout.Width(15));
            myTarget.rotateXByDrag = GUILayout.Toggle(myTarget.rotateXByDrag, "", GUILayout.ExpandWidth(false));
            EditorGUILayout.LabelField("Y", GUILayout.Width(15));
            myTarget.rotateYByDrag = GUILayout.Toggle(myTarget.rotateYByDrag, "", GUILayout.ExpandWidth(false));
            EditorGUILayout.LabelField("Z", GUILayout.Width(15));
            myTarget.rotateZByDrag = GUILayout.Toggle(myTarget.rotateZByDrag, "", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            showAttach = EditorGUILayout.Foldout(showAttach, "Attach Events");
            if (showAttach) {
                EditorGUILayout.PropertyField(this.serializedObject.FindProperty("onAttachStartEvent"), true);
                EditorGUILayout.PropertyField(this.serializedObject.FindProperty("onAttachRepeatEvent"), true);
                EditorGUILayout.PropertyField(this.serializedObject.FindProperty("onAttachStopEvent"), true);
            }

            showUse = EditorGUILayout.Foldout(showUse, "Use Events");
            if (showUse) {
                EditorGUILayout.PropertyField(this.serializedObject.FindProperty("onUseStartEvent"), true);
                EditorGUILayout.PropertyField(this.serializedObject.FindProperty("onUseRepeatEvent"), true);
                EditorGUILayout.PropertyField(this.serializedObject.FindProperty("onUseStopEvent"), true);
            }

            showTouch = EditorGUILayout.Foldout(showTouch, "Touch Events");
            if (showTouch) {
                EditorGUILayout.PropertyField(this.serializedObject.FindProperty("onTouchStartEvent"), true);
                EditorGUILayout.PropertyField(this.serializedObject.FindProperty("onTouchRepeatEvent"), true);
                EditorGUILayout.PropertyField(this.serializedObject.FindProperty("onTouchStopEvent"), true);
            }

            showNumbered = EditorGUILayout.Foldout(showNumbered, "Numbered Events");
            if (showNumbered) {
                EditorGUILayout.PropertyField(this.serializedObject.FindProperty("onEventOneStartEvent"), true);
                EditorGUILayout.PropertyField(this.serializedObject.FindProperty("onEventOneStopEvent"), true);
                EditorGUILayout.PropertyField(this.serializedObject.FindProperty("onEventTwoStartEvent"), true);
                EditorGUILayout.PropertyField(this.serializedObject.FindProperty("onEventTwoStopEvent"), true);
                EditorGUILayout.PropertyField(this.serializedObject.FindProperty("onEventThreeStartEvent"), true);
                EditorGUILayout.PropertyField(this.serializedObject.FindProperty("onEventThreeStopEvent"), true);
            }

            showHover = EditorGUILayout.Foldout(showHover, "Hovered Events");
            if (showHover) {
                EditorGUILayout.PropertyField(this.serializedObject.FindProperty("onHoverEnterEvent"), true);
                EditorGUILayout.PropertyField(this.serializedObject.FindProperty("onHoverRepeatEvent"), true);
                EditorGUILayout.PropertyField(this.serializedObject.FindProperty("onHoverExitEvent"), true);
            }

            showDrag = EditorGUILayout.Foldout(showDrag, "Drag Events");
            if (showDrag) {
                EditorGUILayout.PropertyField(this.serializedObject.FindProperty("onDragEnterEvent"), true);
                EditorGUILayout.PropertyField(this.serializedObject.FindProperty("onDragRepeatEvent"), true);
                EditorGUILayout.PropertyField(this.serializedObject.FindProperty("onDragExitEvent"), true);
            }

            showCustomizeGrab = EditorGUILayout.Foldout(showCustomizeGrab, "Customize Drag");
            if (showCustomizeGrab) {
                myTarget.grabOnlyFirst = EditorGUILayout.Toggle("Grab only first InteractableObject", myTarget.grabOnlyFirst, GUILayout.Width(120));
                myTarget.disableDefaultApplyTransformEvent = EditorGUILayout.Toggle("Disable default Apply Transform Function", myTarget.disableDefaultApplyTransformEvent, GUILayout.Width(120));
                EditorGUILayout.PropertyField(this.serializedObject.FindProperty("onApplyTransformOverrideEvent"), true);
            }
        }
    }
    #endif

    public enum InteractableEventAction {
        undefined,
        grab,
        drop,
        debugLog
    }

    public class InteractableObject : NetworkBehaviour {
        public new Rigidbody rigidbody;

        // can this object be moved when dragged
        //public bool moveOnDrag = true;
        public bool moveXByDrag = true;
        public bool moveYByDrag = true;
        public bool moveZByDrag = true;
        public bool rotateXByDrag = true;
        public bool rotateYByDrag = true;
        public bool rotateZByDrag = true;

        public bool grabOnlyFirst = true;

        [NonSerialized]
        public List<InputDevice> hoveringDevices = new List<InputDevice>();
        [NonSerialized]
        public InputDevice grabbedByDevice = null;

        [Header("Attach Trigger Event Received")]
        public UnityDeviceDataEvent onAttachStartEvent = new UnityDeviceDataEvent();
        public UnityDeviceDataEvent onAttachRepeatEvent = new UnityDeviceDataEvent();
        public UnityDeviceDataEvent onAttachStopEvent = new UnityDeviceDataEvent();

        [Header("Use Trigger Event Received")]
        public UnityDeviceDataEvent onUseStartEvent = new UnityDeviceDataEvent();
        public UnityDeviceDataEvent onUseRepeatEvent = new UnityDeviceDataEvent();
        public UnityDeviceDataEvent onUseStopEvent = new UnityDeviceDataEvent();

        [Header("Touch Trigger Event Received")]
        public UnityDeviceDataEvent onTouchStartEvent = new UnityDeviceDataEvent();
        public UnityDeviceDataEvent onTouchRepeatEvent = new UnityDeviceDataEvent();
        public UnityDeviceDataEvent onTouchStopEvent = new UnityDeviceDataEvent();

        [Header("Event one Event Received")]
        public UnityDeviceDataEvent onEventOneStartEvent = new UnityDeviceDataEvent();
        public UnityDeviceDataEvent onEventOneStopEvent = new UnityDeviceDataEvent();

        [Header("Event two Event Received")]
        public UnityDeviceDataEvent onEventTwoStartEvent = new UnityDeviceDataEvent();
        public UnityDeviceDataEvent onEventTwoStopEvent = new UnityDeviceDataEvent();

        [Header("Event three Event Received")]
        public UnityDeviceDataEvent onEventThreeStartEvent = new UnityDeviceDataEvent();
        public UnityDeviceDataEvent onEventThreeStopEvent = new UnityDeviceDataEvent();

        [Header("Object Hovered Events")]
        public UnityDeviceDataEvent onHoverEnterEvent = new UnityDeviceDataEvent();
        public UnityDeviceDataEvent onHoverRepeatEvent = new UnityDeviceDataEvent();
        public UnityDeviceDataEvent onHoverExitEvent = new UnityDeviceDataEvent();

        [Header("Object Drag Events")]
        public UnityDeviceDataEvent onDragEnterEvent = new UnityDeviceDataEvent();
        public UnityDeviceDataEvent onDragRepeatEvent = new UnityDeviceDataEvent();
        public UnityDeviceDataEvent onDragExitEvent = new UnityDeviceDataEvent();

        [Header("Override the Apply Transform Event")]
        public bool disableDefaultApplyTransformEvent = false;
        public UnityDeviceDataEvent onApplyTransformOverrideEvent = new UnityDeviceDataEvent();

        protected virtual void Awake() {
            rigidbody = GetComponent<Rigidbody>();
        }

        protected virtual void Update() {
        }

        #region event handling
        /// <summary>
        /// activated when the use event is Triggered on the device
        /// </summary>
        internal virtual void __OnUseStart(InputDeviceData deviceData) {
            //ExecuteEvent(eventUseStartAction, deviceData);
            onUseStartEvent.Invoke(deviceData);
        }
        internal virtual void __OnUseRepeat(InputDeviceData deviceData) {
            onUseRepeatEvent.Invoke(deviceData);
        }
        internal virtual void __OnUseStop(InputDeviceData deviceData) {
            //ExecuteEvent(eventUseStopAction, deviceData);
            onUseStopEvent.Invoke(deviceData);
        }

        /// <summary>
        /// activated when the grab event is Triggered on the device
        /// </summary>
        internal virtual void __OnGrabStart(InputDeviceData deviceData) {
            //ExecuteEvent(eventGrabStartAction, deviceData);
            onAttachStartEvent.Invoke(deviceData);
        }
        internal virtual void __OnGrabRepeat(InputDeviceData deviceData) {
            onAttachRepeatEvent.Invoke(deviceData);
        }
        internal virtual void __OnGrabStop(InputDeviceData deviceData) {
            //ExecuteEvent(eventGrabStopAction, deviceData);
            onAttachStopEvent.Invoke(deviceData);
        }

        /// <summary>
        /// activated when the grab event is Triggered on the device
        /// </summary>
        internal virtual void __OnTouchStart(InputDeviceData deviceData) {
            //ExecuteEvent(eventTouchStartAction, deviceData);
            onTouchStartEvent.Invoke(deviceData);
        }
        internal virtual void __OnTouchRepeat(InputDeviceData deviceData) {
            onTouchRepeatEvent.Invoke(deviceData);
        }
        internal virtual void __OnTouchStop(InputDeviceData deviceData) {
            //ExecuteEvent(eventTouchStopAction, deviceData);
            onTouchStopEvent.Invoke(deviceData);
        }

        /// <summary>
        /// activated when the event one is Triggered on the device
        /// </summary>
        internal virtual void __OnEventOneStart(InputDeviceData deviceData) {
            //ExecuteEvent(eventOneStartAction, deviceData);
            onEventOneStartEvent.Invoke(deviceData);
        }
        internal virtual void __OnEventOneStop(InputDeviceData deviceData) {
            //ExecuteEvent(eventOneStopAction, deviceData);
            onEventOneStopEvent.Invoke(deviceData);
        }

        /// <summary>
        /// activated when the event two is Triggered on the device
        /// </summary>
        internal virtual void __OnEventTwoStart(InputDeviceData deviceData) {
            //ExecuteEvent(eventTwoStartAction, deviceData);
            onEventTwoStartEvent.Invoke(deviceData);
        }
        internal virtual void __OnEventTwoStop(InputDeviceData deviceData) {
            //ExecuteEvent(eventTwoStopAction, deviceData);
            onEventTwoStopEvent.Invoke(deviceData);
        }

        /// <summary>
        /// activated when the event tree is Triggered on the device
        /// </summary>
        internal virtual void __OnEventThreeStart(InputDeviceData deviceData) {
            //ExecuteEvent(eventThreeStartAction, deviceData);
            onEventThreeStartEvent.Invoke(deviceData);
        }
        internal virtual void __OnEventThreeStop(InputDeviceData deviceData) {
            //ExecuteEvent(eventThreeStopAction, deviceData);
            onEventThreeStopEvent.Invoke(deviceData);
        }
        #endregion

        #region controller funtions
        public virtual void DoAttachToController(InputDeviceData deviceData) {
            //Debug.Log("InteractableObject.DoAttachToController");
            if (grabOnlyFirst) {
                if (isFirstHovered(deviceData)) {
                    deviceData.inputDevice.AttachObject(this);
                } else {
                    // not attaching
                }
            } else {
                deviceData.inputDevice.AttachObject(this);
            }
        }
        public virtual void DoDetachFromController(InputDeviceData deviceData) {
            //Debug.Log("InteractableObject.DoDetachFromController");
            deviceData.inputDevice.DetachObject(this);
        }
        #endregion

        #region TOUCHING & GRABBING handling
        /// <summary>
        /// is this object touched by one or more input devcies
        /// </summary>
        protected bool isHovered {
            get { return (hoveringDevices.Count > 0); }
        }

        /// <summary>
        /// is this object grabbed by a device
        /// </summary>
        protected bool isGrabbed {
            get { return (grabbedByDevice != null); }
        }

        /// <summary>
        /// check if this interactableobject is the first in the hovered list
        /// </summary>
        protected bool isFirstHovered (InputDeviceData deviceData) {
            if (deviceData.hoveredInteractableObjects.Count > 0) {
                return (deviceData.hoveredInteractableObjects[0] == this);
            }
            return false;
        }

        /// <summary>
        /// is used to apply the position of the grabbing device to this object
        /// </summary>
        /// <param name="targetTransform"></param>
        public void ApplyTransformation(InputDeviceData deviceData) {
            if (!disableDefaultApplyTransformEvent) { 
                Transform targetTransform = deviceData.inputDevice.grabAttachementPoint.transform;
                Vector3 newPosition = new Vector3(
                            moveXByDrag ? targetTransform.position.x : transform.position.x,
                            moveYByDrag ? targetTransform.position.y : transform.position.y,
                            moveZByDrag ? targetTransform.position.z : transform.position.z
                        );
                Quaternion newRotation = Quaternion.Euler(
                            rotateXByDrag ? targetTransform.rotation.eulerAngles.x : transform.rotation.eulerAngles.x,
                            rotateYByDrag ? targetTransform.rotation.eulerAngles.y : transform.rotation.eulerAngles.y,
                            rotateZByDrag ? targetTransform.rotation.eulerAngles.z : transform.rotation.eulerAngles.z
                        );
                if (rigidbody) {
                    rigidbody.MovePosition(newPosition);
                    rigidbody.MoveRotation(newRotation);
                    rigidbody.velocity = Vector3.zero;
                    rigidbody.angularVelocity = Vector3.zero;
                } else {
                    transform.position = newPosition;
                    transform.rotation = newRotation;
                }
            }
            onApplyTransformOverrideEvent.Invoke(deviceData);
        }
        public void ApplyVelocity (InputDeviceData deviceData) {
            if (rigidbody) {
                rigidbody.velocity = deviceData.deviceVelocity;
                rigidbody.angularVelocity = deviceData.deviceAngularVelocity;
            }
        }

        /// <summary>
        /// device touch enter event
        /// </summary>
        [HideInInspector] // removed Client
        internal void OnHoverEnter(InputDeviceData deviceData) {
            if (this == null) {
                Debug.LogError("InteractableObject.OnHoverEnter: object destroyed!");
                return;
            }
            //Debug.Log("InteractableObject.OnHoverEnter: "+Time.frameCount+" "+ gameObject.name + " " + inputDevice.ToString());
            if (hoveringDevices.Contains(deviceData.inputDevice)) {
                Debug.LogError("InteractableObject.OnHoverEnter: device already hovered " + deviceData.ToString() + " / " + hoveringDevices.Count);
            } else {
                hoveringDevices.Add(deviceData.inputDevice);
                onHoverEnterEvent.Invoke(deviceData);
                StyleChangeEvent();
            }
        }

        /// <summary>
        /// device touch repeat event, Executed from WorldspaceInputDevice
        /// </summary>
        [HideInInspector] // removed Client
        internal void OnHoverRepeat(InputDeviceData deviceData) {
            if (this == null) {
                Debug.LogError("InteractableObject.OnHoverRepeat: object destroyed!");
                return;
            }
            //throw new NotImplementedException("InteractiableObject.OnTouchRepeat: must be overloaded");
            if (hoveringDevices.Contains(deviceData.inputDevice)) {
                onHoverRepeatEvent.Invoke(deviceData);
            } else {
                Debug.LogError("InteractableObject.OnHoverRepeat: device not in list " + deviceData.ToString() + " / " + hoveringDevices.Count);
            }
        }

        /// <summary>
        /// device touch exit event
        /// </summary>
        [HideInInspector] // removed Client
        internal void OnHoverExit(InputDeviceData deviceData) {
            if (this == null) {
                Debug.LogError("InteractableObject.OnHoverExit: object destroyed!");
                return;
            }
            //Debug.Log("InteractableObject.OnHoverExit: " + (gameObject!=null? gameObject.name:"(gameObject destroyed)") + " " + (inputDevice != null ? inputDevice.ToString() : "(inputDevice destroyed)"));
            if (hoveringDevices.Contains(deviceData.inputDevice)) {
                hoveringDevices.Remove(deviceData.inputDevice);
                onHoverExitEvent.Invoke(deviceData);
                StyleChangeEvent();
            } else {
                Debug.LogError("InteractableObject.OnHoverExit: device not in list " + deviceData.ToString() + " / " + hoveringDevices.Count);
            }
        }

        /// <summary>
        /// Grab start Event
        /// </summary>
        [HideInInspector] // Client
        internal void OnDragEnter(InputDeviceData deviceData) {
            if (this == null) {
                Debug.LogError("InteractableObject.OnDragEnter: object destroyed!");
                return;
            }
            if (grabbedByDevice != null) {
                Debug.LogError("InteractableObject.OnDragEnter: already grabbed '" + gameObject.name + "' by '" + (grabbedByDevice != null ? grabbedByDevice.ToString() : "null") + "' new device: '" + deviceData + "'");
            } else {
                Debug.Log("InteractableObject.OnDragEnter: " + gameObject.name + " by " + deviceData);
            }
            grabbedByDevice = deviceData.inputDevice;
            onDragEnterEvent.Invoke(deviceData);
            StyleChangeEvent();
        }

        /// <summary>
        /// Grab repeat Event
        /// </summary>
        [HideInInspector] // Client
        internal void OnDragRepeat(InputDeviceData deviceData) {
            if (this == null) {
                Debug.LogError("InteractableObject.OnDragRepeat: object destroyed!");
                return;
            }
            //throw new NotImplementedException("InteractiableObject.OnGrabRepeat: must be overloaded");
            if (grabbedByDevice != deviceData.inputDevice) {
                Debug.LogError("InteractableObject.OnDragRepeat: different grabbing device! " + deviceData.ToString() + " / " + (grabbedByDevice != null ? grabbedByDevice.ToString() : "null"));
            }
            onDragRepeatEvent.Invoke(deviceData);
        }

        /// <summary>
        /// Grab stop Event
        /// </summary>
        [HideInInspector] // Client
        internal void OnDragExit(InputDeviceData deviceData) {
            if (this == null) {
                Debug.LogError("InteractableObject.OnDragExit: object destroyed!");
                return;
            }
            Debug.Log("InteractableObject.OnDragExit: " + gameObject.name + " " + deviceData.ToString());
            if (grabbedByDevice == null) {
                Debug.LogError("InteractableObject.OnDragExit: already having no inputDevice " + deviceData.ToString() + " / " + (grabbedByDevice != null ? grabbedByDevice.ToString() : "null"));
            }
            grabbedByDevice = null;
            onDragExitEvent.Invoke(deviceData);
            StyleChangeEvent();
        }
        #endregion

        /// <summary>
        /// function to change style according to touched and grabbed state
        /// is Executed on hover enter & exit and grab/drag enter & exit events
        /// </summary>
        //[Client]
        protected virtual void StyleChangeEvent() {
            //Debug.LogWarning("InteractiableObject.StyleChangeEvent: should be overloaded");
        }

        #region DEBUG
        protected virtual string LogInfo() {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            //sb.Append(grabbed ? "grabbed " : " ungrabbed ");
            //sb.Append(touched ? "touched " : " untouched ");
            sb.Append(NetworkInteractableObject.HasLocalPlayerAuthority(gameObject) ? "authority " : " !authority ");
            sb.Append(isClient ? "client " : " notclient ");
            sb.Append(isServer ? "server " : " notserver ");
            return sb.ToString();
        }
        #endregion

    }
}
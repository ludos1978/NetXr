//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using NetXr;

namespace NetXr {
    [RequireComponent (typeof (InteractableObject))]
    public class InteractableEventDebugger : MonoBehaviour {
        InteractableObject interactableObject;
        Text label;

        public bool logToConsole = false;

        public void Awake () {
            label = GetComponentInChildren<Text> ();

            interactableObject = GetComponent<InteractableObject> ();

            // defaults: vive trigger pressed, left mouse button, thumb down gesture
            interactableObject.onUseStartEvent.AddListener (OnUseStartEvent);
            interactableObject.onUseRepeatEvent.AddListener (OnUseRepeatEvent);
            interactableObject.onUseStopEvent.AddListener (OnUseStopEvent);
            // defaults: vive grip pressed, right mouse button, grab gesture
            interactableObject.onAttachStartEvent.AddListener (OnAttachStartEvent);
            interactableObject.onAttachRepeatEvent.AddListener (OnAttachRepeatEvent);
            interactableObject.onAttachStopEvent.AddListener (OnAttachStopEvent);
            // defaults: vive touchpad pressed, middle mouse button, finger point gesture
            interactableObject.onTouchStartEvent.AddListener (OnTouchStartEvent);
            interactableObject.onTouchRepeatEvent.AddListener (OnTouchRepeatEvent);
            interactableObject.onTouchStopEvent.AddListener (OnTouchStopEvent);

            // defaults: undefined
            interactableObject.onEventOneStartEvent.AddListener (OnEventOneStartEvent);
            interactableObject.onEventOneStopEvent.AddListener (OnEventOneStopEvent);
            // defaults: undefined
            interactableObject.onEventTwoStartEvent.AddListener (OnEventTwoStartEvent);
            interactableObject.onEventTwoStopEvent.AddListener (OnEventTwoStopEvent);
            // defaults: undefined
            interactableObject.onEventThreeStartEvent.AddListener (OnEventThreeStartEvent);
            interactableObject.onEventThreeStopEvent.AddListener (OnEventThreeStopEvent);

            // object is dragged (by this.DoAttachToController() and this.DoDetachFromController())
            interactableObject.onDragEnterEvent.AddListener (OnDragEnterEvent);
            interactableObject.onDragRepeatEvent.AddListener (OnDragRepeatEvent);
            interactableObject.onDragExitEvent.AddListener (OnDragExitEvent);
            // object is hovered
            interactableObject.onHoverEnterEvent.AddListener (OnHoverEnterEvent);
            interactableObject.onHoverRepeatEvent.AddListener (OnHoverRepeatEvent);
            interactableObject.onHoverExitEvent.AddListener (OnHoverExitEvent);

            // it's also possible to attach to the input devices directly
            InputDeviceManager.Instance.newInputDeviceInitializedEvent.AddListener (InitControllerCallback);
        }

        private bool dragged = false, hovered = false, used = false, grabbed = false, touched = false, eventOne = false, eventTwo = false, eventThree = false;
        private List<int> draggedBy = new List<int> (), hoveredBy = new List<int> (), usedBy = new List<int> (), grabbedBy = new List<int> (), touchedBy = new List<int> (), eventOneBy = new List<int> (), eventTwoBy = new List<int> (), eventThreeBy = new List<int> ();
        private List<int> viveGripDevices = new List<int> (), leapGrabDevices = new List<int> (), mouse0Devices = new List<int> ();
        private List<int> viveGripDevicesThis = new List<int> (), leapGrabDevicesThis = new List<int> (), mouse0DevicesThis = new List<int> ();

        void Update () {
            label.text = GetText ();
        }

        private string GetText () {
            StringBuilder sb = new StringBuilder ();
            sb.Append (dragged ? "dragged " : "undragged ");
            sb.Append (string.Join (",", draggedBy.Select (x => x.ToString ()).ToArray ()));
            sb.Append ("\n");
            sb.Append (hovered ? "hovered " : "unhovered ");
            sb.Append (string.Join (",", hoveredBy.Select (x => x.ToString ()).ToArray ()));
            sb.Append ("\n");
            sb.Append (used ? "used " : "unused ");
            sb.Append (string.Join (",", usedBy.Select (x => x.ToString ()).ToArray ()));
            sb.Append ("\n");
            sb.Append (grabbed ? "grabbed " : "ungrabbed ");
            sb.Append (string.Join (",", grabbedBy.Select (x => x.ToString ()).ToArray ()));
            sb.Append ("\n");
            sb.Append (touched ? "touched " : "untouched ");
            sb.Append (string.Join (",", touchedBy.Select (x => x.ToString ()).ToArray ()));
            sb.Append ("\n");
            sb.Append (eventOne ? "eventOne " : "not eventOne ");
            sb.Append (string.Join (",", eventOneBy.Select (x => x.ToString ()).ToArray ()));
            sb.Append ("\n");
            sb.Append (eventTwo ? "eventTwo " : "not eventTwo ");
            sb.Append (string.Join (",", eventTwoBy.Select (x => x.ToString ()).ToArray ()));
            sb.Append ("\n");
            sb.Append (eventThree ? "eventThree " : "not eventThree ");
            sb.Append (string.Join (",", eventThreeBy.Select (x => x.ToString ()).ToArray ()));
            sb.Append ("\n");
            sb.Append (viveGripDevices.Count > 0 ? "viveGrip " : "not viveGrip ");
            sb.Append (string.Join (",", viveGripDevices.Select (x => x.ToString ()).ToArray ()));
            sb.Append ("\n");
            sb.Append (viveGripDevicesThis.Count > 0 ? "viveGripThis " : "not viveGripThis ");
            sb.Append (string.Join (",", viveGripDevicesThis.Select (x => x.ToString ()).ToArray ()));
            sb.Append ("\n");
            sb.Append (leapGrabDevices.Count > 0 ? "leapGrab " : "not leapGrab ");
            sb.Append (string.Join (",", leapGrabDevices.Select (x => x.ToString ()).ToArray ()));
            sb.Append ("\n");
            sb.Append (leapGrabDevicesThis.Count > 0 ? "leapGrabThis " : "not leapGrabThis ");
            sb.Append (string.Join (",", leapGrabDevicesThis.Select (x => x.ToString ()).ToArray ()));
            sb.Append ("\n");
            sb.Append (mouse0Devices.Count > 0 ? "mouse0 " : "not mouse0 ");
            sb.Append (string.Join (",", mouse0Devices.Select (x => x.ToString ()).ToArray ()));
            sb.Append ("\n");
            sb.Append (mouse0DevicesThis.Count > 0 ? "mouse0This " : "not mouse0This ");
            sb.Append (string.Join (",", mouse0DevicesThis.Select (x => x.ToString ()).ToArray ()));
            return sb.ToString ();
        }

        private void OnDragEnterEvent (InputDeviceData deviceData) {
            if (logToConsole)
                Debug.Log ("InteractableEventDebugger.OnDragEnterEvent");
            dragged = true;
            draggedBy.Add (deviceData.inputDevice.deviceId);
        }

        private void OnDragRepeatEvent (InputDeviceData deviceData) { }

        private void OnDragExitEvent (InputDeviceData deviceData) {
            if (logToConsole)
                Debug.Log ("InteractableEventDebugger.OnDragExitEvent");
            dragged = false;
            draggedBy.Remove (deviceData.inputDevice.deviceId);
        }

        private void OnHoverEnterEvent (InputDeviceData deviceData) {
            if (logToConsole)
                Debug.Log ("InteractableEventDebugger.OnHoverEnterEvent");
            hovered = true;
            hoveredBy.Add (deviceData.inputDevice.deviceId);
        }

        private void OnHoverRepeatEvent (InputDeviceData deviceData) { }

        private void OnHoverExitEvent (InputDeviceData deviceData) {
            if (logToConsole)
                Debug.Log ("InteractableEventDebugger.OnHoverExitEvent");
            hovered = false;
            hoveredBy.Remove (deviceData.inputDevice.deviceId);
        }

        private void OnUseStartEvent (InputDeviceData deviceData) {
            if (logToConsole)
                Debug.Log ("InteractableEventDebugger.OnUseStartEvent");
            used = true;
            usedBy.Add (deviceData.inputDevice.deviceId);
        }

        private void OnUseRepeatEvent (InputDeviceData deviceData) { }

        private void OnUseStopEvent (InputDeviceData deviceData) {
            if (logToConsole)
                Debug.Log ("InteractableEventDebugger.OnUseStopEvent");
            used = false;
            usedBy.Remove (deviceData.inputDevice.deviceId);
        }

        private void OnAttachStartEvent (InputDeviceData deviceData) {
            if (logToConsole)
                Debug.Log ("InteractableEventDebugger.OnGrabStartEvent");
            grabbed = true;
            grabbedBy.Add (deviceData.inputDevice.deviceId);
        }

        private void OnAttachRepeatEvent (InputDeviceData deviceData) { }

        private void OnAttachStopEvent (InputDeviceData deviceData) {
            if (logToConsole)
                Debug.Log ("InteractableEventDebugger.OnGrabStopEvent");
            grabbed = false;
            grabbedBy.Remove (deviceData.inputDevice.deviceId);
        }

        private void OnTouchStartEvent (InputDeviceData deviceData) {
            if (logToConsole)
                Debug.Log ("InteractableEventDebugger.OnTouchStartEvent");
            touched = true;
            touchedBy.Add (deviceData.inputDevice.deviceId);
        }

        private void OnTouchRepeatEvent (InputDeviceData deviceData) { }

        private void OnTouchStopEvent (InputDeviceData deviceData) {
            if (logToConsole)
                Debug.Log ("InteractableEventDebugger.OnTouchStopEvent");
            touched = false;
            touchedBy.Remove (deviceData.inputDevice.deviceId);
        }

        private void OnEventOneStartEvent (InputDeviceData deviceData) {
            if (logToConsole)
                Debug.Log ("InteractableEventDebugger.OnEventOneStartEvent");
            eventOne = true;
            eventOneBy.Add (deviceData.inputDevice.deviceId);
        }

        private void OnEventOneStopEvent (InputDeviceData deviceData) {
            if (logToConsole)
                Debug.Log ("InteractableEventDebugger.OnEventOneStopEvent");
            eventOne = false;
            eventOneBy.Remove (deviceData.inputDevice.deviceId);
        }

        private void OnEventTwoStartEvent (InputDeviceData deviceData) {
            if (logToConsole)
                Debug.Log ("InteractableEventDebugger.OnEventTwoStartEvent");
            eventTwo = true;
            eventTwoBy.Add (deviceData.inputDevice.deviceId);
        }

        private void OnEventTwoStopEvent (InputDeviceData deviceData) {
            if (logToConsole)
                Debug.Log ("InteractableEventDebugger.OnEventTwoStopEvent");
            eventTwo = false;
            eventTwoBy.Remove (deviceData.inputDevice.deviceId);
        }

        private void OnEventThreeStartEvent (InputDeviceData deviceData) {
            if (logToConsole)
                Debug.Log ("InteractableEventDebugger.OnEventThreeStartEvent");
            eventThree = true;
            eventThreeBy.Add (deviceData.inputDevice.deviceId);
        }

        private void OnEventThreeStopEvent (InputDeviceData deviceData) {
            if (logToConsole)
                Debug.Log ("InteractableEventDebugger.OnEventThreeStopEvent");
            eventThree = false;
            eventThreeBy.Remove (deviceData.inputDevice.deviceId);
        }

        /// <summary>
        /// Methode die sinn macht auf einer controller extension
        /// </summary>
        /// <param name="controllerId"></param>
        private void InitControllerCallback (int controllerId) {
            InputDevice inputDevice = InputDeviceManager.Instance.GetInputDeviceFromId (controllerId);
            // if this is a vive controller
            if (inputDevice.deviceType == InputDeviceType.Vive) {
                inputDevice.inputController.viveGripDown.AddListener (ViveGripDown);
                //inputDevice.inputController.viveGripPressed.AddListener(ViveGripPressed);
                inputDevice.inputController.viveGripUp.AddListener (ViveGripUp);
            }
            // if this is a right vive controller
            if ((inputDevice.deviceType == InputDeviceType.Vive) && (inputDevice.deviceHand == InputDeviceHand.Right)) { }
            if (inputDevice.deviceType == InputDeviceType.Leap) {
                inputDevice.inputController.leapGrabGestureStartEvent.AddListener (LeapGrabGestureStart);
                //inputDevice.inputController.leapGrabGestureRepeatEvent.AddListener(LeapGrabGestureRepeat);
                inputDevice.inputController.leapGrabGestureStopEvent.AddListener (LeapGrabGestureStop);
            }
            // add input for mouse
            if (inputDevice.deviceType == InputDeviceType.Mouse) {
                inputDevice.inputController.mouseButton0Down.AddListener (Mouse0Down);
                //inputDevice.inputController.mouseButton0Pressed.AddListener(Mouse0Pressed);
                inputDevice.inputController.mouseButton0Up.AddListener (Mouse0Up);
            }
        }

        private void ViveGripDown (InputDeviceData deviceData) {
            // use deviceData to check correlation with this object
            if (deviceData.hoveredInteractableObjects.Contains (interactableObject) || deviceData.grabbedInteractableObject == interactableObject) {
                viveGripDevicesThis.Add (deviceData.inputDevice.deviceId);
            }
            viveGripDevices.Add (deviceData.inputDevice.deviceId);
        }

        private void ViveGripUp (InputDeviceData deviceData) {
            viveGripDevices.Remove (deviceData.inputDevice.deviceId);
            if (viveGripDevicesThis.Contains (deviceData.inputDevice.deviceId))
                viveGripDevicesThis.Remove (deviceData.inputDevice.deviceId);
        }

        //private void ViveGripPressed(InputDeviceData deviceData) {
        //    throw new NotImplementedException();
        //}

        private void LeapGrabGestureStart (InputDeviceData deviceData) {
            if (deviceData.hoveredInteractableObjects.Contains (interactableObject) || deviceData.grabbedInteractableObject == interactableObject) {
                leapGrabDevicesThis.Add (deviceData.inputDevice.deviceId);
            }
            leapGrabDevices.Add (deviceData.inputDevice.deviceId);
        }

        private void LeapGrabGestureStop (InputDeviceData deviceData) {
            leapGrabDevices.Remove (deviceData.inputDevice.deviceId);
            if (leapGrabDevicesThis.Contains (deviceData.inputDevice.deviceId))
                leapGrabDevicesThis.Remove (deviceData.inputDevice.deviceId);
        }

        //private void LeapGrabGestureRepeat(InputDeviceData deviceData) {
        //    throw new NotImplementedException();
        //}

        private void Mouse0Down (InputDeviceData deviceData) {
            if (deviceData.hoveredInteractableObjects.Contains (interactableObject) || deviceData.grabbedInteractableObject == interactableObject) {
                mouse0DevicesThis.Add (deviceData.inputDevice.deviceId);
            }
            mouse0Devices.Add (deviceData.inputDevice.deviceId);
        }

        private void Mouse0Up (InputDeviceData deviceData) {
            mouse0Devices.Remove (deviceData.inputDevice.deviceId);
            if (mouse0DevicesThis.Contains (deviceData.inputDevice.deviceId))
                mouse0DevicesThis.Remove (deviceData.inputDevice.deviceId);
        }

        //private void Mouse0Pressed(InputDeviceData deviceData) {
        //    throw new NotImplementedException();
        //}
    }
}
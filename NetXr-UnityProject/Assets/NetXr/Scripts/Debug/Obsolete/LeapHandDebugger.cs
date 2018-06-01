//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace NetXr {
    // ALTERNATIVE IS LIKELY WORSE THEN THE OTHER DEBUGGER (LeapHandStateDebugger)
    [Obsolete]
    public class LeapHandDebugger : MonoBehaviour {
        Text label;
        LeapInputController leapController;

        public bool logToConsole = false;

        // Use this for initialization
        void Awake() {
            label = GetComponentInChildren<Text>();
            leapController = GetComponent<LeapInputController>();

            //internal bool thumbDownState = false;
            leapController.leapThumbHandDownGestureStartEvent.AddListener(leapThumbDownGestureStartEvent);
            leapController.leapThumbHandDownGestureStopEvent.AddListener(leapThumbDownGestureStopEvent);
            leapController.leapThumbHandDownGestureRepeatEvent.AddListener(leapThumbDownGestureRepeatEvent);

            //internal bool indexPointingState = false;
            //private float lastIndexPointingState = 0; // index open, others closed, last time was the case
            leapController.leapIndexPointingIgnoreThumbGestureStartEvent.AddListener(leapIndexPointingGestureStartEvent);
            leapController.leapIndexPointingIgnoreThumbGestureStopEvent.AddListener(leapIndexPointingGestureStopEvent);
            leapController.leapIndexPointingIgnoreThumbGestureRepeatEvent.AddListener(leapIndexPointingGestureRepeatEvent);

            //internal GestureState indexPointingTriggerState = GestureState.undefined;
            //internal bool indexThumbPointingState = false;
            //internal float lastIndexThumbPointingState = 0; // thumb and index open, others closed, last time was the case
            //leapController.leapIndexPointingTriggerGesturePrepareEvent.AddListener(leapIndexPointingTriggerGesturePrepareEvent);
            //leapController.leapIndexPointingTriggerGestureStartEvent.AddListener(leapIndexPointingTriggerGestureStartEvent);
            //leapController.leapIndexPointingTriggerGestureRepeatEvent.AddListener(leapIndexPointingTriggerGestureRepeatEvent);
            //leapController.leapIndexPointingTriggerGestureStopEvent.AddListener(leapIndexPointingTriggerGestureStopEvent);

            //internal GestureState handGrabGestureState = GestureState.undefined;
            leapController.leapGrabGesturePrepareEvent.AddListener(leapGrabGesturePrepareEvent);
            leapController.leapGrabGestureStartEvent.AddListener(leapGrabGestureStartEvent);
            leapController.leapGrabGestureStopEvent.AddListener(leapGrabGestureStopEvent);
            leapController.leapGrabGestureRepeatEvent.AddListener(leapGrabGestureRepeatEvent);

            //internal GestureState handReleaseGestureState = GestureState.undefined;
            leapController.leapReleaseGesturePrepareEvent.AddListener(leapReleaseGesturePrepareEvent);
            leapController.leapReleaseGestureStartEvent.AddListener(leapReleaseGestureStartEvent);
            leapController.leapReleaseGestureStopEvent.AddListener(leapReleaseGestureStopEvent);
            leapController.leapReleaseGestureRepeatEvent.AddListener(leapReleaseGestureRepeatEvent);

            //internal bool openGestureState = false;
            //private float lastOpenGestureTime = 0; // used for grab & release gesture
            leapController.leapOpenGestureStartEvent.AddListener(leapOpenGestureStartEvent);
            leapController.leapOpenGestureStopEvent.AddListener(leapOpenGestureStopEvent);
            leapController.leapOpenGestureRepeatEvent.AddListener(leapOpenGestureRepeatEvent);

            //internal bool closedGestureState = false;
            //private float lastClosedGestureTime = 0; // used for grab & release gesture
            leapController.leapClosedGestureStartEvent.AddListener(leapClosedGestureStartEvent);
            leapController.leapClosedGestureStopEvent.AddListener(leapClosedGestureStopEvent);
            leapController.leapClosedGestureRepeatEvent.AddListener(leapClosedGestureRepeatEvent);
        }

        public enum GestureState {
            Undefined,
            Prepare,
            Start,
            Stop,
            Repeat
        }

        #region thumbDownGestureState
        public GestureState thumbDownGestureState = GestureState.Undefined;
        private void leapThumbDownGestureStartEvent(InputDeviceData arg0) {
            thumbDownGestureState = GestureState.Start;
        }

        private void leapThumbDownGestureStopEvent(InputDeviceData arg0) {
            thumbDownGestureState = GestureState.Stop;
        }

        private void leapThumbDownGestureRepeatEvent(InputDeviceData arg0) {
            thumbDownGestureState = GestureState.Repeat;
        }
        #endregion

        #region indexPointingGestureState
        public GestureState indexPointingGestureState = GestureState.Undefined;
        private void leapIndexPointingGestureStartEvent(InputDeviceData arg0) {
            indexPointingGestureState = GestureState.Start;
        }

        private void leapIndexPointingGestureStopEvent(InputDeviceData arg0) {
            indexPointingGestureState = GestureState.Stop;
        }

        private void leapIndexPointingGestureRepeatEvent(InputDeviceData arg0) {
            indexPointingGestureState = GestureState.Repeat;
        }
        #endregion

        #region indexPointingTriggerState
        public GestureState indexPointingTriggerState = GestureState.Undefined;
        private void leapIndexPointingTriggerGesturePrepareEvent(InputDeviceData arg0) {
            indexPointingTriggerState = GestureState.Prepare;
        }

        private void leapIndexPointingTriggerGestureStartEvent(InputDeviceData arg0) {
            indexPointingTriggerState = GestureState.Start;
        }

        private void leapIndexPointingTriggerGestureRepeatEvent(InputDeviceData arg0) {
            indexPointingTriggerState = GestureState.Repeat;
        }

        private void leapIndexPointingTriggerGestureStopEvent(InputDeviceData arg0) {
            indexPointingTriggerState = GestureState.Stop;
        }
        #endregion

        #region grabGestureState
        public GestureState grabGestureState = GestureState.Undefined;
        private void leapGrabGesturePrepareEvent(InputDeviceData arg0) {
            grabGestureState = GestureState.Prepare;
        }

        private void leapGrabGestureStartEvent(InputDeviceData arg0) {
            grabGestureState = GestureState.Start;
        }

        private void leapGrabGestureStopEvent(InputDeviceData arg0) {
            grabGestureState = GestureState.Stop;
        }

        private void leapGrabGestureRepeatEvent(InputDeviceData arg0) {
            grabGestureState = GestureState.Repeat;
        }
        #endregion

        #region releaseGestureState
        public GestureState releaseGestureState = GestureState.Undefined;
        private void leapReleaseGesturePrepareEvent(InputDeviceData arg0) {
            releaseGestureState = GestureState.Prepare;
        }

        private void leapReleaseGestureStartEvent(InputDeviceData arg0) {
            releaseGestureState = GestureState.Start;
        }

        private void leapReleaseGestureStopEvent(InputDeviceData arg0) {
            releaseGestureState = GestureState.Stop;
        }

        private void leapReleaseGestureRepeatEvent(InputDeviceData arg0) {
            releaseGestureState = GestureState.Repeat;
        }
        #endregion

        #region openGestureState
        public GestureState openGestureState = GestureState.Undefined;
        private void leapOpenGestureStartEvent(InputDeviceData arg0) {
            openGestureState = GestureState.Start;
        }   

        private void leapOpenGestureStopEvent(InputDeviceData arg0) {
            openGestureState = GestureState.Stop;
        }

        private void leapOpenGestureRepeatEvent(InputDeviceData arg0) {
            openGestureState = GestureState.Repeat;
        }
        #endregion

        #region closedGestureState
        public GestureState closedGestureState = GestureState.Undefined;
        private void leapClosedGestureStartEvent(InputDeviceData arg0) {
            closedGestureState = GestureState.Start;
        }

        private void leapClosedGestureStopEvent(InputDeviceData arg0) {
            closedGestureState = GestureState.Stop;
        }

        private void leapClosedGestureRepeatEvent(InputDeviceData arg0) {
            closedGestureState = GestureState.Repeat;
        }
        #endregion

        void Update() {
            label.text = GetText();
        }

        private string GetText() {
            StringBuilder sb = new StringBuilder();
            sb.Append("thumbDownGestureState ");
            sb.Append(thumbDownGestureState.ToString());
            sb.Append("\n");
            sb.Append("indexPointingGestureState ");
            sb.Append(indexPointingGestureState.ToString());
            sb.Append("\n");
            sb.Append("indexPointingTriggerState ");
            sb.Append(indexPointingTriggerState.ToString());
            sb.Append("\n");
            sb.Append("grabGestureState ");
            sb.Append(grabGestureState.ToString());
            sb.Append("\n");
            sb.Append("releaseGestureState ");
            sb.Append(releaseGestureState.ToString());
            sb.Append("\n");
            sb.Append("openGestureState ");
            sb.Append(openGestureState.ToString());
            sb.Append("\n");
            sb.Append("closedGestureState ");
            sb.Append(closedGestureState.ToString());
            sb.Append("\n");
            //sb.Append(leapGrabDevices.Count > 0 ? "leapGrab " : "not leapGrab ");
            //sb.Append(string.Join(",", leapGrabDevices.Select(x => x.ToString()).ToArray()));
            //sb.Append("\n");
            //sb.Append(leapGrabDevicesThis.Count > 0 ? "leapGrabThis " : "not leapGrabThis ");
            //sb.Append(string.Join(",", leapGrabDevicesThis.Select(x => x.ToString()).ToArray()));
            //sb.Append("\n");
            //sb.Append(mouse0Devices.Count > 0 ? "mouse0 " : "not mouse0 ");
            //sb.Append(string.Join(",", mouse0Devices.Select(x => x.ToString()).ToArray()));
            //sb.Append("\n");
            //sb.Append(mouse0DevicesThis.Count > 0 ? "mouse0This " : "not mouse0This ");
            //sb.Append(string.Join(",", mouse0DevicesThis.Select(x => x.ToString()).ToArray()));
            return sb.ToString();
        }

    }
}
//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace NetXr { 
    public class LeapHandStateDebugger : MonoBehaviour {
        LeapInputController leapController;
        // List<string> log = new List<string>();
        Dictionary<string, float> logDict = new Dictionary<string, float>();
        // int maxLength = 10;
        Text logText;
        Regex regex;
        InputDeviceData lastDeviceData;
        public Gradient gradient;
        public float gradientDuration = 10;
        // Use this for initialization
        void Start () {
            logText = GetComponentInChildren<Text>();
            leapController = GetComponent<LeapInputController>();

            leapController.leapThumbFingerDownGestureStartEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapThumbFingerDownGestureStartEvent", deviceData); });
            leapController.leapThumbFingerDownGestureStopEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapThumbFingerDownGestureStopEvent", deviceData); });
            leapController.leapThumbFingerDownGestureRepeatEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapThumbFingerDownGestureRepeatEvent", deviceData); });

            leapController.leapIndexFingerDownGestureStartEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapIndexFingerDownGestureStartEvent", deviceData); });
            leapController.leapIndexFingerDownGestureStopEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapIndexFingerDownGestureStopEvent", deviceData); });
            leapController.leapIndexFingerDownGestureRepeatEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapIndexFingerDownGestureRepeatEvent", deviceData); });

            leapController.leapRingFingerDownGestureStartEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapRingFingerDownGestureStartEvent", deviceData); });
            leapController.leapRingFingerDownGestureStopEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapRingFingerDownGestureStopEvent", deviceData); });
            leapController.leapRingFingerDownGestureRepeatEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapRingFingerDownGestureRepeatEvent", deviceData); });

            leapController.leapIndexPointingIgnoreThumbGestureStartEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapIndexPointingGestureStartEvent", deviceData); });
            leapController.leapIndexPointingIgnoreThumbGestureStopEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapIndexPointingGestureStopEvent", deviceData); });
            leapController.leapIndexPointingIgnoreThumbGestureRepeatEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapIndexPointingGestureRepeatEvent", deviceData); });

            //leapController.leapIndexPointingTriggerGesturePrepareEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapIndexPointingTriggerGesturePrepareEvent", deviceData); });
            //leapController.leapIndexPointingTriggerGestureStartEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapIndexPointingTriggerGestureStartEvent", deviceData); });
            //leapController.leapIndexPointingTriggerGestureRepeatEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapIndexPointingTriggerGestureRepeatEvent", deviceData); });
            //leapController.leapIndexPointingTriggerGestureStopEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapIndexPointingTriggerGestureStopEvent", deviceData); });

            leapController.leapGrabGesturePrepareEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapGrabGesturePrepareEvent", deviceData); });
            leapController.leapGrabGestureStartEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapGrabGestureStartEvent", deviceData); });
            leapController.leapGrabGestureStopEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapGrabGestureStopEvent", deviceData); });
            leapController.leapGrabGestureRepeatEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapGrabGestureRepeatEvent", deviceData); });

            leapController.leapReleaseGesturePrepareEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapReleaseGesturePrepareEvent", deviceData); });
            leapController.leapReleaseGestureStartEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapReleaseGestureStartEvent", deviceData); });
            leapController.leapReleaseGestureStopEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapReleaseGestureStopEvent", deviceData); });
            leapController.leapReleaseGestureRepeatEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapReleaseGestureRepeatEvent", deviceData); });

            leapController.leapOpenGestureStartEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapOpenGestureStartEvent", deviceData); });
            leapController.leapOpenGestureStopEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapOpenGestureStopEvent", deviceData); });
            leapController.leapOpenGestureRepeatEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapOpenGestureRepeatEvent", deviceData); });

            leapController.leapClosedGestureStartEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapClosedGestureStartEvent", deviceData); });
            leapController.leapClosedGestureStopEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapClosedGestureStopEvent", deviceData); });
            leapController.leapClosedGestureRepeatEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapClosedGestureRepeatEvent", deviceData); });

            leapController.leapThumbHandDownGestureStartEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapThumbHandDownGestureStartEvent", deviceData); });
            leapController.leapThumbHandDownGestureStopEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapThumbHandDownGestureStopEvent", deviceData); });
            leapController.leapThumbHandDownGestureRepeatEvent.AddListener((InputDeviceData deviceData) => { LogEvent("leapThumbHandDownGestureRepeatEvent", deviceData); });

            regex = new Regex(@"
                    (?<=[A-Z])(?=[A-Z][a-z]) |
                     (?<=[^A-Z])(?=[A-Z]) |
                     (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);
        }

        string RemoveFromString (string text, string removeString) {
            int index = text.IndexOf(removeString);
            return (index < 0) ? text : text.Remove(index, removeString.Length);
        }

        void LogEvent (string eventName, InputDeviceData deviceData) {
            string cleanEventName = RemoveFromString(RemoveFromString(eventName, "leap"), "Event");
            if (!logDict.ContainsKey(cleanEventName)) {
                logDict.Add(cleanEventName, Time.realtimeSinceStartup);
            } else {
                logDict[cleanEventName] = Time.realtimeSinceStartup;
            }
            lastDeviceData = deviceData;
        }

        void LateUpdate () {
            string logOutput = "";

            //IOrderedEnumerable<KeyValuePair<string, float>> ordered = logDict.OrderBy(x => (Time.realtimeSinceStartup - x.Value));
            IOrderedEnumerable<KeyValuePair<string, float>> ordered = logDict.OrderBy(x => (x.Key));
            foreach (KeyValuePair<string, float> kvp in ordered) {
                float age = (Time.realtimeSinceStartup - kvp.Value);
                Color color = gradient.Evaluate(age / gradientDuration);
                logOutput += "<color=#" + ColorUtility.ToHtmlStringRGBA(color) + ">";
                //if (age < 0.5f) {
                //    logOutput += "<color=#ffffffff>"; // white
                //} else if (age < 1.0f) {
                //    logOutput += "<color=#bfbfbfff>"; // light gray
                //} else if (age < 9.0f) {
                //    logOutput += "<color=#7f7f7fff>"; // gray
                //} else if (age < 15.0f) {
                //    logOutput += "<color=#3f3f3fff>"; // dark gray
                //} else {
                //    logOutput += "<color=#000000ff>"; // dark gray
                //}
                logOutput += regex.Replace(kvp.Key, " ") + " " + age.ToString("0.0") + "</color>\n";
            }

            logOutput += "\n";
            //logOutput += "indexIgnoreThumbPointingState     " + leapController.indexIgnoreThumbPointingState + "\n";
            logOutput += "thumbFingerDownState              " + leapController.thumbFingerDownState + "\n";
            logOutput += "indexFingerDownState              " + leapController.indexFingerDownState + "\n";
            logOutput += "ringFingerDownState               " + leapController.ringFingerDownState + "\n";
            //logOutput += "indexPointingTriggerState         " + leapController.indexPointingTriggerState + "\n";
            //logOutput += "lastIndexIgnoreThumbPointingState " + (Time.realtimeSinceStartup - leapController.lastIndexIgnoreThumbPointingState) + "\n";
            //logOutput += "indexThumbPointingState       " + leapController.indexThumbPointingState + "\n";
            logOutput += "handGrabGestureState              " + leapController.handGrabGestureState + "\n";
            logOutput += "handReleaseGestureState           " + leapController.handReleaseGestureState + "\n";
            logOutput += "thumbDownState                    " + leapController.thumbHandDownState + "\n";
            logOutput += "openGestureState                  " + leapController.openGestureState + "\n";
            logOutput += "closedGestureState                " + leapController.closedGestureState + "\n";

            // log the hovered objects
            logOutput += "\n";
            if (lastDeviceData != null) { 
                logOutput += "grabbed: " + ((lastDeviceData.grabbedInteractableObject != null) ? lastDeviceData.grabbedInteractableObject.name : "none") + "\n";
                foreach (InteractableObject interactableObject in lastDeviceData.hoveredInteractableObjects) {
                    logOutput += "hovered: " + interactableObject.name + "\n";
                }
            }

            //log.Add(eventName + " (" + deviceData + ")");
            //if (log.Count > maxLength) {
            //    log.RemoveRange(0, log.Count - maxLength);
            //}
            // string.Join("\n", log.ToArray());
            logText.text = logOutput; 
        }
    }
}
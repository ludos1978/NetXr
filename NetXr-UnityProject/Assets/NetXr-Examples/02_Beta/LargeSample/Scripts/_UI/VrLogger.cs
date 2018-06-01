//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using System.Text;

[System.Serializable]
public class LogEntry {
    public DateTime timeout;
    public string logString;
    public string stackTrace;
    public LogType logType;
}

public class VrLogger : MonoBehaviour {
    public Text textLabel;

    // list of log entries
    private List<LogEntry> logEntries = new List<LogEntry>();

    public int maxDisplay = 25;
    public int maxEntries = 1000;

    public Toggle displayAllToggle;
    public bool displayAll = true;
    public Toggle displayErrorToggle;
    public bool displayError = true;
    public Toggle displayWarningToggle;
    public bool displayWarning = true;
    public Toggle displayInfoToggle;
    public bool displayInfo = true;

    private string _text = "";
    public string text {
        get {
            return _text;
        }
        set {
            _text = value;
            textLabel.text = _text;
        }
    }

    void Start() {
        // toggle not value and set to value to trigger onValueChanged for sure
        ToggleDisplayAll(!displayAll);
        ToggleDisplayAll(!displayAll);
        ToggleDisplayError(!displayError);
        ToggleDisplayError(!displayError);
        ToggleDisplayInfo(!displayInfo);
        ToggleDisplayInfo(!displayInfo);
        ToggleDisplayWarning(!displayWarning);
        ToggleDisplayWarning(!displayWarning);
    }

    void OnEnable () {
        Application.logMessageReceived += HandleLog;
    }
    void OnDisable() {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType logType) {
        if (logType == LogType.Error || logType == LogType.Assert) {
            AddText (logString, stackTrace, logType, new TimeSpan (0,0,60));
        } else {
            AddText (logString, stackTrace, logType, new TimeSpan (0,0,15));     
        }
    }

    public void ToggleDisplayAll(bool value) {
        if (value != displayAll) {
            displayAll = value;
            displayAllToggle.isOn = value;
        }
    }
    public void ToggleDisplayError(bool value) {
        if (value != displayError) {
            displayError = value;
            displayErrorToggle.isOn = value;
        }
    }
    public void ToggleDisplayWarning(bool value) {
        if (value != displayWarning) {
            displayWarning = value;
            displayWarningToggle.isOn = value;
        }
    }
    public void ToggleDisplayInfo(bool value) {
        if (value != displayInfo) { 
            displayInfo = value;
            displayInfoToggle.isOn = value;
        }
    }

    public void AddText (string logString, string stackTrace, LogType logType, TimeSpan displayDuration) {

        DateTime timeout = DateTime.MinValue;
        if (displayDuration.Ticks > 0) {
            timeout = DateTime.Now + displayDuration;
        }
        logEntries.Add( new LogEntry() { timeout = timeout, logString = logString, stackTrace = stackTrace, logType = logType } );
    }

    public void LateUpdate () {
        // remove log entries > maxEntries
        if (logEntries.Count > maxEntries) {
            logEntries.RemoveRange(maxEntries, logEntries.Count - maxEntries);
        }

        StringBuilder sb = new StringBuilder ();
        for (int i = Mathf.Max(logEntries.Count - maxDisplay, 0); i < logEntries.Count; i++) {
            if ((DateTime.Now >= logEntries[i].timeout) && (!displayAll)) {
                // entry is old & displayAll is off, skip displaying
                continue;
            }

            if (!displayError && (logEntries[i].logType == LogType.Error)) {
                continue;
            }
            if (!displayWarning && (logEntries[i].logType == LogType.Warning)) {
                continue;
            }
            if (!displayInfo && (logEntries[i].logType == LogType.Log)) {
                continue;
            }

            sb.Append ("<color=black>- </color>");
            switch (logEntries [i].logType) {
            case LogType.Assert:
                sb.Insert (0, "<color=red>");
                break;
            case LogType.Exception:
                sb.Insert (0, "<color=red>");
                break;
            case LogType.Error:
                sb.Insert (0, "<color=brown>");
                break;
            case LogType.Warning:
                sb.Insert (0, "<color=purple>");
                break;
            case LogType.Log:
                sb.Insert (0, "<color=grey>");
                break;
            }
            sb.Append (logEntries [i].logString);
            sb.Append ("</color>\n");
        }
        textLabel.text = sb.ToString();
    }
}

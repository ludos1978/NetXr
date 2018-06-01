using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using NetXr;

public enum FINGERS {
    thumb,
    index,
    middle,
    ring,
    pinki
}

public enum HANDEDNESS { Left, Right };

[System.Serializable]
public class HandFingers {
    public FINGERS finger;
    public Transform head;
    public Quaternion headRot;
    public Transform mid;
    public Quaternion midRot;
    public Transform root;
    public Quaternion rootRot;

    /// <summary>
    /// search the given transform by name to define head, mid & root objects
    /// </summary>
    /// <param name="handRoot"></param>
    public void ApplyFromTransform (Transform handRoot, VrHandController vrHandController) {
        string searchStringBase = ""; // finger.ToString().ToLower(); // "index", "middle", ...
        switch (finger) {
            case FINGERS.thumb:
                searchStringBase = vrHandController.thumbFingerName;
                break;
            case FINGERS.index:
                searchStringBase = vrHandController.indexFingerName;
                break;
            case FINGERS.middle:
                searchStringBase = vrHandController.middleFingerName;
                break;
            case FINGERS.ring:
                searchStringBase = vrHandController.ringFingerName;
                break;
            case FINGERS.pinki:
                searchStringBase = vrHandController.pinkyFingerName;
                break;
        }

        foreach (Transform t in handRoot.GetComponentsInChildren<Transform>(true)) {
            string value = t.name.ToLower();
            // skip _end
            if (value.Contains("_end")) {
                continue;
            }
            if (value.StartsWith(searchStringBase + vrHandController.stringSeparator + "01")) {
                root = t;
            }
            if (value.StartsWith(searchStringBase + vrHandController.stringSeparator + "02")) {
                mid = t;
            }
            if (value.StartsWith(searchStringBase + vrHandController.stringSeparator + "03")) {
                head = t;
            }
            //Debug.Log("checking " + value + " for " + searchStringBase);
        }

        if ((root != null) && (mid != null) && (head != null)) {
            headRot = head.transform.localRotation;
            midRot = mid.transform.localRotation;
            rootRot = root.transform.localRotation;
        }
        else {
            Debug.LogError("Something is missing " + root + " " + mid + " " + head + " root " + handRoot.name + " search " + searchStringBase);
        }
    } 

    public void ApplyRotation (Vector3 angle) {
        if ((root != null) && (mid != null) && (head != null)) {
            head.transform.localRotation = headRot * Quaternion.Euler(angle);
            mid.transform.localRotation = midRot * Quaternion.Euler(angle);
            if (finger != FINGERS.thumb) {
                root.transform.localRotation = rootRot * Quaternion.Euler(angle);
            }
        }
    }
}

public enum ACTION {
    Thumb,
    Point,
    Grab
}

public class VrHandController : NetworkBehaviour {
    public InputDeviceHand handedness;

    public Transform handArmature;
    public Transform handTarget;
    public HandFingers[] handFingers = new HandFingers[5];
    public string stringSeparator = ".0"; // name requires to be "index"+stringSeparator+"01"...
    public string thumbFingerName = "thumb";
    public string indexFingerName = "index";
    public string middleFingerName = "middle";
    public string ringFingerName = "ring";
    public string pinkyFingerName = "pinki";

    public Vector3 fingerRotation = new Vector3(0, 0, 60);
    //public Vector3 rightHandFingerRotation = new Vector3(0, 0, 60);

    NetworkPlayerController networkPlayerController = null;

    public bool thumbPressed;
    [SyncVar(hook = "ThumbAngle")]
    private float thumbAngle;
    public bool pointPressed;
    [SyncVar(hook = "PointAngle")]
    private float pointAngle;
    public bool grabPressed;
    [SyncVar(hook = "GrabAngle")]
    private float grabAngle;

    public override void OnStartClient() {
        //Debug.LogWarning("VrHandController.OnStartClient: NetworkPlayerController" + networkPlayerController.isLocalPlayer + " NetworkIdentity " + GetComponent<NetworkIdentity>().isLocalPlayer + " parent " + (transform.parent != null ? transform.parent.name : "null"));
        networkPlayerController = GetComponent<NetworkPlayerController>();
        base.OnStartClient();
        InitHandModel();
    }

    public override void OnStartLocalPlayer() {
        //Debug.LogWarning("VrHandController.OnStartLocalPlayer: NetworkPlayerController" + networkPlayerController.isLocalPlayer + " NetworkIdentity " + GetComponent<NetworkIdentity>().isLocalPlayer + " parent " + (transform.parent!=null?transform.parent.name:"null"));
        base.OnStartLocalPlayer();
        // will be true anyway...
        if (networkPlayerController.isLocalPlayer) {
            InitController();
        }
    }

    private void InitHandModel() {
        if (handArmature != null) { 
        handFingers[0] = new HandFingers() { finger = FINGERS.thumb };
        handFingers[1] = new HandFingers() { finger = FINGERS.index };
        handFingers[2] = new HandFingers() { finger = FINGERS.middle };
        handFingers[3] = new HandFingers() { finger = FINGERS.ring };
        handFingers[4] = new HandFingers() { finger = FINGERS.pinki };
        foreach (HandFingers hf in handFingers) {
            hf.ApplyFromTransform(handArmature, this);
        }
        } else {
            Debug.LogError("HandArmature is null");
        }
    }

    private void InitController() {
        InputDeviceManager.Instance.AddCallbackForInputDevice(InitControllerCallback);
    }

    /// <summary>
    /// update angle on server
    /// </summary>
    [Command]
    public void CmdThumbAngle(float value) { thumbAngle = value; }
    /// <summary>
    /// update angle on clients
    /// </summary>
    [Client]
    public void ThumbAngle(float value) { thumbAngle = value; }
    /// <summary>
    /// update angle on server
    /// </summary>
    [Command]
    public void CmdPointAngle(float value) { pointAngle = value; }
    /// <summary>
    /// update angle on clients
    /// </summary>
    [Client]
    public void PointAngle(float value) { pointAngle = value; }
    /// <summary>
    /// update angle on server
    /// </summary>
    [Command]
    public void CmdGrabAngle(float value) { grabAngle = value; }
    /// <summary>
    /// update angle on clients
    /// </summary>
    [Client]
    public void GrabAngle(float value) { grabAngle = value; }
    /// <summary>
    /// 
    /// </summary>
    public void UpdateAngles() {
        if (networkPlayerController.isLocalPlayer) {
            // send data to server
            CmdThumbAngle(thumbAngle);
            CmdPointAngle(pointAngle);
            CmdGrabAngle(grabAngle);
        }
    }

    void LateUpdate() {
        if (networkPlayerController == null)
            return;

        if (networkPlayerController.isLocalPlayer) {
            // update angles from input
            UpdateTarget();
            UpdateInput();
            UpdateAngles();
            ApplyAngles();
        }
        else {
            UpdateTarget();
            ApplyAngles();
        }
    }

    InputDevice listenedInputDevice;
    //WorldspaceController.WorldspaceInputDevice listenedInputDevice;
    private void InitControllerCallback(int controllerId) {
        //WorldspaceController.WorldspaceInputDevice _inputDevice = WorldspaceController.WorldspaceInputDeviceManager.Instance.GetInputDeviceFromId(controllerId);
        InputDevice _inputDevice = InputDeviceManager.Instance.GetInputDeviceFromId(controllerId);
        // if this is a vive controller
        if (_inputDevice.deviceType == InputDeviceType.Vive) {
            if (_inputDevice.deviceHand == handedness) {
                // thumb
                _inputDevice.inputController.viveTouchpadDown.AddListener(ViveTouchpadDown);
                _inputDevice.inputController.viveTouchpadUp.AddListener(ViveTouchpadUp);
                // index
                _inputDevice.inputController.viveGripDown.AddListener(ViveGripDown);
                _inputDevice.inputController.viveGripUp.AddListener(ViveGripUp);
                // middle, ring, pinky
                _inputDevice.inputController.viveTriggerDown.AddListener(ViveTriggerDown);
                _inputDevice.inputController.viveTriggerUp.AddListener(ViveTriggerUp);

                listenedInputDevice = _inputDevice;

                InputDeviceManager.Instance.RemoveCallForInputDevice(InitControllerCallback);
            }
        }
        if (_inputDevice.deviceType == InputDeviceType.Mouse) {
            if ((_inputDevice.deviceHand == InputDeviceHand.Undefined) && (handedness == InputDeviceHand.Right)) {
                // thumb
                _inputDevice.inputController.mouseButton0Down.AddListener(ViveTouchpadDown);
                _inputDevice.inputController.mouseButton0Up.AddListener(ViveTouchpadUp);
                // index
                _inputDevice.inputController.mouseButton2Down.AddListener(ViveTriggerDown);
                _inputDevice.inputController.mouseButton2Up.AddListener(ViveTriggerUp);
                // middle, ring, pinky
                _inputDevice.inputController.mouseButton1Down.AddListener(ViveGripDown);
                _inputDevice.inputController.mouseButton1Up.AddListener(ViveGripUp);

                listenedInputDevice = _inputDevice;

                InputDeviceManager.Instance.RemoveCallForInputDevice(InitControllerCallback);
            }
        }
    }

    private void RemoveControllerCallbacks () {
        listenedInputDevice.inputController.viveGripDown.AddListener(ViveGripDown);
        listenedInputDevice.inputController.viveGripUp.AddListener(ViveGripUp);
        listenedInputDevice.inputController.viveTouchpadDown.AddListener(ViveTouchpadDown);
        listenedInputDevice.inputController.viveTouchpadUp.AddListener(ViveTouchpadUp);
        listenedInputDevice.inputController.viveTriggerDown.AddListener(ViveTriggerDown);
        listenedInputDevice.inputController.viveTriggerUp.AddListener(ViveTriggerUp);
    }

    private void ViveGripDown(InputDeviceData deviceData) {
        SetGrabState(true);
    }

    private void ViveGripUp(InputDeviceData deviceData) {
        SetGrabState(false);
    }

    private void ViveTouchpadDown(InputDeviceData deviceData) {
        SetThumbState(true);
    }

    private void ViveTouchpadUp(InputDeviceData deviceData) {
        SetThumbState(false);
    }

    private void ViveTriggerDown(InputDeviceData deviceData) {
        SetPointState(true);
    }

    private void ViveTriggerUp(InputDeviceData deviceData) {
        SetPointState(false);
    }

    public void SetThumbState(bool state) {
        thumbPressed = state;
    }
    public void SetPointState(bool state) {
        pointPressed = state;
    }
    public void SetGrabState(bool state) {
        grabPressed = state;
    }

    public Vector3 handOffset = new Vector3(0, -0.05f, -0.1f);

    void UpdateTarget() {
        // read position of Hand from NetworkPlayerController
        // foreach (NetworkInputDevice netInputDev in NetworkPlayerController.LocalInstance.addedInputDevices) {
        foreach (NetworkInputDevice netInputDev in gameObject.GetComponent<NetworkPlayerController>().addedInputDevices) {
            if (netInputDev != null) {
                if (netInputDev.deviceHand == handedness) {
                    handTarget.position = netInputDev.transform.position + netInputDev.transform.forward * handOffset.z + netInputDev.transform.up * handOffset.y + netInputDev.transform.right * handOffset.x;
                    handTarget.rotation = netInputDev.transform.rotation;
                }
            }
        }
    }

    // Update is called once per frame
    void UpdateInput() {
        if (thumbPressed) {
            thumbAngle = Mathf.MoveTowards(thumbAngle, 1, Time.deltaTime * 5);
        }
        else {
            thumbAngle = Mathf.MoveTowards(thumbAngle, 0, Time.deltaTime * 5);
        }
        if (pointPressed) {
            pointAngle = Mathf.MoveTowards(pointAngle, 1, Time.deltaTime * 5);
        }
        else {
            pointAngle = Mathf.MoveTowards(pointAngle, 0, Time.deltaTime * 5);
        }
        if (grabPressed) {
            grabAngle = Mathf.MoveTowards(grabAngle, 1, Time.deltaTime * 5);
        }
        else {
            grabAngle = Mathf.MoveTowards(grabAngle, 0, Time.deltaTime * 5);
        }
    }

    void ApplyAngles () {
        //float angle = -60;
        //Vector3 rotation = fingerRotation;
        //if (handedness == InputDeviceHand.Right) {
        //    rotation = rightHandFingerRotation;
        //}
        handFingers[0].ApplyRotation(thumbAngle * fingerRotation);
        handFingers[1].ApplyRotation(pointAngle * fingerRotation);
        handFingers[2].ApplyRotation(grabAngle * fingerRotation);
        handFingers[3].ApplyRotation(grabAngle * fingerRotation);
        handFingers[4].ApplyRotation(grabAngle * fingerRotation);
    }
}

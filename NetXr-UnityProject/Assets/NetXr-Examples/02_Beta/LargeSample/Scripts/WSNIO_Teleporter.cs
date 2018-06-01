//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetXr;

namespace NetXr {
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine.Events;

    [CustomEditor(typeof(WSNIO_Teleporter))]
    public class WSNIO_TeleporterInspector : Editor {
        public override void OnInspectorGUI() {
            WSNIO_Teleporter myScript = (WSNIO_Teleporter)target;
            InteractableObject interactableObject = myScript.GetComponent<InteractableObject>();

            if (GUILayout.Button("Define default Callbacks")) {
                if (interactableObject.onHoverEnterEvent.GetPersistentEventCount() == 0) { 
                    //UnityEditor.Events.UnityEventTools.RemovePersistentListener(interactableObject.onAttachStartEvent, (UnityAction)myScript.DoShowTeleporterStart);
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(interactableObject.onHoverEnterEvent, myScript.DoShowTeleporterStart);
                } else {
                    Debug.LogError("WSNIO_TeleporterInspector: cannot define persistent listener when it already contains events");
                }
                if (interactableObject.onHoverExitEvent.GetPersistentEventCount() == 0) {
                    //UnityEditor.Events.UnityEventTools.RemovePersistentListener(interactableObject.onAttachStopEvent, (UnityAction)myScript.DoShowTeleporterStop);
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(interactableObject.onHoverExitEvent, myScript.DoShowTeleporterStop);
                } else {
                    Debug.LogError("WSNIO_TeleporterInspector: cannot define persistent listener when it already contains events");
                }
                if (interactableObject.onUseStartEvent.GetPersistentEventCount() == 0) {
                    //UnityEditor.Events.UnityEventTools.RemovePersistentListener(interactableObject.onUseStartEvent, (UnityAction)myScript.DoTeleportTo);
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(interactableObject.onUseStartEvent, myScript.DoTeleportTo);
                } else {
                    Debug.LogError("WSNIO_TeleporterInspector: cannot define persistent listener when it already contains events");
                }
                serializedObject.ApplyModifiedProperties();
            }

            DrawDefaultInspector();
        }
    }
#endif

    public class WSNIO_TeleporterData {
        // target square line renderer
        public LineRenderer targetPositionLR;
        public bool enabled = false;
    }

    public class WSNIO_Teleporter : MonoBehaviour {
        public Material lineMaterial;
        public float lineWidth = 0.01f;

        Dictionary<int, WSNIO_TeleporterData> teleporterDatas;
        public void Awake () {
            teleporterDatas = new Dictionary<int, WSNIO_TeleporterData>();
        }

        public void DoShowTeleporterStart (InputDeviceData deviceData) {
            //Debug.Log("WSNIO_Teleporter.DoShowTeleporterStart " + deviceData);
            // do not teleport with mouse
            if (deviceData.inputDevice.deviceType == InputDeviceType.Mouse) {
                return;
            }

            //if (!deviceData.hoveredInteractableObjects.Contains(this.GetComponent<InteractableObject>())) {
            //    Debug.LogError("WSNIO_Teleporter.DoShowTeleporterStart: "+name+" is not in hovered objects of controller, maybe wrong callbacks setup?");
            //}

            // switch inputdevice to parabola cast (does not work nicely because it may switch between parabala and line casting every frame)
            //deviceData.inputDevice.physicsCastType = PhysicsCastType.parabola;

            if (!teleporterDatas.ContainsKey(deviceData.inputDevice.deviceId)) {
                GameObject targetPositionLRGo = new GameObject("lineRenderer" + deviceData.inputDevice.deviceId, new System.Type[] { typeof(LineRenderer) });
                targetPositionLRGo.transform.parent = this.transform;

                WSNIO_TeleporterData teleporterData = new WSNIO_TeleporterData() {
                    targetPositionLR = targetPositionLRGo.GetComponent<LineRenderer>()
                };

                teleporterData.targetPositionLR.endWidth = lineWidth;
                teleporterData.targetPositionLR.startWidth = lineWidth;
                teleporterData.targetPositionLR.material = lineMaterial;

                teleporterDatas.Add(deviceData.inputDevice.deviceId, teleporterData);
            }

            teleporterDatas[deviceData.inputDevice.deviceId].enabled = true;
        }

        public void DoShowTeleporterStop (InputDeviceData deviceData) {
            //Debug.Log("WSNIO_Teleporter.DoShowTeleporterStop "+ deviceData);

            // do not teleport with mouse
            if (deviceData.inputDevice.deviceType == InputDeviceType.Mouse) {
                return;
            }

            //deviceData.inputDevice.physicsCastType = PhysicsCastType.ray;

            if (teleporterDatas.ContainsKey(deviceData.inputDevice.deviceId)) {
                teleporterDatas[deviceData.inputDevice.deviceId].targetPositionLR.positionCount = 0;
                teleporterDatas[deviceData.inputDevice.deviceId].enabled = false;
            } else {
                Debug.LogError("WSNIO_Teleporter.DoShowTeleporterStop");
            }
        }

        public void Update () {
            foreach (KeyValuePair<int, WSNIO_TeleporterData> kvp in teleporterDatas) {
                if (kvp.Value.enabled) {
                    // get the current device data
                    InputDeviceData deviceData = InputDeviceManager.Instance.GetInputDeviceFromId(kvp.Key).deviceData;
                    UpdateFunction(deviceData, kvp.Value);
                }
            }
        }

        public virtual void UpdateFunction(InputDeviceData deviceData, WSNIO_TeleporterData teleporterData) {
            LineRenderer targetPositionLR = teleporterData.targetPositionLR;
            if ((deviceData.hoveredInteractableObjects.Count > 0) || (deviceData.grabbedInteractableObject != null)) {
                targetPositionLR.positionCount = 5;
                targetPositionLR.SetPositions(new Vector3[] {
                    deviceData.worldspaceHit + new Vector3(-1,.2f,-1),
                    deviceData.worldspaceHit + new Vector3( 1,.2f,-1),
                    deviceData.worldspaceHit + new Vector3( 1,.2f, 1),
                    deviceData.worldspaceHit + new Vector3(-1,.2f, 1),
                    deviceData.worldspaceHit + new Vector3(-1,.2f,-1)
                });
            } else {
                targetPositionLR.positionCount = 0;
            }
        }

        public virtual void DoTeleportTo(InputDeviceData deviceData) {
            // do not teleport with mouse
            if (deviceData.inputDevice.deviceType == InputDeviceType.Mouse) {
                return;
            }

            if (!deviceData.hoveredInteractableObjects.Contains(this.GetComponent<InteractableObject>())) {
                Debug.LogError("WSNIO_Teleporter.DoShowTeleporterStart: " + name + " is not in hovered objects of controller, maybe wrong callbacks setup?");
            }

            if ((deviceData.grabbedInteractableObject != null) || (deviceData.uiHit)) {
                return;
            }
            if (teleporterDatas.ContainsKey(deviceData.inputDevice.deviceId)) {
                if (teleporterDatas[deviceData.inputDevice.deviceId].enabled) {
                    CameraController.Instance.FadeToColor(0.2f, Color.black);
                    PlayerSettingsController.Instance.cameraInstance.transform.position = deviceData.worldspaceHit;
                }
            }
        }
    }
}
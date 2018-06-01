//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using UnityEngine;

namespace NetXr {
    public class WSNIO_ClimbableGrabObject : MonoBehaviour {
        private InteractableObject interactableObject;
        public Vector3 localRaySourceRelativeStartPosition = Vector3.zero;
        public Vector3 raySourceStartPosition = Vector3.zero;
        public Quaternion worldPreviousRotation = Quaternion.identity; // new Vector3(60, 0, 0);

        public Vector3 playAreaStartPosition = Vector3.zero;
        public Vector3 controllerStartPosition = Vector3.zero;
        public Vector3 controllerRelativeStartPosition = Vector3.zero;

        // Use this for initialization
        void Awake () {
            //Debug.Log("WSNIO_ClimbableGrabObject.Awake: " + name);
            interactableObject = GetComponent<InteractableObject> ();
            interactableObject.onAttachStartEvent.AddListener (interactableObject.DoAttachToController);
            interactableObject.onAttachStopEvent.AddListener (interactableObject.DoDetachFromController);
            interactableObject.onDragEnterEvent.AddListener (OnDragStart);
            interactableObject.onDragExitEvent.AddListener (OnDragStop);
            interactableObject.disableDefaultApplyTransformEvent = true;
            interactableObject.onApplyTransformOverrideEvent.AddListener (ApplyTransformation);
            //interactableObject.onTouchStartEvent.AddListener(DoTouchStart);
            //interactableObject.onUseStartEvent.AddListener(DoUseBubbleGun);
        }

        public void OnDragStart (InputDeviceData deviceData) {
            Debug.Log ("WSNIO_ClimbableGrabObject.OnDragStart");
            //PlayerSettingsController.Instance.cameraInstance.GetComponent<Rigidbody>().isKinematic = true;
            PlayerPhysics.Instance.SetKinematic (deviceData, true);
            playAreaStartPosition = PlayerSettingsController.Instance.cameraInstance.transform.position;
            controllerStartPosition = GetControllerPosition (deviceData.inputDevice.controller.transform, deviceData.inputDevice.sphereCastPoint);
        }

        public void OnDragStop (InputDeviceData deviceData) {
            Debug.Log ("WSNIO_ClimbableGrabObject.OnDragStop");
            PlayerPhysics.Instance.SetKinematic (deviceData, false);
            //PlayerSettingsController.Instance.cameraInstance.GetComponent<Rigidbody>().isKinematic = false;
        }

        private Vector3 GetControllerPosition (Transform controllerTransform, Transform pointTransform) {
            Vector3 objLocalPos = controllerTransform.parent.InverseTransformPoint (pointTransform.position);

            Transform steamVrPlayArea = PlayerSettingsController.Instance.cameraInstance.transform;
            if (steamVrPlayArea.localScale != Vector3.one) {
                return steamVrPlayArea.localRotation * Vector3.Scale (objLocalPos, steamVrPlayArea.localScale);
            }

            return steamVrPlayArea.localRotation * objLocalPos;
        }

        //public Vector3 ControllerPosition (InputDeviceData deviceData) {
        //    return deviceData.inputDevice.controller.transform.TransformPoint(controllerRelativeStartPosition);
        //}

        public void ApplyTransformation (InputDeviceData deviceData) {
            Vector3 newPos = playAreaStartPosition - (GetControllerPosition (deviceData.inputDevice.controller.transform, deviceData.inputDevice.sphereCastPoint) - controllerStartPosition);
            Debug.Log ("WSNIO_ClimbableGrabObject.ApplyTransformation: " + newPos + " " + GetControllerPosition (deviceData.inputDevice.controller.transform, deviceData.inputDevice.sphereCastPoint) + " " + controllerStartPosition);
            PlayerSettingsController.Instance.cameraInstance.transform.position = newPos;
        }
    }
}
//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using UnityEngine;

namespace NetXr {
    public class WSNIO_BubbleGun : MonoBehaviour {
        private InteractableObject interactableObject;
        public string BubbleShotPrefabName = "BubbleShot";
        public Transform source;
        // public GameObject bubbleTemplate;
        public float bubbleForce = 50;
        new Rigidbody rigidbody;
        public Quaternion relativeRotation = Quaternion.identity; // new Vector3(60, 0, 0);
        public Vector3 relativePosition = Vector3.zero;
        public bool relativeTransformInitialized = false;

        public void Awake () {
            rigidbody = GetComponent<Rigidbody> ();
            interactableObject = GetComponent<InteractableObject> ();
            interactableObject.onAttachStartEvent.AddListener (OnDragStart);
            interactableObject.onAttachStartEvent.AddListener (interactableObject.DoAttachToController);
            interactableObject.onAttachStopEvent.AddListener (interactableObject.DoDetachFromController);
            interactableObject.onUseStartEvent.AddListener (DoUseBubbleGun);
            interactableObject.disableDefaultApplyTransformEvent = true;
            interactableObject.onApplyTransformOverrideEvent.AddListener (ApplyTransformation);
            interactableObject.onTouchStartEvent.AddListener (DoTouchStart);
        }

        public void DoUseBubbleGun (InputDeviceData deviceData) {
            // GameObject bubbleInstance = Instantiate (bubbleTemplate, source.transform.position, source.transform.rotation);
            Debug.Log("Create Shot at "+source.transform.position+ "   " +source.transform.rotation);
            NetworkPlayerController.LocalInstance.CmdCreateNetworkObjectAuthForce(source.transform.position,  source.transform.rotation, BubbleShotPrefabName, true, source.transform.forward * bubbleForce, ForceMode.VelocityChange);
            // bubbleInstance.SetActive (true);
            // if (bubbleShot) {
            //     NetworkPlayerController.LocalInstance.CmdGetAuthority(bubbleShot.GetComponent<NetworkIdentity>().netId);
            //     bubbleShot.GetComponent<Rigidbody> ().AddForce (source.transform.forward * bubbleForce, ForceMode.VelocityChange);
            //     Object.Destroy (bubbleShot, 15.0f);
            // }
        }

        public void OnDragStart (InputDeviceData deviceData) {
            relativePosition = deviceData.inputDevice.controller.transform.InverseTransformPoint (transform.position);
            relativeRotation = Quaternion.Inverse (deviceData.inputDevice.controller.transform.rotation) * transform.rotation;
        }
        public void DoTouchStart (InputDeviceData deviceData) {
            relativePosition = deviceData.inputDevice.controller.transform.InverseTransformPoint (deviceData.inputDevice.physicsRaySource.transform.position);
            Quaternion rotationDelta = Quaternion.Euler (0, 0, 0); // Quaternion.FromToRotation(deviceData.inputDevice.raySource.transform.forward, deviceData.inputDevice.controller.transform.forward);
            relativeRotation = rotationDelta; // Quaternion.identity;
        }

        public void ApplyTransformation (InputDeviceData deviceData) {
            Vector3 newPosition = deviceData.inputDevice.controller.transform.TransformPoint (relativePosition);
            Quaternion newRotation = deviceData.inputDevice.controller.transform.rotation * relativeRotation;

            rigidbody.MovePosition (newPosition);
            rigidbody.MoveRotation (newRotation);
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }
    }
}
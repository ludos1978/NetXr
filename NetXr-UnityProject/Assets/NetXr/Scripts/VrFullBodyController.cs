using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetXr {
    public class VrFullBodyController : MonoBehaviour {
        Transform vrCameraTransform;
        PlayerPhysics playerPhysics;

        [Header("Aspects of the 3d Model")]
        public Transform bodyTransform;
        public Transform headTransform;
        public float characterHeight = 1.5f;
        public Vector3 characterEyeOffset = new Vector3(0, 0, -0.05f);
        public Vector3 defaultUpRotation = new Vector3(-90, 0, 0);

        [Header("Human playing the game")]
        public float playerSize = 1.8f;

        void Start () {
            //playerEyeHeight = transform.localPosition.y;
            playerPhysics = gameObject.GetComponentInParent<PlayerPhysics>();
            if (playerPhysics != null) {
                // the local player
                vrCameraTransform = gameObject.transform.parent;
            } else {
                // is a remote networked player
                vrCameraTransform = gameObject.transform;
            }
        }

        // Update is called once per frame
        void LateUpdate() {
            transform.localScale = Vector3.one * playerSize / characterHeight; // new Vector3(1, playerSize / 1.5f, 1);

            Quaternion forwardVector = Quaternion.Euler(defaultUpRotation + new Vector3(0, vrCameraTransform.rotation.eulerAngles.y, 0));

            // the local player is attached to the camera and needs to be positioned below it
            if (playerPhysics != null) {
                //transform.localPosition = Vector3.zero;
                //float height = vrCameraTransform.position.y - playerPhysics.transform.position;
                // apply height of play area
                transform.position = new Vector3(vrCameraTransform.position.x, playerPhysics.transform.position.y + playerPhysics.playerHeight, vrCameraTransform.position.z) + forwardVector * characterEyeOffset; // playerPhysics.transform.position + Vector3.up * playerEyeHeight;
                //transform.rotation = Quaternion.identity;
            }

            // keep body vertical
            //Debug.Log("apply rotation " + rot.eulerAngles + " " + transform.rotation.eulerAngles);
            transform.rotation = forwardVector;
        }
    }
}
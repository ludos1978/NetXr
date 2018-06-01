using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetXr {
    public class VrBodyController : MonoBehaviour {
        public string bodyTargetName = "body-head/body";
        //public string leftHandTargetName = "body-head/body/l-hand-target";
        //public string rightHandTargetName = "body-head/body/r-hand-target";
        public Vector3 defaultUpRotation = new Vector3(-90, 0, 0);

        // Update is called once per frame
        void Update() {
            if (NetworkPlayerController.LocalInstance.isLocalPlayer) {
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }

            // keep body vertical
            Quaternion rot = Quaternion.Euler(defaultUpRotation + new Vector3(0, 0, transform.rotation.eulerAngles.y));
            //Debug.Log("apply rotation " + rot.eulerAngles + " " + transform.rotation.eulerAngles);
            transform.Find(bodyTargetName).transform.rotation = rot;
        }
    }
}
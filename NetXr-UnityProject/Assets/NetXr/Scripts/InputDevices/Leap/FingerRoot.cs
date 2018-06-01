//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using UnityEngine;

namespace NetXr {
    [ExecuteInEditMode]
    public class FingerRoot : MonoBehaviour {
        public Vector3 leftPos;
        public Vector3 rightPos;
        public Transform lookAt;

        public void SetLeftHand (bool state) {
            if (state) {
                transform.localPosition = leftPos;
            } else {
                transform.localPosition = rightPos;
            }
        }

        // Update is called once per frame
        void Update () {
            if (lookAt) {
                transform.LookAt (lookAt.position - lookAt.forward * 0.05f);
            }
        }
    }
}
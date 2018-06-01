//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using UnityEngine;

namespace NetXr {
    public class WSNIO_HapticFeedback : MonoBehaviour {
        public float rumbleForce = 1;
        public float rumbleLength = 0.01f;
        public AnimationCurve rumbleCurve;

        // Use this for initialization
        public void StartHapticFeedback(InputDeviceData deviceData) {
            if (rumbleCurve.length == 0) {
                deviceData.inputDevice.inputController.EnableHapticFeedback(rumbleForce, rumbleLength);
            }
            else {
                deviceData.inputDevice.inputController.EnableHapticFeedbackCurve(rumbleForce, rumbleLength, rumbleCurve);
            }
        }
    }
}
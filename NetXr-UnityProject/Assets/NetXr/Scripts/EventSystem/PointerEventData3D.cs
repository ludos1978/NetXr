//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using UnityEngine;
using UnityEngine.EventSystems;

namespace NetXr {
    /// <summary>
    /// Each touch event creates one of these containing all the relevant information.
    /// </summary>
    public class PointerEventData3D : PointerEventData {
        public PointerEventData3D(EventSystem eventSystem) : base(eventSystem) {
        }

        // Current position of the mouse or touch event
        public Vector3 position3d { get; set; }
        // Delta since last update
        public Vector3 delta3d { get; set; }
        // Position of the press event
        public Vector3 pressPosition3d { get; set; }
    }
}

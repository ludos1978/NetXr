//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using UnityEngine;

namespace NetXr {
    public class MouseLock : MonoBehaviour {
        #region Singleton
        private static MouseLock instance = null;
        public static MouseLock Instance {
            get {
                if (instance == null) {
                    instance = (MouseLock) FindObjectOfType (typeof (MouseLock));
                }
                return instance;
            }
        }
        #endregion

        public KeyCode mouseLockReleaseButton = KeyCode.Escape;
        public bool lockMouseOnPress = true;
        public bool mouseCanLock = true;

        void Awake () {
            MouseLock.Instance.SetLock (mouseCanLock);
        }

        void Update () {
            bool altPressed = (Input.GetKey (KeyCode.LeftAlt) || Input.GetKey (KeyCode.RightAlt));
            // if mouse not locked, left mousebutton pressed and not alt pressed
            if ((MouseLock.Instance.GetLocked () == false) && (Input.GetMouseButtonDown (0) && lockMouseOnPress) && (!altPressed)) {
                // lock the mouse
                MouseLock.Instance.SetLock (true);
            }
            if (Input.GetKeyDown (mouseLockReleaseButton)) {
                MouseLock.Instance.SetLock (false);
            }
        }

        public void SetLock (bool state) {
            if (state && mouseCanLock) {
                Cursor.lockState = CursorLockMode.Locked;
                if (PlayerSettingsController.Instance.gazeControllerEnabled) { }
                if (PlayerSettingsController.Instance.mouseControllerEnabled) { }
            } else {
                Cursor.lockState = CursorLockMode.None;
                if (PlayerSettingsController.Instance.mouseControllerEnabled) { }
                if (PlayerSettingsController.Instance.gazeControllerEnabled) { }
            }
        }

        public bool GetLocked () {
            return ((Cursor.lockState == CursorLockMode.Confined) || (Cursor.lockState == CursorLockMode.Locked));
        }
    }
}
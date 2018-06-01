//=========================== Copyright (c) Unknown. ==========================
//
// Source:
// Unity Technologies Standard Assets
//
//=============================================================================

using System.Collections;
using UnityEngine;
using NetXr;

namespace NetXr {
    public class MouseLook : MonoBehaviour {

        private MouseLookHandler mouseLook;

        // Use this for initialization
        void Start () {
            StartMouse ();
        }

        // Update is called once per frame
        void Update () {
            UpdateMouse ();
        }

        void StartMouse () {
            mouseLook = new MouseLookHandler ();
            mouseLook.Init (transform, Camera.main.transform);
        }

        void UpdateMouse () {
            if (NetXr.MouseController.Instance.MouseControlsCamera ()) {
                // non vr player input here
                float x = Input.GetAxis ("Horizontal") * Time.deltaTime * 3.0f;
                float z = Input.GetAxis ("Vertical") * Time.deltaTime * 3.0f;
                float y = Input.GetAxis ("Mouse ScrollWheel") * Time.deltaTime * 30.0f;

                transform.Translate (x, 0, z);
                GetComponent<PlayerPhysics> ().colliderHeight += y;

                mouseLook.LookRotation (transform, Camera.main.transform);
            }
        }
    }
}
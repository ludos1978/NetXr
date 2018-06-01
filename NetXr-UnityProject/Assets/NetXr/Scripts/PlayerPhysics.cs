//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using NetXr;

namespace NetXr { 
    [RequireComponent(typeof(CapsuleCollider), typeof(Rigidbody))]
    public class PlayerPhysics : MonoBehaviour {
        #region Singleton
        private static PlayerPhysics instance = null;
        public static PlayerPhysics Instance {
            get {
                if (instance == null) {
                    instance = ((PlayerPhysics)FindObjectOfType(typeof(PlayerPhysics)));
                }
                return instance;
            }
        }
        #endregion

        public new Transform camera;
        new Rigidbody rigidbody;
        CapsuleCollider playerCollider;

        // when using a mouse, the camera needs to have a fixed height
        public bool vrControlledHeight = true;

        public float playerHeight = 1.5f;
        public float colliderHeight = 1.5f;
        private Vector2 playerPos = Vector2.zero;

        private float playerRadius = 0.1f;
        private float heightAdd = -.15f;
        // private bool networkInitialized = false;

        List<int> setKinematicControllers = new List<int>();

        void Awake () {
            rigidbody = GetComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            playerCollider = GetComponent<CapsuleCollider>();
            NetworkManagerModuleManager.Instance.onClientNetworkConnectEvent.AddListener(OnClientConnect);
        }

        private void OnClientConnect(NetworkConnection arg0) {
            rigidbody.isKinematic = false;
        }

        void FixedUpdate () {
            // when using a vr, the camera is controlled by the headset
            if (vrControlledHeight) {
                Vector3 camPos = camera.transform.localPosition;
                playerHeight = camPos.y;
                colliderHeight = (camPos.y + heightAdd);
                playerPos = new Vector2(camPos.x, camPos.z);
            }
            // when using a mouse the height must be fixed as the camera can move down by physics
            else {
                camera.transform.localPosition = new Vector3(0,colliderHeight,0);
            }
            playerCollider.height = colliderHeight;
            playerCollider.radius = playerRadius;
            playerCollider.center = new Vector3(playerPos.x, colliderHeight / 2, playerPos.y);
        }

        public void SetKinematic (InputDeviceData deviceData, bool state) {
            int deviceId = deviceData.inputDevice.deviceId;
            if (state) {
                if (!setKinematicControllers.Contains(deviceId)) { 
                    setKinematicControllers.Add(deviceId);
                }
            } else {
                if (setKinematicControllers.Contains(deviceId)) {
                    setKinematicControllers.Remove(deviceId);
                }
            }
            Debug.Log("PlayerPhysics.SetKinematic: " + (state ? "add " : "remove ") + deviceId + " kinematic: " + (setKinematicControllers.Count > 0));

            if (setKinematicControllers.Count > 0) {
                rigidbody.isKinematic = true;
            } else {
                rigidbody.isKinematic = false;
                rigidbody.velocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
            }
        }
    }
}
//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace NetXr {
    public class NetworkDiscoveryClient : NetworkDiscovery {
        #region Singleton
        private static NetworkDiscoveryClient instance = null;
        public static NetworkDiscoveryClient Instance {
            get {
                if (instance == null) {
                    instance = ((NetworkDiscoveryClient) FindObjectOfType (typeof (NetworkDiscoveryClient)));
                }
                return instance;
            }
        }
        #endregion

        public override void Start () {
            //Debug.Log("ClientNetworkDiscovery.Start");
            base.Start ();
            base.Initialize ();
            try {
                base.StartAsClient ();
            } catch (Exception e) {
                Debug.LogError ("ClientNetworkDiscovery.Start: ERROR: " + e.ToString ());
                AutoNetworkDiscoveryController.Instance.DiscoveryError ();
            }
            if (!running) {
                Debug.LogError ("ClientNetworkDiscovery.Start: not started!");
                AutoNetworkDiscoveryController.Instance.DiscoveryError ();
            }
        }

        public override void OnReceivedBroadcast (string fromAddress, string data) {
            ParseReceivedData (fromAddress, data);
        }
    }
}
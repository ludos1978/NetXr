//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace NetXr {
    public class NetworkDiscoveryServer : NetworkDiscovery {
        #region Singleton
        private static NetworkDiscoveryServer instance = null;
        public static NetworkDiscoveryServer Instance {
            get {
                if (instance == null) {
                    instance = ((NetworkDiscoveryServer) FindObjectOfType (typeof (NetworkDiscoveryServer)));
                }
                return instance;
            }
        }
        #endregion

        // hide client startup function
        private static new void StartAsClient () {
            Debug.LogError ("ClientNetworkDiscovery.StartAsClient: DISABLED");
        }

        public override void Start () {
            //Debug.Log("ServerNetworkDiscovery.Start");
            base.Start ();
            broadcastData = BroadcastData ();
            //UpdateBroadcastData();
            base.Initialize ();
            base.StartAsServer ();
        }

        public void UpdateBroadcastData () {
            broadcastData = BroadcastData ();
            StartCoroutine (UpdateBroadcastDataCoroutine ());
        }
        public IEnumerator UpdateBroadcastDataCoroutine () {
            if ((hostId != -1) && running) {
                base.StopBroadcast ();
            }
            base.Initialize ();
            yield return new WaitForSeconds (0.1f);
            base.StartAsServer ();
        }
    }
}
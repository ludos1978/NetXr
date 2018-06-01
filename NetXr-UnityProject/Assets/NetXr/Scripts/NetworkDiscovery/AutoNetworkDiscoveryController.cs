//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.VR;

namespace NetXr {
    public class AutoNetworkDiscoveryController : MonoBehaviour {
        #region Singleton
        private static AutoNetworkDiscoveryController instance = null;
        public static AutoNetworkDiscoveryController Instance {
            get {
                if (instance == null) {
                    instance = ((AutoNetworkDiscoveryController) FindObjectOfType (typeof (AutoNetworkDiscoveryController)));
                }
                return instance;
            }
        }
        #endregion

        public enum NETWORK_STATE {
            //MAYBE_SERVER, // we will be server, but have not started yet
            //SERVER, // we have started the server
            MAYBE_HOST,
            HOST,
            INDIFFERENT, // we could be server, but need to decide with peers
            MAYBE_CLIENT,
            CLIENT, // we cannot be server
            DISCONNECTED
        }

        public int broadcastPort = 47777;
        public int networkingPort = 7777;

        public NETWORK_STATE networkState = NETWORK_STATE.INDIFFERENT;
        public int requireHostCount = 2;
        public float autostartDelay = 5;

        public float autostartTimer = 0.0f;

        public UnityEngine.UI.Text networkstateLogText;

        // Use this for initialization
        void Awake () {
            DontDestroyOnLoad (transform.gameObject);

            StartCoroutine (NetworkDiscoveryUpdateCoroutine ());

            NetworkDiscoveryClient.Instance.broadcastPort = broadcastPort;
            NetworkDiscoveryServer.Instance.broadcastPort = broadcastPort;
            NetworkManagerModuleManager.Instance.networkPort = networkingPort;
        }

        // Update is called once per frame
        void Update () { }

        IEnumerator NetworkDiscoveryUpdateCoroutine () {
            // wait 2 seconds before starting to determine host state (wait for incoming data)
            yield return new WaitForSeconds (2.0f);

            while (true) {
                // update network discovery while we are not started
                if ((networkState != NETWORK_STATE.HOST) && (networkState != NETWORK_STATE.CLIENT)) {
                    NetworkDiscoveryUpdate ();
                }
                // update log text
                if (networkstateLogText != null) {
                    networkstateLogText.text = networkState.ToString ("G");
                }
                // delay
                yield return new WaitForSeconds (0.1f);
            }
        }
        /// <summary>
        /// Run every 0.1 seconds from NetworkDiscoveryUpdateCoroutine to update according to Broadcasted data
        /// </summary>
        void NetworkDiscoveryUpdate () {
            // require the amount of clients given
            List<HostEntry> remoteHosts = NetworkDiscoveryClient.Instance.GetRemoteHosts ();

            switch (networkState) {
                case NETWORK_STATE.MAYBE_CLIENT:
                    DetermineClientServerState (remoteHosts);
                    break;
                    /*case NETWORK_STATE.MAYBE_SERVER:
                        DetermineClientServerState(remoteHosts);
                        break;*/
                case NETWORK_STATE.MAYBE_HOST:
                    DetermineClientServerState (remoteHosts);
                    break;
                case NETWORK_STATE.INDIFFERENT:
                    DetermineClientServerState (remoteHosts);
                    break;

                    // if we are the host, dont do anything
                case NETWORK_STATE.HOST:
                    break;
                    /*case NETWORK_STATE.SERVER:
                        break;*/
                    // if we are a client, dont do anything
                case NETWORK_STATE.CLIENT:
                    break;
            }

            if (remoteHosts.Count >= requireHostCount) {
                // we have waited long enougth to start up
                if ((Time.time - NetworkDiscoveryClient.Instance.hostsMutedTime) > autostartDelay) {
                    StartUp (remoteHosts);
                }
            } else {
                autostartTimer = Time.time;
            }
        }

        /// <summary>
        /// This function is called while trying to determine the state of the local process
        /// </summary>
        void DetermineClientServerState (List<HostEntry> remoteHosts) {
            autostartTimer = Time.time;

            //int maxPrio = int.MinValue;
            //int maxPrioHost = -1;
            foreach (HostEntry remoteHost in remoteHosts) {
                if (NetworkDiscoveryClient.Instance.LocalServerPriority () < remoteHost.serverPriority) {
                    if (networkState != NETWORK_STATE.MAYBE_CLIENT) {
                        Debug.Log ("NetworkSettingsController.DetermineClientServerState: switch local host to NETWORK_STATE.MAYBE_CLIENT");
                        networkState = NETWORK_STATE.MAYBE_CLIENT;
                        NetworkDiscoveryServer.Instance.UpdateBroadcastData ();
                    }
                    break;
                }
            }

            // still indifferent, likely i should be the server
            if (networkState == NETWORK_STATE.INDIFFERENT) {
                // are all known remote computers clients?
                bool allClients = true;
                bool anyServer = false;
                foreach (HostEntry remoteHost in remoteHosts) {
                    if (remoteHost.networkState != NETWORK_STATE.MAYBE_CLIENT) {
                        allClients = false;
                    }
                    if (remoteHost.networkState == NETWORK_STATE.HOST) {
                        anyServer = true;
                    }
                }

                if (allClients && !anyServer) {
                    if (networkState != NETWORK_STATE.MAYBE_HOST) {
                        Debug.Log ("SettingsController.DetermineClientServerState: switch local host to NETWORK_STATE.MAYBE_SERVER (all clients: " + allClients + " / any servers: " + anyServer + ")");
                        networkState = NETWORK_STATE.MAYBE_HOST;
                        NetworkDiscoveryServer.Instance.UpdateBroadcastData ();
                    }
                }

                if (anyServer) {
                    Debug.Log ("SettingsController.DetermineClientServerState: switch local host to NETWORK_STATE.MAYBE_CLIENT (all clients: " + allClients + " / any servers: " + anyServer + ")");
                    networkState = NETWORK_STATE.MAYBE_CLIENT;
                    NetworkDiscoveryServer.Instance.UpdateBroadcastData ();
                }
            }
        }

        /// <summary>
        /// This function is called if no change in the hosts list have occured after "autostartDelay"
        /// </summary>
        void StartUp (List<HostEntry> remoteHosts) {
            // check if any host is still indifferent
            foreach (HostEntry remoteHost in remoteHosts) {
                if (remoteHost.networkState == NETWORK_STATE.INDIFFERENT) {
                    Debug.LogError ("NetworkSettingsController.NetworkDiscoveryUpdate: ERROR remote host is still indifferent, while we are now ready to start!!!");
                    return;
                }
            }

            // can start now, should have handled server/client determination by now
            switch (networkState) {
                case NETWORK_STATE.MAYBE_HOST:
                    StartHost ();
                    return;
                case NETWORK_STATE.MAYBE_CLIENT:
                    // check if any remote host is in host (server) mode
                    foreach (HostEntry remoteHost in remoteHosts) {
                        if (remoteHost.networkState == NETWORK_STATE.HOST) {
                            StartClient (remoteHost.ip);
                            return;
                        }
                    }
                    // we detected no remote host in host (server) mode, so we may have a problem here...
                    Debug.Log ("NetworkSettingsController.NetworkDiscoveryUpdate: no of the remote hosts ins in host mode, skip starting of client mode");
                    break;
                case NETWORK_STATE.HOST:
                    Debug.LogError ("NetworkSettingsController.NetworkDiscoveryUpdate: ERROR this host is already in server state");
                    break;
                case NETWORK_STATE.CLIENT:
                    Debug.LogError ("NetworkSettingsController.NetworkDiscoveryUpdate: ERROR this host is already in client state");
                    break;

                default:
                    Debug.LogError ("NetworkSettingsController.NetworkDiscoveryUpdate: ERROR this host is still indifferent, while we should be set by now");
                    break;
            }
        }

        /// <summary>
        /// start this software instance as host (server & client)
        /// </summary>
        void StartHost () {
            Debug.Log ("NetworkSettingsController.StartHost");
            //CustomNetworkManager.singleton.networkAddress = fromAddress;
            networkState = NETWORK_STATE.HOST;
            NetworkDiscoveryServer.Instance.UpdateBroadcastData ();
#if false
            //ConnectionConfig connCfg = new ConnectionConfig();
            //NetworkManagerModuleManager.Instance.StartHost(connCfg, 16);
            UnityEngine.Networking.Match.MatchInfo matchInfo = new UnityEngine.Networking.Match.MatchInfo ();
            NetworkManagerModuleManager.Instance.StartHost (matchInfo);
#else
            //NetworkManagerModuleManager.Instance.networkAddress = 
            //NetworkManagerModuleManager.Instance.networkPort = 
            NetworkManagerModuleManager.Instance.StartHost ();
#endif
        }

        /*void StartServer () {
            Debug.Log("SettingsController.StartServer:");
            //CustomNetworkManager.singleton.networkAddress = fromAddress;
            networkState = NETWORK_STATE.SERVER;
            ServerNetworkDiscovery.Instance.UpdateBroadcastData();
            CustomNetworkManager.singleton.StartServer();
        }*/

        /// <summary>
        /// Start this software instance as client
        /// </summary>
        void StartClient (string serverAddress) {
            //Debug.Log("NetworkSettingsController.StartClient: "+serverAddress+":"+networkingPort);
            networkState = NETWORK_STATE.CLIENT;
            NetworkDiscoveryServer.Instance.UpdateBroadcastData ();
            NetworkManagerModuleManager.Instance.networkAddress = serverAddress;
#if false
            UnityEngine.Networking.Match.MatchInfo matchInfo = new UnityEngine.Networking.Match.MatchInfo ();
            ConnectionConfig connCfg = new ConnectionConfig ();
            NetworkClient netClient = NetworkManagerModuleManager.Instance.StartClient (matchInfo, connCfg);
#else
            NetworkClient netClient = NetworkManagerModuleManager.Instance.StartClient ();
#endif
            Debug.Log ("NetworkSettingsController.StartClient: client started " + netClient.serverIp + " " + netClient.serverPort + " " + netClient.isConnected);
        }

        public void DiscoveryError () {
            // starting the discovery service failed, currently only because of blocked ports possible
            StartClient ("localhost");
        }

        public void Disconnected () {
            networkState = NETWORK_STATE.DISCONNECTED;
        }
    }

}
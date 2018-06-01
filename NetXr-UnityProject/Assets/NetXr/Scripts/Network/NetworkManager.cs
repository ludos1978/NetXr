//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.VR;

namespace NetXr {
    public class NetworkManager : MonoBehaviour {
        #region Singleton
        private static NetworkManager instance = null;
        public static NetworkManager Instance {
            get {
                if (instance == null) {
                    instance = ((NetworkManager) FindObjectOfType (typeof (NetworkManager)));
                }
                return instance;
            }
        }
        #endregion

        public bool ShouldBeServer;

        //public GameObject vrPlayerPrefab;
        private int playerCount = 0;
        [HideInInspector]
        private GameObject networkPlayerController;

        public Camera activeCamera;

        // USING AWAKE CAUSES AN ERROR IN UNITY 5.4.1f1 (reload of networking!)
        // void Awake () {
        //}

        void OnEnable () {
            // set all prefabs disable, so we can call things before start & onEnable
            NetworkManagerModuleManager.Instance.playerPrefab.SetActive (false);
            //        vrPlayerPrefab.SetActive(false);

            // try fix the network connection losses
            NetworkManagerModuleManager.Instance.connectionConfig.NetworkDropThreshold = 90;

            NetworkManagerModuleManager.Instance.autoCreatePlayer = false;

            // load configuration
            var settingsPath = Application.dataPath + "/settings.cfg";
            if (File.Exists (settingsPath)) {
                StreamReader textReader = new StreamReader (settingsPath, System.Text.Encoding.ASCII);
                ShouldBeServer = textReader.ReadLine () == "Server";
                NetworkManagerModuleManager.Instance.networkAddress = textReader.ReadLine ();
                textReader.Close ();
            }

            NetworkManagerModuleManager.Instance.onClientNetworkConnectEvent.AddListener (OnClientConnect);
            NetworkManagerModuleManager.Instance.onClientDisconnectEvent.AddListener (OnClientDisconnect);
            NetworkManagerModuleManager.Instance.onServerAddPlayerEvent.AddListener (OnServerAddPlayer);
            NetworkManagerModuleManager.Instance.onServerRemovePlayerEvent.AddListener (OnServerRemovePlayer);
            NetworkManagerModuleManager.Instance.onDropConnectionEvent.AddListener (OnDropConnection);
            NetworkManagerModuleManager.Instance.onServerDisconnectEvent.AddListener (OnServerDisconnect);
            NetworkManagerModuleManager.Instance.onServerErrorEvent.AddListener (OnServerError);
        }

        void OnDisable () {
            NetworkManagerModuleManager.Instance.onClientNetworkConnectEvent.RemoveListener (OnClientConnect);
            NetworkManagerModuleManager.Instance.onClientDisconnectEvent.RemoveListener (OnClientDisconnect);
            NetworkManagerModuleManager.Instance.onServerAddPlayerEvent.RemoveListener (OnServerAddPlayer);
            NetworkManagerModuleManager.Instance.onServerRemovePlayerEvent.RemoveListener (OnServerRemovePlayer);
            NetworkManagerModuleManager.Instance.onDropConnectionEvent.RemoveListener (OnDropConnection);
            NetworkManagerModuleManager.Instance.onServerDisconnectEvent.RemoveListener (OnServerDisconnect);
            NetworkManagerModuleManager.Instance.onServerErrorEvent.RemoveListener (OnServerError);
        }

        void Start () { }

        /// <summary>
        /// Called on the client when connected to a server.
        /// The default implementation of this function sets the client as ready and adds a player.
        /// </summary>
        public void OnClientConnect (NetworkConnection conn) {
            Debug.Log ("CustomNetworkManager.OnClientConnect: connected to server " + conn.address);
            //base.OnClientConnect(conn);

            SpawnMessage extraMessage = new SpawnMessage ();

            ClientScene.AddPlayer (NetworkManagerModuleManager.Instance.client.connection, 0, extraMessage);
            NetworkManagerModuleManager.Instance.client.connection.isReady = true;
            //ClientScene.Ready (client.connection);

        }
        /// <summary>
        /// Called on clients when disconnected from a server.
        /// </summary>
        public void OnClientDisconnect (NetworkConnection conn) {
            //base.OnClientDisconnect(conn);
            AutoNetworkDiscoveryController.Instance.Disconnected ();
            Debug.LogError ("CustomNetworkManager.OnClientDisconnect: server connection lost " + conn);
        }

        /// <summary>
        /// Called on the server when a client adds a new player with ClientScene.AddPlayer.
        /// </summary>
        public void OnServerAddPlayer (NetworkConnection conn, short playerControllerId, NetworkReader extraMessageReader) {
            SpawnMessage message = new SpawnMessage ();
            message.Deserialize (extraMessageReader);

            //Transform spawnPoint = this.startPositions [playerCount];
            Vector3 spawnPos = Vector3.zero;
            Quaternion spawnRot = Quaternion.identity;
            Transform spawnPoint = NetworkManagerModuleManager.Instance.GetStartPosition ();
            if (spawnPoint != null) {
                spawnPos = spawnPoint.position;
                spawnRot = spawnPoint.rotation;
                Debug.Log("using spawn start position " + spawnPos + " rotation " + spawnRot);
            } else {
                spawnPos = new Vector3(0, 0, 0);
                spawnRot = Quaternion.identity;
                Debug.Log("using fixed start position " + spawnPos + " rotation " + spawnRot);
            }

            Debug.Log ("CustomNetworkManager.OnServerAddPlayer: address: " + conn.address + " playerControllerId " + playerControllerId + " spawn: " + spawnPoint + " prefab: " + NetworkManagerModuleManager.Instance.playerPrefab);
            try { 
                networkPlayerController = (GameObject) Instantiate (NetworkManagerModuleManager.Instance.playerPrefab, spawnPos, spawnRot);
                networkPlayerController.SetActive (true);
            } catch (Exception e) {
                Debug.LogError("ERROR: you probably have assigned a scene instance of the player prefab instead of the prefab file in NetworkManager\n"+e.ToString());
            }
            //Debug.Log("CustomNetworkManager.OnServerAddPlayer: enabled "+ networkPlayerController.name + " " + networkPlayerController.activeSelf);

            //Debug.LogError("CustomNetworkManager.OnServerAddPlayer: adding player to connection");
            NetworkServer.AddPlayerForConnection (conn, networkPlayerController, playerControllerId);

            activeCamera = networkPlayerController.GetComponentInChildren<Camera> ();

            //GiC_SoundingObjectsManager.Instance.SanitizeAtoms();
            //GiC_SoundingObjectsManager.Instance.ResendAllAtoms();
        }

        /// <summary>
        /// Called on the server when a client removes a player.
        /// </summary>
        public void OnServerRemovePlayer (NetworkConnection conn, UnityEngine.Networking.PlayerController player) {
            Debug.LogWarning ("CustomNetworkManager.OnServerRemovePlayer");
            playerCount--;
        }

        /// <summary>
        /// Callback that happens when a NetworkMatch.DropConnection match request has been processed on the server.
        /// </summary>
        private void OnDropConnection (bool success, string extendedInfo) {
            Debug.LogWarning ("CustomNetworkManager.OnDropConnection: " + success + ": " + extendedInfo);
        }

        /// <summary>
        /// Called on the server when a client disconnects.
        /// </summary>
        private void OnServerDisconnect (NetworkConnection netConn) {
            Debug.LogWarning ("CustomNetworkManager.OnServerDisconnect: " + netConn);
            // clean up atoms because the user may take an atom with him when loosing the connection
            //NetworkManager.singleton.
            //GiC_SoundingObjectsManager.Instance.SanitizeAtoms();
        }

        /// <summary>
        /// Called on the server when a network error occurs for a client connection.
        /// </summary>
        private void OnServerError (NetworkConnection netConn, int errorCode) {
            Debug.LogWarning ("CustomNetworkManager.OnServerError: " + netConn + " errCode " + errorCode);
        }
    }
}
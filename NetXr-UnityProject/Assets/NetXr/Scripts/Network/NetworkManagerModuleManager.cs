//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;

namespace NetXr {
    [System.Serializable]
    public class UnityNetworkEvent : UnityEvent { }
        [System.Serializable]
    public class UnityNetworkClientEvent : UnityEvent<NetworkClient> { }
        [System.Serializable]
    public class UnityNetworkConnectionEvent : UnityEvent<NetworkConnection> { }
        [System.Serializable]
    public class UnityNetworkConnectionIntEvent : UnityEvent<NetworkConnection, int> { }
        [System.Serializable]
    public class UnityNetworkConnectionShortReaderEvent : UnityEvent<NetworkConnection, short, NetworkReader> { }
        [System.Serializable]
    public class UnityNetworkConnectionPlayerControllerEvent : UnityEvent<NetworkConnection, PlayerController> { }
        [System.Serializable]
    public class UnityNetworkStringEvent : UnityEvent<string> { }
        [System.Serializable]
    public class UnityNetworkBoolStringEvent : UnityEvent<bool, string> { }
        [System.Serializable]
    public class UnityNetworkBoolStringMatchInfoEvent : UnityEvent<bool, string, MatchInfo> { }
        [System.Serializable]
    public class UnityNetworkBoolStringMatchListEvent : UnityEvent<bool, string, List<MatchInfoSnapshot>> { }
        [System.Serializable]
    public class UnityNetworkBehaviourEvent : UnityEvent<NetworkBehaviour> { }

    /// <summary>
    /// callbacks for networking related events
    /// </summary>
    public class NetworkManagerModuleManager : UnityEngine.Networking.NetworkManager {
        #region Singleton
        private static NetworkManagerModuleManager instance = null;
        public static NetworkManagerModuleManager Instance {
            get {
                if (instance == null) {
                    instance = ((NetworkManagerModuleManager) FindObjectOfType (typeof (NetworkManagerModuleManager)));
                }
                return instance;
            }
        }
        #endregion

        #region START
        public UnityNetworkEvent onStartServerEvent;
        /// <summary>
        /// This hook is invoked when a server is started - including when a host is started.
        /// </summary>
        public override void OnStartServer () {
            base.OnStartServer ();
            onStartServerEvent.Invoke ();
        }

        public UnityNetworkEvent onStartHostEvent;
        /// <summary>
        /// This hook is invoked when a host is started.
        /// </summary>
        public override void OnStartHost () {
            base.OnStartHost ();
            onStartHostEvent.Invoke ();
        }

        public UnityNetworkClientEvent onStartClientEvent;
        /// <summary>
        /// This is a hook that is invoked when the client is started.
        /// </summary>
        public override void OnStartClient (NetworkClient client) {
            base.OnStartClient (client);
            onStartClientEvent.Invoke (client);
        }
        #endregion

        #region STOP
        public UnityNetworkEvent onStopServerEvent;
        /// <summary>
        /// This hook is called when a client is stopped.
        /// </summary>
        public override void OnStopServer () {
            base.OnStopServer ();
            onStopServerEvent.Invoke ();
        }

        public UnityNetworkEvent onStopHostEvent;
        /// <summary>
        /// This hook is called when a host is stopped.
        /// </summary>
        public override void OnStopHost () {
            base.OnStopHost ();
            onStopHostEvent.Invoke ();
        }

        public UnityNetworkEvent onStopClientEvent;
        /// <summary>
        /// This hook is called when a client is stopped.
        /// </summary>
        public override void OnStopClient () {
            base.OnStopClient ();
            onStopClientEvent.Invoke ();
        }
        #endregion

        #region CLIENT
        public UnityNetworkConnectionEvent onClientNetworkConnectEvent;
        /// <summary>
        /// Called on the client when connected to a server.
        /// </summary>
        public override void OnClientConnect (NetworkConnection conn) {
            base.OnClientConnect (conn);
            onClientNetworkConnectEvent.Invoke (conn);
        }

        public UnityNetworkConnectionEvent onClientDisconnectEvent;
        /// <summary>
        /// Called on clients when disconnected from a server.
        /// </summary>
        public override void OnClientDisconnect (NetworkConnection conn) {
            base.OnClientDisconnect (conn);
            onClientDisconnectEvent.Invoke (conn);
        }

        public UnityNetworkConnectionIntEvent onClientErrorEvent;
        /// <summary>
        /// Called on clients when a network error occurs.
        /// </summary>
        public override void OnClientError (NetworkConnection conn, int errorCode) {
            base.OnClientError (conn, errorCode);
            onClientErrorEvent.Invoke (conn, errorCode);
        }

        public UnityNetworkConnectionEvent onClientNotReadyEvent;
        /// <summary>
        /// Called on clients when a servers tells the client it is no longer ready.
        /// </summary>
        public override void OnClientNotReady (NetworkConnection conn) {
            base.OnClientNotReady (conn);
            onClientNotReadyEvent.Invoke (conn);
        }

        public UnityNetworkConnectionEvent onClientSceneChangedEvent;
        /// <summary>
        /// Called on clients when a scene has completed loaded, when the scene load was initiated by the server.
        /// </summary>
        public override void OnClientSceneChanged (NetworkConnection conn) {
            base.OnClientSceneChanged (conn);
            onClientSceneChangedEvent.Invoke (conn);
        }
        #endregion

        #region SERVER
        public UnityNetworkConnectionEvent onServerConnectEvent;
        /// <summary>
        /// Called on the server when a new client connects.
        /// </summary>
        public override void OnServerConnect (NetworkConnection conn) {
            base.OnServerConnect (conn);
            onServerConnectEvent.Invoke (conn);
        }

        public UnityNetworkConnectionEvent onServerDisconnectEvent;
        /// <summary>
        /// Called on the server when a client disconnects.
        /// </summary>
        public override void OnServerDisconnect (NetworkConnection conn) {
            base.OnServerDisconnect (conn);
            onServerDisconnectEvent.Invoke (conn);
        }

        public UnityNetworkConnectionIntEvent onServerErrorEvent;
        /// <summary>
        /// Called on the server when a network error occurs for a client connection.
        /// </summary>
        public override void OnServerError (NetworkConnection conn, int errorCode) {
            base.OnServerError (conn, errorCode);
            onServerErrorEvent.Invoke (conn, errorCode);
        }

        public UnityNetworkConnectionEvent onServerReadyEvent;
        /// <summary>
        /// Called on the server when a client is ready.
        /// </summary>
        public override void OnServerReady (NetworkConnection conn) {
            base.OnServerReady (conn);
            onServerReadyEvent.Invoke (conn);
        }

        public UnityNetworkConnectionShortReaderEvent onServerAddPlayerEvent;
        /// <summary>
        /// Called on the server when a client adds a new player with ClientScene.AddPlayer.
        /// </summary>
        public override void OnServerAddPlayer (NetworkConnection conn, short playerControllerId, NetworkReader extraMessageReader) {
            if (autoCreatePlayer) {
                Debug.Log ("NetworkManagerModuleManager.OnServerAddPlayer: autoCreatePlayer");
                base.OnServerAddPlayer (conn, playerControllerId, extraMessageReader);
            }
            onServerAddPlayerEvent.Invoke (conn, playerControllerId, extraMessageReader);
        }

        //public delegate void OnServerAddPlayerDelegate(NetworkConnection conn, short playerControllerId);
        //public OnServerAddPlayerDelegate onServerAddPlayerDelegate;
        /// <summary>
        /// Called on the server when a client adds a new player with ClientScene.AddPlayer.
        /// </summary>
        //public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId) {
        //    if (onServerAddPlayerDelegate != null) {
        //        onServerAddPlayerDelegate(conn, playerControllerId);
        //    }
        //    base.OnServerAddPlayer(conn, playerControllerId);
        //}

        public UnityNetworkConnectionPlayerControllerEvent onServerRemovePlayerEvent;
        /// <summary>
        /// Called on the server when a client removes a player.
        /// </summary>
        public override void OnServerRemovePlayer (NetworkConnection conn, PlayerController player) {
            base.OnServerRemovePlayer (conn, player);
            onServerRemovePlayerEvent.Invoke (conn, player);
        }

        public UnityNetworkStringEvent onServerSceneChangedEvent;
        /// <summary>
        /// Called on the server when a scene is completed loaded, when the scene load was initiated by the server with ServerChangeScene().
        /// </summary>
        public override void OnServerSceneChanged (string sceneName) {
            base.OnServerSceneChanged (sceneName);
            onServerSceneChangedEvent.Invoke (sceneName);
        }

        public UnityNetworkStringEvent serverChangeSceneEvent;
        /// <summary>
        /// Called on the server when a scene is completed loaded, when the scene load was initiated by the server with ServerChangeScene().
        /// </summary>
        public override void ServerChangeScene (string newSceneName) {
            base.ServerChangeScene (newSceneName);
            serverChangeSceneEvent.Invoke (newSceneName);
        }
        #endregion

        #region MATCH
        public UnityNetworkBoolStringMatchInfoEvent onMatchCreateEvent;
        /// <summary>
        /// Callback that happens when a NetworkMatch.CreateMatch request has been processed on the server.
        /// </summary>
        public override void OnMatchCreate (bool success, string extendedInfo, MatchInfo matchInfo) {
            base.OnMatchCreate (success, extendedInfo, matchInfo);
            onMatchCreateEvent.Invoke (success, extendedInfo, matchInfo);
        }

        public UnityNetworkBoolStringMatchInfoEvent onMatchJoinedEvent;
        /// <summary>
        /// Callback that happens when a NetworkMatch.JoinMatch request has been processed on the server.
        /// </summary>
        public override void OnMatchJoined (bool success, string extendedInfo, MatchInfo matchInfo) {
            base.OnMatchJoined (success, extendedInfo, matchInfo);
            onMatchJoinedEvent.Invoke (success, extendedInfo, matchInfo);
        }

        public UnityNetworkBoolStringEvent onDestroyMatchEvent;
        /// <summary>
        /// Callback that happens when a NetworkMatch.DestroyMatch request has been processed on the server.
        /// </summary>
        public override void OnDestroyMatch (bool success, string extendedInfo) {
            base.OnDestroyMatch (success, extendedInfo);
            onDestroyMatchEvent.Invoke (success, extendedInfo);
        }
        public UnityNetworkBoolStringMatchListEvent onMatchListEvent;
        /// <summary>
        /// Callback that happens when a NetworkMatch.ListMatches request has been processed on the server.
        /// </summary>
        public override void OnMatchList (bool success, string extendedInfo, List<MatchInfoSnapshot> matchList) {
            base.OnMatchList (success, extendedInfo, matchList);
            onMatchListEvent.Invoke (success, extendedInfo, matchList);
        }

        public UnityNetworkBoolStringEvent onSetMatchAttributesEvent;
        /// <summary>
        /// Callback that happens when a NetworkMatch.SetMatchAttributes has been processed on the server.
        /// </summary>
        public override void OnSetMatchAttributes (bool success, string extendedInfo) {
            base.OnSetMatchAttributes (success, extendedInfo);
            onSetMatchAttributesEvent.Invoke (success, extendedInfo);
        }

        public UnityNetworkBoolStringEvent onDropConnectionEvent;
        /// <summary>
        /// Callback that happens when a NetworkMatch.DropConnection match request has been processed on the server.
        /// </summary>
        public override void OnDropConnection (bool success, string extendedInfo) {
            base.OnDropConnection (success, extendedInfo);
            onDropConnectionEvent.Invoke (success, extendedInfo);
        }
        #endregion

        #region CUSTOM
        public UnityNetworkBehaviourEvent onStartLocalPlayerEvent;
        /// <summary>
        /// Callback that when the player has been connectd to the server
        /// </summary>
        public void OnStartLocalPlayerCompleted (NetworkBehaviour netBehaviour) {
            onStartLocalPlayerEvent.Invoke (netBehaviour);
        }
        #endregion
    }
}
//======= Copyright (c) Reto Spoerri, All rights reserved. ===============
//
// Purpose: 
//
//=============================================================================

//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;

namespace NetXr {
    public class NetworkManagerModule : UnityEngine.Networking.NetworkManager {
        #region START
        /// <summary>
        /// This hook is invoked when a server is started - including when a host is started.
        /// </summary>
        public override void OnStartServer () {
            //base.OnStartServer();
        }

        /// <summary>
        /// This hook is invoked when a host is started.
        /// </summary>
        public override void OnStartHost () {
            //base.OnStartHost();
        }

        /// <summary>
        /// This is a hook that is invoked when the client is started.
        /// </summary>
        public override void OnStartClient (NetworkClient client) {
            //base.OnStartClient(client);
        }
        #endregion

        #region STOP
        /// <summary>
        /// This hook is called when a client is stopped.
        /// </summary>
        public override void OnStopServer () {
            //base.OnStopServer();
        }

        /// <summary>
        /// This hook is called when a host is stopped.
        /// </summary>
        public override void OnStopHost () {
            //base.OnStopHost();
        }

        /// <summary>
        /// This hook is called when a client is stopped.
        /// </summary>
        public override void OnStopClient () {
            //base.OnStopClient();
        }
        #endregion

        #region CLIENT
        /// <summary>
        /// Called on the client when connected to a server.
        /// </summary>
        public override void OnClientConnect (NetworkConnection conn) {
            //base.OnClientConnect(conn);
        }

        /// <summary>
        /// Called on clients when disconnected from a server.
        /// </summary>
        public override void OnClientDisconnect (NetworkConnection conn) {
            //base.OnClientDisconnect(conn);
        }

        /// <summary>
        /// Called on clients when a network error occurs.
        /// </summary>
        public override void OnClientError (NetworkConnection conn, int errorCode) {
            //base.OnClientError(conn, errorCode);
        }

        /// <summary>
        /// Called on clients when a servers tells the client it is no longer ready.
        /// </summary>
        public override void OnClientNotReady (NetworkConnection conn) {
            //base.OnClientNotReady(conn);
        }

        /// <summary>
        /// Called on clients when a scene has completed loaded, when the scene load was initiated by the server.
        /// </summary>
        public override void OnClientSceneChanged (NetworkConnection conn) {
            //base.OnClientSceneChanged(conn);
        }
        #endregion

        #region SERVER
        /// <summary>
        /// Called on the server when a new client connects.
        /// </summary>
        public override void OnServerConnect (NetworkConnection conn) {
            //base.OnServerConnect(conn);
        }

        /// <summary>
        /// Called on the server when a client disconnects.
        /// </summary>
        public override void OnServerDisconnect (NetworkConnection conn) {
            //base.OnServerDisconnect(conn);
        }

        /// <summary>
        /// Called on the server when a network error occurs for a client connection.
        /// </summary>
        public override void OnServerError (NetworkConnection conn, int errorCode) {
            //base.OnServerError(conn, errorCode);
        }

        /// <summary>
        /// Called on the server when a client is ready.
        /// </summary>
        public override void OnServerReady (NetworkConnection conn) {
            //base.OnServerReady(conn);
        }

        /// <summary>
        /// Called on the server when a client adds a new player with ClientScene.AddPlayer.
        /// </summary>
        public override void OnServerAddPlayer (NetworkConnection conn, short playerControllerId, NetworkReader extraMessageReader) {
            //base.OnServerAddPlayer(conn, playerControllerId, extraMessageReader);
        }

        /// <summary>
        /// Called on the server when a client adds a new player with ClientScene.AddPlayer.
        /// </summary>
        public override void OnServerAddPlayer (NetworkConnection conn, short playerControllerId) {
            //base.OnServerAddPlayer(conn, playerControllerId);
        }

        /// <summary>
        /// Called on the server when a client removes a player.
        /// </summary>
        public override void OnServerRemovePlayer (NetworkConnection conn, PlayerController player) {
            //base.OnServerRemovePlayer(conn, player);
        }

        /// <summary>
        /// Called on the server when a scene is completed loaded, when the scene load was initiated by the server with ServerChangeScene().
        /// </summary>
        public override void OnServerSceneChanged (string sceneName) {
            //base.OnServerSceneChanged(sceneName);
        }

        /// <summary>
        /// Called on the server when a scene is completed loaded, when the scene load was initiated by the server with ServerChangeScene().
        /// </summary>
        public override void ServerChangeScene (string newSceneName) {
            //base.ServerChangeScene(newSceneName);
        }

        /// <summary>
        /// Callback that happens when a NetworkMatch.DropConnection match request has been processed on the server.
        /// </summary>
        public override void OnDropConnection (bool success, string extendedInfo) {
            //base.OnDropConnection(success, extendedInfo);
        }
        #endregion

        #region MATCH
        /// <summary>
        /// Callback that happens when a NetworkMatch.CreateMatch request has been processed on the server.
        /// </summary>
        public override void OnMatchCreate (bool success, string extendedInfo, MatchInfo matchInfo) {
            //base.OnMatchCreate(success, extendedInfo, matchInfo);
        }

        /// <summary>
        /// Callback that happens when a NetworkMatch.JoinMatch request has been processed on the server.
        /// </summary>
        public override void OnMatchJoined (bool success, string extendedInfo, MatchInfo matchInfo) {
            //base.OnMatchJoined(success, extendedInfo, matchInfo);
        }

        /// <summary>
        /// Callback that happens when a NetworkMatch.DestroyMatch request has been processed on the server.
        /// </summary>
        public override void OnDestroyMatch (bool success, string extendedInfo) {
            //base.OnDestroyMatch(success, extendedInfo);
        }

        /// <summary>
        /// Callback that happens when a NetworkMatch.ListMatches request has been processed on the server.
        /// </summary>
        public override void OnMatchList (bool success, string extendedInfo, List<MatchInfoSnapshot> matchList) {
            //base.OnMatchList(success, extendedInfo, matchList);
        }

        /// <summary>
        /// Callback that happens when a NetworkMatch.SetMatchAttributes has been processed on the server.
        /// </summary>
        public override void OnSetMatchAttributes (bool success, string extendedInfo) {
            //base.OnSetMatchAttributes(success, extendedInfo);
        }
        #endregion

        /// <summary>
        /// This starts a network "host" - a server and client in the same application.
        /// </summary>
        //public override NetworkClient StartHost(ConnectionConfig config, int maxConnections) {
        //    return base.StartHost(config, maxConnections);
        //}

        /// <summary>
        /// This starts a network "host" - a server and client in the same application.
        /// </summary>
        //public override NetworkClient StartHost(MatchInfo info) {
        //    return base.StartHost(info);
        //}

        /// <summary>
        /// This starts a network "host" - a server and client in the same application.
        /// </summary>
        //public override NetworkClient StartHost() {
        //    return base.StartHost();
        //}
    }
}
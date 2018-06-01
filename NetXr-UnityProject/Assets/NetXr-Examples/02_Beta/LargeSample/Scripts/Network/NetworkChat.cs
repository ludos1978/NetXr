//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace NetXr {
    public class ChatMessages {
        public enum ChatMessageTypes {
            CLIENT_CHAT_MESSAGE = 1000,
            SERVER_CHAT_MESSAGE = 1001
        }

        public class ClientChatMessage : MessageBase {
            public string message;
        }
        public class ServerChatMessage : MessageBase {
            public string message;
            public string sender;
        }
    }

    public class NetworkChat : MonoBehaviour {
        #region Singleton
        private static NetworkChat instance = null;
        public static NetworkChat Instance {
            get {
                if (instance == null) {
                    instance = ((NetworkChat) FindObjectOfType (typeof (NetworkChat)));
                }
                return instance;
            }
        }
        #endregion

        public ChatLog chatLog;

        public void OnEnable () {
            NetworkManagerModuleManager.Instance.onStartClientEvent.AddListener (OnStartClient);
            NetworkManagerModuleManager.Instance.onStopClientEvent.AddListener (OnStopClient);
            NetworkManagerModuleManager.Instance.onStartServerEvent.AddListener (OnStartServer);
            NetworkManagerModuleManager.Instance.onStopServerEvent.AddListener (OnStopServer);
            NetworkManagerModuleManager.Instance.onServerConnectEvent.AddListener (OnServerConnect);
        }

        // hook into NetworkManager client setup process
        public void OnStartClient (NetworkClient client) {
            client.RegisterHandler ((short) ChatMessages.ChatMessageTypes.CLIENT_CHAT_MESSAGE, OnClientChatMessage);
        }
        public void OnStopClient () {
            //client.UnregisterHandler((short)ChatMessages.ChatMessageTypes.CLIENT_CHAT_MESSAGE);
        }

        // hook into NetManagers server setup process
        public void OnStartServer () {
            NetworkServer.RegisterHandler ((short) ChatMessages.ChatMessageTypes.SERVER_CHAT_MESSAGE, OnServerChatMessage);
        }
        public void OnStopServer () {
            NetworkServer.UnregisterHandler ((short) ChatMessages.ChatMessageTypes.SERVER_CHAT_MESSAGE);
        }

        /// <summary>
        /// Call this to send a message from a client
        /// sends messges to server
        /// </summary>
        /// <param name="currentMessage"></param>
        public void SendChatMessage (string currentMessage) {
            if (!string.IsNullOrEmpty (currentMessage)) {
                ChatMessages.ClientChatMessage msg = new ChatMessages.ClientChatMessage ();
                msg.message = currentMessage;
                UnityEngine.Networking.NetworkManager.singleton.client.Send ((short)ChatMessages.ChatMessageTypes.SERVER_CHAT_MESSAGE, msg);
                //currentMessage = String.Empty;
            }
        }

        /// <summary>
        /// Called when a client connects, sends history of messages to client
        /// </summary>
        /// <param name="conn"></param>
        public void OnServerConnect (NetworkConnection conn) {
            // send history of message to client
            ChatMessages.ServerChatMessage chat = new ChatMessages.ServerChatMessage ();
            NetworkServer.SendToClient (conn.connectionId, (short) ChatMessages.ChatMessageTypes.CLIENT_CHAT_MESSAGE, chat);
            if ((chatLog != null) && (chatLog.chatHistory != null)) {
                for (int i = 0; i < chatLog.chatHistory.Count; i++) {
                    chat.message = chatLog.chatHistory[i];
                    chat.sender = "history";
                    NetworkServer.SendToClient (conn.connectionId, (short) ChatMessages.ChatMessageTypes.CLIENT_CHAT_MESSAGE, chat);
                }
            }
        }

        /// <summary>
        /// Called when server receives message from client
        /// called on ChatMessages.ChatMessageTypes.SERVER_CHAT_MESSAGE
        /// </summary>
        /// <param name="netMsg"></param>
        private void OnServerChatMessage (NetworkMessage netMsg) {
            var msg = netMsg.ReadMessage<ChatMessages.ClientChatMessage> ();

            string sender = netMsg.conn.address;
            //Debug.Log("NetworkChat.OnServerChatMessage: " + Network.isClient + " " + Network.isServer);
            if (sender == "localClient") {
                sender = "server (" + NetworkDiscovery.GetAddress (new List<System.Net.Sockets.AddressFamily> () { System.Net.Sockets.AddressFamily.InterNetworkV6 }) + ")";
            } else { }
            //Debug.Log("NetworkChat.OnServerChatMessage: server received: " + sender + " : " + msg.message);

            ChatMessages.ServerChatMessage chat = new ChatMessages.ServerChatMessage ();
            chat.message = msg.message;
            chat.sender = sender;

            // send message to all clients
            NetworkServer.SendToAll ((short) ChatMessages.ChatMessageTypes.CLIENT_CHAT_MESSAGE, chat);
        }

        /// <summary>
        /// called when client receives message from server
        /// called on ChatMessages.ChatMessageTypes.CLIENT_CHAT_MESSAGE
        /// </summary>
        /// <param name="netMsg"></param>
        private void OnClientChatMessage (NetworkMessage netMsg) {
            var msg = netMsg.ReadMessage<ChatMessages.ServerChatMessage> ();
            //Debug.Log("NetworkChat.OnClientChatMessage: client received: " + msg.sender + " : " + msg.message);

            chatLog.Add (msg.message, msg.sender);
        }
    }
}
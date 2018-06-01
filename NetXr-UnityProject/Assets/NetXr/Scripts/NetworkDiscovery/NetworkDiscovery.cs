//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace NetXr {
    #if UnityEditor
    using UnityEditor;
    [CustomEditor (typeof (NetworkDiscovery))]
    //[CustomPropertyDrawer(typeof(CustomNetworkDiscovery))]
    public class CustomNetworkDiscoveryEditorView : Editor {
        public override void OnInspectorGUI () {
            //GUILayout.Label("This is a Label in a Custom Editor");
            base.OnInspectorGUI ();
        }
    }
    #endif


    [System.Serializable]
    public class HostEntry {
        public string ip;
        public int processId;
        public AutoNetworkDiscoveryController.NETWORK_STATE networkState;
        public int serverPriority;
        public float lastSeenTime;
    }

    public class NetworkDiscovery : UnityEngine.Networking.NetworkDiscovery {

        // 5 seconds until we remove remote hosts
        public float remoteHostTimeout = 5;

        // list of known remote hosts
        private List<HostEntry> remoteHosts = new List<HostEntry> ();

        //public float hostDiscoveredTime = 0.0f;
        public float hostsMutedTime = 0.0f;

        public void OnEnable () {
            broadcastPort = AutoNetworkDiscoveryController.Instance.broadcastPort;
        }

        public virtual void Start () {
            broadcastPort = AutoNetworkDiscoveryController.Instance.broadcastPort;
            broadcastKey = 12341234;
            broadcastVersion = 1;
            broadcastSubVersion = 1;
            broadcastInterval = 1000;

            useNetworkManager = false;

            broadcastData = "undefined";

            showGUI = false;

            isClient = false;
            isServer = false;

            Initialize ();
        }

        // combine data
        public string BroadcastData () {
            return ":" + LocalHostIp () + ":" + LocalProcessId () + ":" + LocalNetworkState () + ":" + LocalServerPriority () + ":";
        }
        // handle data
        public void ParseReceivedData (string fromAddress, string receivedData) {
            string cleanData = Regex.Replace (receivedData, @"[^\u0020-\u007E]", string.Empty);
            string[] dataSplit = cleanData.Split (':');

            string remoteHostIp = dataSplit[1];
            int remoteProcessIdInt = -1;
            int.TryParse (dataSplit[2], out remoteProcessIdInt);

            //string remoteServerState = dataSplit[3];
            int remoteNetworkStateInt = -1;
            int.TryParse (dataSplit[3], out remoteNetworkStateInt);
            AutoNetworkDiscoveryController.NETWORK_STATE remoteNetworkState = (AutoNetworkDiscoveryController.NETWORK_STATE) remoteNetworkStateInt;

            //string remoteServerPriority = dataSplit[4];
            int remoteServerPriorityInt = -1;
            int.TryParse (dataSplit[4], out remoteServerPriorityInt);

            // remote is not really remote, it's the same process and computer
            if (string.Equals (remoteHostIp, LocalHostIp ()) &&
                (remoteProcessIdInt == LocalProcessId ())) {
                //UnityEngine.Debug.Log("ParseReceivedData: ignore same process and ip '" + remoteHostIp + "' '" + LocalHostIp() + "' : '" + remoteProcessIdInt + "' '" + LocalProcessId() + "'");
                return;
            } else {
                // other host, continue
                //UnityEngine.Debug.Log("ParseReceivedData: other process and ip '" + remoteHostIp + "' '" + LocalHostIp() + "' : '" + remoteProcessIdInt + "' '" + LocalProcessId() +"'");
            }

            int hostId = -1;
            for (int i = 0; i < remoteHosts.Count; i++) {
                HostEntry remoteHost = remoteHosts[i];
                if ((remoteHostIp == remoteHost.ip) && (remoteProcessIdInt == remoteHost.processId)) {
                    hostId = i;
                    break;
                }
            }

            // host is new, add it to list, reset timer
            if (hostId == -1) {
                UnityEngine.Debug.Log ("ParseReceivedData: detected new host " + remoteHostIp + " " + remoteProcessIdInt + " " + remoteNetworkState + " " + remoteServerPriorityInt);
                HostEntry remoteHost = new HostEntry () {
                    ip = remoteHostIp,
                    processId = remoteProcessIdInt,
                    networkState = remoteNetworkState,
                    serverPriority = remoteServerPriorityInt,
                    lastSeenTime = Time.time
                };
                remoteHosts.Add (remoteHost);
                //hostDiscoveredTime = Time.time;
                hostsMutedTime = Time.time;
                return;
            }

            // host is known
            else {
                // are the parameters the same, that should stay the same?
                if (
                    (string.Compare (remoteHosts[hostId].ip, remoteHostIp) == 0) &&
                    (remoteHosts[hostId].processId == remoteProcessIdInt) &&
                    (remoteHosts[hostId].serverPriority == remoteServerPriorityInt)
                ) {
                    // good
                } else {
                    // bad
                    UnityEngine.Debug.LogError ("CustomNetworkDiscovery.ParseReceivedData: data changed that should actually be static!");
                    UnityEngine.Debug.Log (" - " + remoteHosts[hostId].ip + " " + remoteHostIp);
                    UnityEngine.Debug.Log (" - " + remoteHosts[hostId].processId + " " + remoteProcessIdInt);
                    UnityEngine.Debug.Log (" - " + remoteHosts[hostId].serverPriority + " " + remoteServerPriorityInt);
                }

                if (remoteHosts[hostId].networkState == remoteNetworkState) {
                    //UnityEngine.Debug.Log("ParseReceivedData: update host with same network state : " + remoteHostIp + " " + remoteProcessId + " " + remoteNetworkState + " " + remoteServerPriorityInt);
                    // do nothing, nothing changed
                }
                // value changed (server state)
                else {
                    UnityEngine.Debug.Log ("CustomNetworkDiscovery.ParseReceivedData: remote host changed " + remoteHostIp + ":" + remoteProcessIdInt + " changed from " + remoteHosts[hostId].networkState + " to " + remoteNetworkState);
                    remoteHosts[hostId].networkState = remoteNetworkState;

                    if (((remoteHosts[hostId].networkState == AutoNetworkDiscoveryController.NETWORK_STATE.MAYBE_HOST) && (remoteNetworkState == AutoNetworkDiscoveryController.NETWORK_STATE.HOST)) ||
                        ((remoteHosts[hostId].networkState == AutoNetworkDiscoveryController.NETWORK_STATE.MAYBE_CLIENT) && (remoteNetworkState == AutoNetworkDiscoveryController.NETWORK_STATE.CLIENT))) {
                        // dont reset timer if changed from hypotetical state to real state
                    } else {
                        // reset timer
                        hostsMutedTime = Time.time;
                    }
                }

                remoteHosts[hostId].lastSeenTime = Time.time;
            }
        }

        public List<HostEntry> GetRemoteHosts () {
            List<HostEntry> updatedRemoteHosts = new List<HostEntry> ();
            for (int i = 0; i < remoteHosts.Count; i++) {
                if ((Time.time - remoteHosts[i].lastSeenTime) > remoteHostTimeout) {
                    // skip this host, it has timed out
                } else {
                    // add to list
                    updatedRemoteHosts.Add (remoteHosts[i]);
                }
            }
            remoteHosts = updatedRemoteHosts;
            return updatedRemoteHosts;
        }

        // generate data
        public string LocalHostIp () {
            return Regex.Replace (Network.player.ipAddress, @"[^\u0020-\u007E]", string.Empty);
        }

        public int LocalNetworkState () {
            return (int) AutoNetworkDiscoveryController.Instance.networkState;
        }

        public int LocalProcessId () {
            int processId = Process.GetCurrentProcess ().Id; //.ToString();
            return processId;
        }
        private int LocalUniqueId () {
            string uniqueIdString = SystemInfo.deviceUniqueIdentifier.Substring (0, 8);
            //UnityEngine.Debug.Log("LocalUniqueId: uniqueId " + uniqueIdString);
            int uniqueId = int.Parse (uniqueIdString, System.Globalization.NumberStyles.HexNumber);
            return (int) (uniqueId % (int.MaxValue / 2));
        }
        public int LocalServerPriority () {
            // use local unique identifier
            return LocalProcessId () + LocalUniqueId ();
        }

        public static string GetAddress (List<System.Net.Sockets.AddressFamily> excludeTypes) {
            string hostName = Dns.GetHostName ();
            IPAddress[] ipAddresses = Dns.GetHostEntry (hostName).AddressList;
            string addressString = "";
            foreach (IPAddress ip in ipAddresses) {
                if (!excludeTypes.Contains (ip.AddressFamily)) {
                    addressString += "'" + ip + "', ";
                }
            }
            UnityEngine.Debug.Log ("CustomNetworkDiscovery.Start: " + addressString);
            return addressString;
        }

    }
}
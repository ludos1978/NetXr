//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using UnityEngine;
using UnityEngine.Networking;
using NetXr;
using System.Collections.Generic;

namespace NetXr {
    public class NetworkPlayerController : NetworkBehaviour {
        #region Singleton
        private static NetworkPlayerController _localInstance;
        public static NetworkPlayerController LocalInstance {
            get {
                if (_localInstance == null) {
                    Debug.LogError("NetworkPlayerController.LocalInstance: not yet initialized, awake must have run");
                    return null;
                }
                return _localInstance;
            }
            set {
                _localInstance = value;
            }
        }

        public static NetworkPlayerController GetLocalNetworkPlayer() {
            for (int i = 0; i < ClientScene.localPlayers.Count; i++) {
                NetworkPlayerController netPlCt = ClientScene.localPlayers[i].gameObject.GetComponent<NetworkPlayerController>();
                if (netPlCt != null) {
                    return netPlCt;
                }
            }
            return null;
        }
        #endregion

        protected GameObject PlayerRootGameObject;

        public List<NetworkInputDevice> addedInputDevices = new List<NetworkInputDevice>();

        public GameObject ViveControllerPrefab;
        public GameObject MouseControllerPrefab;
        public GameObject LeapControllerPrefab;

        void Awake() {
            //Debug.LogError("NetworkPlayerController.Awake");
            if (isLocalPlayer) { // do not use it here, not initialized!
            }

            PlayerRootGameObject = gameObject;
        }

        //void Start () {
        //Debug.LogError("NetworkPlayerController.Start");
        //}

        // This happens after OnStartClient(), as it is triggered by an ownership message from the server.
        // This is an appropriate place to activate components or functionality that should only be active for the local player, such as cameras and input.
        public override void OnStartLocalPlayer() {
            //Debug.Log("NetworkPlayerController.OnStartLocalPlayer " + enabled);
            base.OnStartLocalPlayer();

            if (isLocalPlayer) {
                _localInstance = this;
            }

            if (PlayerSettingsController.Instance.GetHeadTransform() == null) {
                Debug.LogError("NetworkPlayerController.OnStartLocalPlayer: headTransform not found!");
            }

            PlayerSettingsController.Instance.cameraInstance.transform.position = transform.position;
            // reparent, keep 0 pos and 0 rot
            transform.SetParent(PlayerSettingsController.Instance.GetHeadTransform(), false);
            Debug.Log("NetworkPlayerController.OnStartLocalPlayer: reparenting to " + PlayerSettingsController.Instance.GetHeadTransform() + " " + isLocalPlayer);

            //FindObjectOfType<OVRInputModule>().rayTransform = transform;

            if (isLocalPlayer) {
                Renderer myRenderer = GetComponentInChildren<Renderer>();
                if (myRenderer != null) {
                    myRenderer.material.color = Color.blue;
                }

                if (PlayerSettingsController.Instance.mouseControllerEnabled) {
                    CmdCreateMouseNetworkControllers(netId, MouseController.Instance.inputDevice.deviceId);
                }
                /*if (PlayerSettingsController.Instance.viveControllerEnabled) {
                    //CmdCreateViveNetworkControllers (netId);
                }*/
                /*if (PlayerSettingsController.Instance.leapControllerEnabled) {
                    CmdCreateLeapNetworkControllers (netId);
                }*/
            } else {
                Debug.LogWarning("NetworkPlayerController.OnStartLocalPlayer: this should not occur!, othervise i dont understand what functions get called on client/server");
                PlayerRootGameObject.name = "remotePlayerInstance";
            }

            if (isLocalPlayer) {
                // required to create callback for other listeners
                NetworkManagerModuleManager.Instance.OnStartLocalPlayerCompleted(this);
            }

            UpdatePlayerName();
        }

        //public string bodyTargetName = "body-head/body";
        ////public string leftHandTargetName = "body-head/body/l-hand-target";
        ////public string rightHandTargetName = "body-head/body/r-hand-target";
        //public Vector3 defaultUpRotation = new Vector3(-90, 0, 0);

        public void Update() {
            //if (isLocalPlayer) {
            //    transform.localPosition = Vector3.zero;
            //    transform.localRotation = Quaternion.identity;
            //}

            //// keep body vertical
            //Quaternion rot = Quaternion.Euler(defaultUpRotation + new Vector3(0, 0, transform.rotation.eulerAngles.y));
            ////Debug.Log("apply rotation " + rot.eulerAngles + " " + transform.rotation.eulerAngles);
            //transform.Find(bodyTargetName).transform.rotation = rot;

            // 
            //foreach (NetworkInputDevice netInputDev in addedInputDevices) {
            //    if (netInputDev != null) {
            //        if (netInputDev.deviceHand == InputDeviceHand.Left) {
            //            Transform lHand = transform.Find(leftHandTargetName);
            //            lHand.position = netInputDev.transform.position; // + netInputDev.transform.forward; // * -0.1f;
            //            lHand.rotation = netInputDev.transform.rotation;
            //        }
            //        if (netInputDev.deviceHand == InputDeviceHand.Right) {
            //            Transform rHand = transform.Find(rightHandTargetName);
            //            rHand.position = netInputDev.transform.position; // + netInputDev.transform.forward; // * -0.1f;
            //            rHand.rotation = netInputDev.transform.rotation;
            //        }

            //        if (netInputDev.deviceHand == InputDeviceHand.Undefined) {
            //            Transform rHand = transform.Find(rightHandTargetName);
            //            rHand.position = netInputDev.transform.position + netInputDev.transform.forward * 0.4f + netInputDev.transform.up * -0.2f + netInputDev.transform.right * 0.2f;
            //            //rHand.rotation = Quaternion.identity; // Quaternion.LookRotation(transform.forward); // netInputDev.transform.rotation;
            //            rHand.localRotation = Quaternion.identity;
            //        }
            //    }
            //}
        }

        #region player name sync
        public override void OnStartClient() {
            base.OnStartClient();
            ApplyPlayerName();
        }

        [SyncVar(hook = "PlayerName")]
        string playerName = "NotInitialized";
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        [Command]
        public void CmdPlayerName(string value) {
            playerName = value;
            ApplyPlayerName();
        }
        /// <summary>
        /// callback der auf allen client ausgeführt wird, wenn der wert verändert wird
        /// </summary>
        [Client]
        public void PlayerName(string value) {
            playerName = value;
            // führe visuelle anpassungen aus (SeaboardDataHandler)
            ApplyPlayerName();
        }
        /// <summary>
        /// 
        /// </summary>
        public void UpdatePlayerName() {
            if (NetworkPlayerController.LocalInstance == this) {
                // TODO: Reto set player name, nothing else setup
                string title = "";
                //switch (GiCPlaybackManager.Instance.ThisPlayerInterface) {
                //    case GiC_Player.GJ:
                //        title = "GJ";
                //        break;
                //    case GiC_Player.Leap:
                //        title = "Trees";
                //        break;
                //    case GiC_Player.Paint:
                //        title = "Paint";
                //        break;
                //    case GiC_Player.Seaboard:
                //        title = "Keys";
                //        break;
                //    case GiC_Player.Undefined:
                //        title = "Wrong";
                //        break;
                //    default:
                //        title = "Undefined";
                //        break;
                //}
                //Debug.Log("NetworkPlayerController.UpdatePlayerName: " + title);
                CmdPlayerName(title);
            }
        }
        /// <summary>
        /// apply the value from syncvar
        /// </summary>
        public void ApplyPlayerName() {
            Transform t = transform.Find("NameLabel");
            if (t != null) {
                TextMesh tm = t.GetComponent<TextMesh>();
                if (tm != null) {
                    tm.text = playerName;
                    //Debug.Log("NetworkPlayerController.ApplyPlayerName: name applied " + playerName);
                }
            }

            Color c = Color.white;
            switch (playerName) {
                case "GJ":
                    c = Color.gray;
                    break;
                case "Trees":
                    c = Color.yellow;
                    break;
                case "Paint":
                    c = Color.green;
                    break;
                case "Keys":
                    c = Color.magenta;
                    break;
                default:
                    break;
            }

            Transform r = transform.Find("GodRay");
            if (r != null) {
                LineRenderer lr = r.GetComponent<LineRenderer>();
                if (lr != null) {
                    lr.startColor = c;
                    lr.endColor = c;
                    //Debug.Log("NetworkPlayerController.ApplyPlayerName: color applied " + c);
                }
            }

            //Debug.Log("NetworkPlayerController.ApplyPlayerName: label not found to apply name " + playerName);
        }
        #endregion

        #region Create Network Controllers (Mouse, Vive, Leap)
        [Command]
        void CmdCreateMouseNetworkControllers(NetworkInstanceId playerId, int inputDeviceId) {
            GameObject mouseControllerInstance = Instantiate(MouseControllerPrefab);
            NetworkMouseHand mouseHand = mouseControllerInstance.GetComponent<NetworkMouseHand>();

            mouseHand.ownerId = playerId;
            mouseHand.inputDeviceId = inputDeviceId;

            mouseControllerInstance.SetActive(true);

            NetworkServer.SpawnWithClientAuthority(mouseControllerInstance, connectionToClient);
        }

        [Client]
        public void ClientCreateViveNetworkController(int inputDeviceId) {
            CmdCreateViveNetworkController(netId, inputDeviceId);
        }
        [Command]
        void CmdCreateViveNetworkController(NetworkInstanceId playerId, int inputDeviceId) {
            GameObject viveInstance = Instantiate(ViveControllerPrefab);
            NetworkViveHand viveHand = viveInstance.GetComponent<NetworkViveHand>();
            viveHand.ownerId = playerId;
            viveHand.inputDeviceId = inputDeviceId;
            viveHand.gameObject.SetActive(true);
            NetworkServer.SpawnWithClientAuthority(viveInstance, connectionToClient);
        }

        //[Command]
        //void CmdCreateViveNetworkControllers (NetworkInstanceId playerId) {
        //    GameObject leftHandInstance = Instantiate (viveControllerPrefab);
        //    GameObject rightHandInstance = Instantiate (viveControllerPrefab);

        //    NetworkViveHand leftVRHand = leftHandInstance.GetComponent<NetworkViveHand> ();
        //    NetworkViveHand rightVRHand = rightHandInstance.GetComponent<NetworkViveHand> ();

        //    //leftVRHand.side = InputDeviceHand.Left;
        //    //rightVRHand.side = InputDeviceHand.Right;
        //    leftVRHand.ownerId = playerId;
        //    rightVRHand.ownerId = playerId;

        //    leftVRHand.gameObject.SetActive (true);
        //    rightVRHand.gameObject.SetActive (true);

        //    NetworkServer.SpawnWithClientAuthority (leftHandInstance, base.connectionToClient);
        //    NetworkServer.SpawnWithClientAuthority (rightHandInstance, base.connectionToClient);
        //}

        [Client]
        public void ClientCreateLeapNetworkController(int inputDeviceId) {
            CmdCreateLeapNetworkController(netId, inputDeviceId);
        }
        [Command]
        void CmdCreateLeapNetworkController(NetworkInstanceId playerId, int inputDeviceId) {
            GameObject leapInstance = Instantiate(LeapControllerPrefab);
            NetworkLeapHand leapVrHand = leapInstance.GetComponent<NetworkLeapHand>();
            leapVrHand.ownerId = playerId;
            leapVrHand.inputDeviceId = inputDeviceId;
            leapVrHand.gameObject.SetActive(true);
            NetworkServer.SpawnWithClientAuthority(leapInstance, connectionToClient);
        }

        //[Command]
        //void CmdCreateLeapNetworkControllers (NetworkInstanceId playerId) {
        //    GameObject leftHandInstance = Instantiate (leapControllerPrefab);
        //    GameObject rightHandInstance = Instantiate (leapControllerPrefab);

        //    NetworkLeapHand leftVRHand = leftHandInstance.GetComponent<NetworkLeapHand> ();
        //    NetworkLeapHand rightVRHand = rightHandInstance.GetComponent<NetworkLeapHand> ();

        //    leftVRHand.ownerId = playerId;
        //    rightVRHand.ownerId = playerId;
        //    leftVRHand.side = LeapHandSide.Left;
        //    rightVRHand.side = LeapHandSide.Right;

        //    leftVRHand.gameObject.SetActive (true);
        //    rightVRHand.gameObject.SetActive (true);

        //    NetworkServer.SpawnWithClientAuthority (leftHandInstance, base.connectionToClient);
        //    NetworkServer.SpawnWithClientAuthority (rightHandInstance, base.connectionToClient);
        //}
        #endregion

        #region Handle Object Creation
        [Command]
        public void CmdCreateNetworkObject(Vector3 createPosition, Quaternion createRotation, string networkObjectPrefabName) {
            GameObject go = CreateNetworkObject(createPosition, createRotation, networkObjectPrefabName);
        }
        [Command]
        public void CmdCreateNetworkObjectAuthForce(Vector3 createPosition, Quaternion createRotation, string networkObjectPrefabName, bool giveAuthority, Vector3 addForce, ForceMode forceMode) {
            GameObject go = CreateNetworkObject(createPosition, createRotation, networkObjectPrefabName);
            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb != null) {
                rb.AddForce(addForce, forceMode);
            }

            NetworkInstanceId netId = go.GetComponent<NetworkIdentity>().netId;
            if (giveAuthority && (netId != null)) {
                CmdGetAuthority(netId);
            }
        }

        [Server]
        public GameObject CreateNetworkObject(Vector3 createPosition, Quaternion createRotation, string networkObjectPrefabName) {
            GameObject prefab = null;
            for (int i = 0; i < NetworkManagerModuleManager.Instance.spawnPrefabs.Count; i++) {
                if (NetworkManagerModuleManager.Instance.spawnPrefabs[i].name == networkObjectPrefabName) {
                    prefab = NetworkManagerModuleManager.Instance.spawnPrefabs[i];
                }
            }

            GameObject networkObjectInstance = null;
            if (prefab != null) {
                networkObjectInstance = Instantiate(prefab, createPosition, createRotation);
                networkObjectInstance.SetActive(true);
                NetworkServer.Spawn(networkObjectInstance);
                return networkObjectInstance;
                // return networkObjectInstance.GetComponent<NetworkIdentity>().netId;
            }
            return null;
            // return is not possible
            // return networkObjectInstance;
        }

        /// <summary>
        /// Create an Atom
        /// </summary>
        /// <param name="createPosition">The Position where to create the Atom</param>
        /// <param name="createRotation">The Rotation to Initialize the object with</param>
        //[Command]
        //public void CmdCreateAtom(Vector3 createPosition, Quaternion createRotation) {
        //    Debug.Log("NetworkPlayerController.CmdCreateAtom: " + soundPresetId);

        //    GameObject atomInstance = Instantiate(AtomPrefab, createPosition, createRotation);
        //    NetworkInformationCore atomNetCore = atomInstance.GetComponent<NetworkInformationCore>();

        // Open Edit Room on local atom
        //    CmdGetAuthority(atomNetCore.netId);
        //}

        /// <summary>
        /// Get Authority over an Atom
        /// </summary>
        /// <param name="objectId">Object identifier.</param>
        /// http://answers.unity3d.com/questions/1245341/unet-how-do-i-properly-handle-client-authority-wit.html ???
        [Command]
        public void CmdGetAuthority(NetworkInstanceId objectId) {
            GameObject iObject = NetworkServer.FindLocalObject(objectId);
            NetworkIdentity objectNetworkIdentity = iObject.GetComponent<NetworkIdentity>();
            //GameObject controllerObject = NetworkServer.FindLocalObject (controllerId);
            //NetworkIdentity playerNetworkIdentity = iObject.GetComponent<NetworkIdentity> ();
            //NetworkIdentity playerNetworkIdentity = connectionToClient;
            //objectNetworkIdentity.localPlayerAuthority = true;

            AssignAuthority(objectNetworkIdentity, connectionToClient);

            /*if (objectNetworkIdentity.localPlayerAuthority) {
                objectNetworkIdentity.AssignClientAuthority (connectionToClient);
                Debug.Log ("NetworkPlayerController.CmdGetAuthority: set client authority to " + connectionToClient);
            } else {
                Debug.LogWarning ("NetworkPlayerController.CmdGetAuthority: no localPlayerAuthority (server " + Network.isServer + " client " + Network.isClient + ")");
            }*/
        }

        [Server]
        private void AssignAuthority(NetworkIdentity objectNetworkIdentity, NetworkConnection theConnectionToClient) {
            NetworkConnection currentOwner = objectNetworkIdentity.clientAuthorityOwner;
            if (currentOwner == theConnectionToClient) { } else {
                if (currentOwner != null) {
                    Debug.Log("NetworkPlayerController.AssignAuthority: remove client authority from " + currentOwner);
                    objectNetworkIdentity.RemoveClientAuthority(currentOwner);
                }
                //Debug.Log("NetworkPlayerController.AssignAuthority: set client authority to " + _connectionToClient);
                objectNetworkIdentity.AssignClientAuthority(theConnectionToClient);
            }
        }

        /// <summary>
        /// Remove Authority over and Atom
        /// </summary>
        /// <param name="objectId">Object identifier.</param>
        [Command]
        public void CmdDropAuthority(NetworkInstanceId objectId) {
            //Debug.Log("NetworkPlayerController.CmdDropAuthority: remove client authority");
            GameObject iObject = NetworkServer.FindLocalObject(objectId);
            NetworkIdentity objectNetworkIdentity = iObject.GetComponent<NetworkIdentity>();
            //objectNetworkIdentity.localPlayerAuthority = false;
            //if (objectNetworkIdentity.localPlayerAuthority) {
            NetworkConnection currentOwner = objectNetworkIdentity.clientAuthorityOwner;
            if (currentOwner == connectionToClient) {
                objectNetworkIdentity.RemoveClientAuthority(connectionToClient);
            } else {
                string owner = (currentOwner != null) ? ("(" + currentOwner.address + " " + currentOwner.hostId + " " + currentOwner.connectionId + ")") : "null";
                Debug.LogWarning("NetworkPlayerController.CmdDropAuthority: tryed to remove authority from foreign object (currentOwner " + owner + " connectionToClient " + connectionToClient + ")");
            }

            /*    Debug.Log ("NetworkPlayerController.CmdGetAuthority: remove client authority from " + connectionToClient);
            } else {
                Debug.LogWarning ("NetworkPlayerController.CmdGrab: no localPlayerAuthority (server " + Network.isServer + " client " + Network.isClient + ")");
            }*/
        }

        [Command]
        public void CmdToggleAuthority(NetworkInstanceId objectId) {
            GameObject iObject = NetworkServer.FindLocalObject(objectId);
            NetworkIdentity objectNetworkIdentity = iObject.GetComponent<NetworkIdentity>();
            NetworkConnection currentOwner = objectNetworkIdentity.clientAuthorityOwner;
            if (currentOwner == connectionToClient) {
                objectNetworkIdentity.RemoveClientAuthority(connectionToClient);
            } else {
                AssignAuthority(objectNetworkIdentity, connectionToClient);
            }
        }

        //[Command]
        //public void CmdGrab (NetworkInstanceId objectId, NetworkInstanceId controllerId) {
        //    GameObject iObject = NetworkServer.FindLocalObject (objectId);
        //    NetworkIdentity objectNetworkIdentity = iObject.GetComponent<NetworkIdentity> ();
        //    //networkIdentity.localPlayerAuthority = true;
        //    //if (networkIdentity.localPlayerAuthority) {
        //    //objectNetworkIdentity.AssignClientAuthority (connectionToClient);

        //    //} else {
        //    //    Debug.LogWarning ("NetworkPlayerController.CmdGrab: no localPlayerAuthority (server " + Network.isServer + " client " + Network.isClient + ")");
        //    //}

        //    AssignAuthority (objectNetworkIdentity, connectionToClient);

        //    InteractableObject interactableObject = iObject.GetComponent<InteractableObject> ();
        //    if (isServer) //(!isClient)
        //        interactableObject.RpcAttachCall (controllerId); // client-side

        //    //NetworkInputDevice inputDevice = NetworkServer.FindLocalObject (controllerId).GetComponent<NetworkInputDevice> (); //.GetGrabPoint();
        //    interactableObject.AttachCall(controllerId); // inputDevice); // server-side
        //}

        //[Command] // called from the client, run on the server
        //public void CmdDrop (NetworkInstanceId objectId, Vector3 currentHolderVelocity) {
        //    GameObject iObject = NetworkServer.FindLocalObject (objectId);
        //    NetworkIdentity objectNetworkIdentity = iObject.GetComponent<NetworkIdentity> ();
        //    //if (objectNetworkIdentity.localPlayerAuthority) {
        //    //objectNetworkIdentity.RemoveClientAuthority (connectionToClient);
        //    //}

        //    // get object we drop
        //    InteractableObject interactableObject = iObject.GetComponent<InteractableObject> ();
        //    if (isServer) //(!isClient)
        //        interactableObject.RpcDetach (currentHolderVelocity); // client-side
        //    interactableObject.Detach (currentHolderVelocity); // server-side
        //}

        //[Command]
        //public void CmdUse (NetworkInstanceId objectId) {
        //    Debug.Log ("NetworkPlayerController.CmdUse: " + objectId);
        //    GameObject iObject = NetworkServer.FindLocalObject (objectId);
        ////        NetworkIdentity networkIdentity = iObject.GetComponent<NetworkIdentity>();

        //    InteractableObject interactableObject = iObject.GetComponent<InteractableObject> ();
        //    if (isServer) //(!isClient)
        //        interactableObject.RpcUse (); // client-side
        //    interactableObject.Use (); // server-side
        //}

        //[Client]
        //public bool PlayerHasAuthority (NetworkIdentity objectNetworkIdentity) {
        //    NetworkConnection objectOwner = objectNetworkIdentity.clientAuthorityOwner;
        //    return (connectionToClient == objectOwner);
        //}

        #endregion

        public void AddedDevice(NetworkInputDevice addedDevice) {
            Debug.Log("NetworkPlayerController.AddedDevice: " + addedDevice);
            //NetworkInputDevice
            addedInputDevices.Add(addedDevice);
        }
    }
}
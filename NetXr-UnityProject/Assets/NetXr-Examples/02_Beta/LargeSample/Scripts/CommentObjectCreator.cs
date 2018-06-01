//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using UnityEngine;

namespace NetXr {
    public class CommentObjectCreator : MonoBehaviour {
        #region Singleton
        private static CommentObjectCreator instance = null;
        public static CommentObjectCreator Instance {
            get {
                if (instance == null) {
                    instance = ((CommentObjectCreator) FindObjectOfType (typeof (CommentObjectCreator)));
                }
                return instance;
            }
        }
        #endregion

        public GameObject commentObjectPrefab;

        // Update is called once per frame
        void Update () {
            if (Input.GetKeyDown (KeyCode.K)) {
                CreateCommentObject ();
            }
        }

        public void CreateCommentObject () {
            Vector3 pos = NetworkPlayerController.LocalInstance.transform.position;
            Vector3 forward = NetworkPlayerController.LocalInstance.transform.forward;
            Quaternion rot = NetworkPlayerController.LocalInstance.transform.rotation;
            NetworkPlayerController.LocalInstance.CmdCreateNetworkObject (pos + forward * 1.5f, rot, commentObjectPrefab.name);
        }
    }
}
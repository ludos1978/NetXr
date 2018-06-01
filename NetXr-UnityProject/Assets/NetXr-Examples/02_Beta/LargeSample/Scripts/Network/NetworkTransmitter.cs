//========================== Copyright (c) Unknown. ===========================
//
// Source: 
// https://bitbucket.org/stupro_hskl_betreuung_kessler/learnit_merged_ss16
//
//=============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace NetXr {
    /// <summary>
    /// Copyright by Someone else, it's from the Unity Forums.
    /// </summary>
    public class NetworkTransmitter : NetworkBehaviour {
        public const int RELIABLE_SEQUENCED_CHANNEL = 2;
        private static int defaultBufferSize = 1000; //max ethernet MTU is ~1400

        private class TransmissionData {
            public int curDataIndex; //current position in the array of data already received.
            public byte[] data;

            public TransmissionData (byte[] _data) {
                curDataIndex = 0;
                data = _data;
            }
        }

        //list of transmissions currently going on. a transmission id is used to uniquely identify to which transmission a received byte[] belongs to.
        List<int> serverTransmissionIds = new List<int> ();
        List<int> clientTransmissionIds = new List<int> ();

        //maps the transmission id to the data being received.
        Dictionary<int, TransmissionData> clientTransmissionData = new Dictionary<int, TransmissionData> ();
        Dictionary<int, TransmissionData> serverTransmissionData = new Dictionary<int, TransmissionData> ();

        //callbacks which are invoked on the respective events. int = transmissionId. byte[] = data sent or received.
        public event UnityAction<int, byte[]> OnDataFragmentSent;
        public event UnityAction<int, byte[]> OnDataFragmentCompletelySent;

        public event UnityAction<int, byte[]> OnDataFragmentReceivedServer;
        public event UnityAction<int, byte[]> OnDataFragmentCompletelyReceivedServer;

        public event UnityAction<int, byte[]> OnDataFragmentReceivedClient;
        public event UnityAction<int, byte[]> OnDataFragmentCompletelyReceivedClient;

        #region SERVERtoCLIENT
        [Server]
        public void SendBytesToClients (int transmissionId, byte[] data) {
            Debug.Assert (!serverTransmissionIds.Contains (transmissionId));
            StartCoroutine (SendBytesToClientsRoutine (transmissionId, data));
        }

        [Server]
        private IEnumerator SendBytesToClientsRoutine (int transmissionId, byte[] data) {
            Debug.Assert (!serverTransmissionIds.Contains (transmissionId));

            //Debug.Log("NetworkTransmitter.SendBytesToClientsRoutine: Send Data: " + data.Length);

            //tell client that he is going to receive some data and tell him how much it will be.
            RpcPrepareToReceiveBytes (transmissionId, data.Length);
            yield return null;

            //begin transmission of data. send chunks of 'bufferSize' until completely transmitted.
            serverTransmissionIds.Add (transmissionId);
            TransmissionData dataToTransmit = new TransmissionData (data);
            int bufferSize = defaultBufferSize;
            while (dataToTransmit.curDataIndex < dataToTransmit.data.Length - 1) {
                //determine the remaining amount of bytes, still need to be sent.
                int remaining = dataToTransmit.data.Length - dataToTransmit.curDataIndex;
                if (remaining < bufferSize) {
                    bufferSize = remaining;
                }

                //prepare the chunk of data which will be sent in this iteration
                byte[] buffer = new byte[bufferSize];
                System.Array.Copy (dataToTransmit.data, dataToTransmit.curDataIndex, buffer, 0, bufferSize);

                //send the chunk
                //Debug.Log("NetworkTransmitter.SendBytesToClientsRoutine: Send Chunk: " + transmissionId);
                RpcReceiveBytes (transmissionId, buffer);
                dataToTransmit.curDataIndex += bufferSize;

                //yield return null;

                if (null != OnDataFragmentSent) {
                    OnDataFragmentSent.Invoke (transmissionId, buffer);
                }
            }

            //transmission complete.
            serverTransmissionIds.Remove (transmissionId);

            if (null != OnDataFragmentCompletelySent) {
                OnDataFragmentCompletelySent.Invoke (transmissionId, dataToTransmit.data);
            }
        }

        [ClientRpc]
        private void RpcPrepareToReceiveBytes (int transmissionId, int expectedSize) {
            if (clientTransmissionData.ContainsKey (transmissionId)) {
                Debug.LogError ("NetworkTransmitter.RpcReceiveBytes: already receiving " + transmissionId + " aborting");
                return;
            }

            //Debug.Log("NetworkTransmitter.RpcPrepareToReceiveBytes: prepare to receive "+expectedSize+ " bytes");
            //prepare data array which will be filled chunk by chunk by the received data
            TransmissionData receivingData = new TransmissionData (new byte[expectedSize]);
            clientTransmissionData.Add (transmissionId, receivingData);
        }

        //use reliable sequenced channel to ensure bytes are sent in correct order
        [ClientRpc (channel = RELIABLE_SEQUENCED_CHANNEL)]
        private void RpcReceiveBytes (int transmissionId, byte[] recBuffer) {
            //already completely received or not prepared?
            if (!clientTransmissionData.ContainsKey (transmissionId)) {
                Debug.LogError ("NetworkTransmitter.RpcReceiveBytes: not receiving " + transmissionId + " aborting");
                return;
            }

            //Debug.Log("NetworkTransmitter.RpcReceiveBytes: Receive Chunk: " + transmissionId);

            //copy received data into prepared array and remember current dataposition
            TransmissionData dataToReceive = clientTransmissionData[transmissionId];
            //dataToReceive.data = new byte[recBuffer.Length];
            System.Array.Copy (recBuffer, 0, dataToReceive.data, dataToReceive.curDataIndex, recBuffer.Length);
            dataToReceive.curDataIndex += recBuffer.Length;

            if (null != OnDataFragmentReceivedClient)
                OnDataFragmentReceivedClient (transmissionId, recBuffer);

            if (dataToReceive.curDataIndex < dataToReceive.data.Length - 1) {
                //current data not completely received
                return;
            }

            //Debug.Log("NetworkTransmitter.RpcReceiveBytes: Completely Received: "+ recBuffer.Length + " " + dataToReceive.curDataIndex +" < "+ dataToReceive.data.Length);

            //current data completely received
            //Debug.Log(LOG_PREFIX + "Completely Received Data at transmissionId=" + transmissionId);
            clientTransmissionData.Remove (transmissionId);

            if (null != OnDataFragmentCompletelyReceivedClient)
                OnDataFragmentCompletelyReceivedClient.Invoke (transmissionId, dataToReceive.data);
        }

        [Client]
        public float GetClientProgressOfTransmissionProcess (int transmissionId) {
            if (!clientTransmissionData.ContainsKey (transmissionId))
                return 0;

            TransmissionData dataToReceive = clientTransmissionData[transmissionId];
            return (float) dataToReceive.curDataIndex / dataToReceive.data.Length;
        }

        [Client]
        public int GetClientNumberOfReceivedBytes (int transmissionId) {
            if (!clientTransmissionData.ContainsKey (transmissionId))
                return 0;

            TransmissionData dataToReceive = clientTransmissionData[transmissionId];
            return dataToReceive.curDataIndex;
        }
        #endregion

        #region CLIENTtoSERVER
        [Client]
        public void SendBytesToServer (int transmissionId, byte[] data) {
            Debug.Assert (!serverTransmissionIds.Contains (transmissionId));
            StartCoroutine (SendBytesToServerRoutine (transmissionId, data));
        }

        [Client]
        private IEnumerator SendBytesToServerRoutine (int transmissionId, byte[] data) {
            Debug.Assert (!clientTransmissionIds.Contains (transmissionId));
            //Debug.Log(LOG_PREFIX + "SendBytesToServerRoutine processId=" + transmissionId + " | datasize=" + data.Length);

            //tell client that he is going to receive some data and tell him how much it will be.
            CmdPrepareToReceiveBytes (transmissionId, data.Length);
            yield return null;

            //begin transmission of data. send chunks of 'bufferSize' until completely transmitted.
            clientTransmissionIds.Add (transmissionId);
            TransmissionData dataToTransmit = new TransmissionData (data);
            int bufferSize = defaultBufferSize;
            while (dataToTransmit.curDataIndex < dataToTransmit.data.Length - 1) {
                //determine the remaining amount of bytes, still need to be sent.
                int remaining = dataToTransmit.data.Length - dataToTransmit.curDataIndex;
                if (remaining < bufferSize)
                    bufferSize = remaining;

                //prepare the chunk of data which will be sent in this iteration
                byte[] buffer = new byte[bufferSize];
                System.Array.Copy (dataToTransmit.data, dataToTransmit.curDataIndex, buffer, 0, bufferSize);

                //send the chunk
                CmdReceiveBytes (transmissionId, buffer);
                dataToTransmit.curDataIndex += bufferSize;

                //yield return null;

                if (null != OnDataFragmentSent)
                    OnDataFragmentSent.Invoke (transmissionId, buffer);
            }

            //transmission complete.
            clientTransmissionIds.Remove (transmissionId);

            if (null != OnDataFragmentCompletelySent)
                OnDataFragmentCompletelySent.Invoke (transmissionId, dataToTransmit.data);
        }

        [Command]
        private void CmdPrepareToReceiveBytes (int transmissionId, int expectedSize) {
            if (serverTransmissionData.ContainsKey (transmissionId))
                return;

            //prepare data array which will be filled chunk by chunk by the received data
            TransmissionData receivingData = new TransmissionData (new byte[expectedSize]);
            serverTransmissionData.Add (transmissionId, receivingData);
        }

        //use reliable sequenced channel to ensure bytes are sent in correct order
        [Command (channel = RELIABLE_SEQUENCED_CHANNEL)]
        private void CmdReceiveBytes (int transmissionId, byte[] recBuffer) {
            //already completely received or not prepared?
            if (!serverTransmissionData.ContainsKey (transmissionId))
                return;

            //copy received data into prepared array and remember current dataposition
            TransmissionData dataToReceive = serverTransmissionData[transmissionId];
            System.Array.Copy (recBuffer, 0, dataToReceive.data, dataToReceive.curDataIndex, recBuffer.Length);
            dataToReceive.curDataIndex += recBuffer.Length;

            if (null != OnDataFragmentReceivedServer)
                OnDataFragmentReceivedServer (transmissionId, recBuffer);

            if (dataToReceive.curDataIndex < dataToReceive.data.Length - 1)
                //current data not completely received
                return;

            //current data completely received
            //Debug.Log(LOG_PREFIX + "Completely Received Data at transmissionId=" + transmissionId);
            serverTransmissionData.Remove (transmissionId);

            if (null != OnDataFragmentCompletelyReceivedServer)
                OnDataFragmentCompletelyReceivedServer.Invoke (transmissionId, dataToReceive.data);
        }

        [Server]
        public float GetServerProgressOfTransmissionProcess (int transmissionId) {
            if (!serverTransmissionData.ContainsKey (transmissionId))
                return 0;

            TransmissionData dataToReceive = serverTransmissionData[transmissionId];
            return (float) dataToReceive.curDataIndex / dataToReceive.data.Length;
        }

        [Server]
        public int GetServerNumberOfReceivedBytes (int transmissionId) {
            if (!serverTransmissionData.ContainsKey (transmissionId))
                return 0;

            TransmissionData dataToReceive = serverTransmissionData[transmissionId];
            return dataToReceive.curDataIndex;
        }
        #endregion

    }
}
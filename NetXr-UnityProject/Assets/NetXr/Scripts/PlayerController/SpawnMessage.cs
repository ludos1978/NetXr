//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using UnityEngine.Networking;

namespace NetXr {
    public class SpawnMessage : MessageBase {
        public string vrDeviceName;

        public override void Deserialize (NetworkReader reader) {
            vrDeviceName = reader.ReadString ();
        }

        public override void Serialize (NetworkWriter writer) {
             writer.Write (vrDeviceName);
        }
    }
}
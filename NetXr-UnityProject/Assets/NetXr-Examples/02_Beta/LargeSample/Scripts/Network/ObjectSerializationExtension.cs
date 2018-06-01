//========================== Copyright (c) Unknown. ===========================
//
// Source: 
// http://stackoverflow.com/questions/1446547/how-to-convert-an-object-to-a-byte-array-in-c-sharp
//
//=============================================================================

using System.Collections;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace NetXr {
    namespace Utils {
        //Extension class to provide serialize / deserialize methods to object.
        // src: http://stackoverflow.com/questions/1446547/how-to-convert-an-object-to-a-byte-array-in-c-sharp
        // changes from http://answers.unity3d.com/questions/895934/serialize-and-deserialize-class-to-byte-on-ios.html
        //NOTE: You need add [Serializable] attribute in your class to enable serialization
        public static class ObjectSerializationExtension {
            public static byte[] SerializeToByteArray<T> (T serializableObject) {
                if (serializableObject == null) {
                    return null;
                }
                T obj = serializableObject;

                using (MemoryStream stream = new MemoryStream ()) {
                    IFormatter formatter = new BinaryFormatter ();
                    formatter.Serialize (stream, obj);
                    return stream.ToArray ();
                }
            }

            public static T Deserialize<T> (byte[] byteArray) {
                if (byteArray == null) {
                    return default (T);
                }

                using (MemoryStream stream = new MemoryStream ()) {
                    IFormatter formatter = new BinaryFormatter ();
                    return (T) formatter.Deserialize (stream);
                }
            }
        }
    }
}
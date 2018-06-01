//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace NetXr {
    /// <summary>
    /// Not really fully implemented, remove & insert missing
    /// </summary>
    [System.Serializable]
    public class ObservedList<T> : List<T> {
        public enum ObserverListChange {
            add,
            remove,
            change,
            addRange,
            removeRange,
            clear,
            insert,
            insertRange,
            removeAll
        }
        //public event Action<int> Changed = delegate { };
        public event Action<ObserverListChange, int[]> Updated = delegate { };

        public new void Add (T item) {
            base.Add (item);
            Updated (ObserverListChange.add, new int[] { base.Count - 1 });
        }
        public new void Remove (T item) {
            //int index = base.IndexOf(item);
            //base.Remove(item);
            //Updated(ObserverListChange.remove, new int[] { index });
            Debug.LogError ("ObservedList.Remove: function not implemented");
        }
        public new void AddRange (IEnumerable<T> collection) {
            //int prevLength = base.Count;
            //base.AddRange(collection);
            //int afterLength = base.Count;

            //Updated(ObserverListChange.addRange, new int[] { -1 });
            Debug.LogError ("ObservedList.AddRange: function not implemented");
        }
        public new void RemoveRange (int index, int count) {
            //int changedCount = base.Count - index;
            //int[] indexes = new int[changedCount];
            //for (int i=0; i<count; i++) {
            //    indexes[i] = index + count;
            //}
            //base.RemoveRange(index, count);
            //Updated(ObserverListChange.removeRange, indexes);
            Debug.LogError ("ObservedList.RemoveRange: function not implemented");
        }
        public new void Clear () {
            base.Clear ();
            Updated (ObserverListChange.clear, new int[] {-1 });
        }
        public new void Insert (int index, T item) {
            //base.Insert(index, item);
            //Updated(ObserverListChange.Insert, new int[] { index });
            Debug.LogError ("ObservedList.Insert: function not implemented");
        }
        public new void InsertRange (int index, IEnumerable<T> collection) {
            //base.InsertRange(index, collection);
            //Updated(ObserverListChange.insertRange, new int[] { index });
            Debug.LogError ("ObservedList.InsertRange: function not implemented");
        }

        public new void RemoveAll (Predicate<T> match) {
            //base.RemoveAll(match);
            //Updated(ObserverListChange.removeAll, new int[] { -1 });
            Debug.LogError ("ObservedList.RemoveAll: function not implemented");
        }

        public new T this [int index] {
            get {
                return base[index];
            }
            set {
                base[index] = value;
                //Changed(index);
                Updated (ObserverListChange.change, new int[] { index });
            }
        }
    }
}
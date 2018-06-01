using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NetXr {
	#if UNITY_EDITOR
	[CustomEditor(typeof(ObjectEventHandler))]
	public class ObjectEventHandlerInspector : Editor {
		public override void OnInspectorGUI() {
			ObjectEventHandler myTarget = (ObjectEventHandler)target;

			foreach (ObjectLayerTagHandler layerTagHandler in myTarget.objectLayerTagHandlers) {
				layerTagHandler.OnGUI();
			}
			// int newCount = EditorGUILayout.IntField("Number of Tag / Layer Objects", myTarget.objectLayerTagHandlers.Count);
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("+")) {
				myTarget.objectLayerTagHandlers.Add ( new ObjectLayerTagHandler() );
			}
			if (GUILayout.Button("-")) {
				if (myTarget.objectLayerTagHandlers.Count > 0) {
					myTarget.objectLayerTagHandlers.RemoveAt( myTarget.objectLayerTagHandlers.Count-1 );
				}
			}
			GUILayout.EndHorizontal();

			            // Show default inspector property editor
            DrawDefaultInspector();
        }
	}
	#endif


	[System.Serializable]
	public class ObjectLayerTagHandler {
		public bool useTag = false;
		public string tag = "";
		public bool useLayer = false;
		public int layer = -1;
		public InteractableObject interactableObject;

		#if UNITY_EDITOR
		public void OnGUI () {
			GUILayout.BeginHorizontal();
			useTag = EditorGUILayout.Toggle(useTag, GUILayout.Width(20));
			if (useTag) {
				tag = EditorGUILayout.TagField("Select Tag:", tag);
			} else {
				GUILayout.Label("Use Tag");
			}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			useLayer = EditorGUILayout.Toggle(useLayer, GUILayout.Width(20));
			if (useLayer) {
				layer = EditorGUILayout.LayerField("Select Layer:", layer);
			} else {
				GUILayout.Label("Use Layer");
			}
			GUILayout.EndHorizontal();
			if (!(useLayer || useTag)) {
				GUILayout.Label("Applies to all objects! (apply layer or tag limitation)");
			}
			interactableObject = EditorGUILayout.ObjectField(interactableObject, typeof(InteractableObject), true) as InteractableObject;
		}
		#endif
	}

	public class ObjectEventHandler : MonoBehaviour {
        #region Singleton
        private static ObjectEventHandler instance = null;
        public static ObjectEventHandler Instance {
            get {
                if (instance == null) {
                    instance = (ObjectEventHandler) FindObjectOfType (typeof (ObjectEventHandler));
                }
                return instance;
            }
        }
        #endregion

		#region check type of object
		public List<ObjectLayerTagHandler> objectLayerTagHandlers = new List<ObjectLayerTagHandler>();
		public List<ObjectLayerTagHandler> GetMatchingObjectLayerTagHandlers (InteractableObject interactableObject) {
			List<ObjectLayerTagHandler> matches = new List<ObjectLayerTagHandler>();
			foreach (ObjectLayerTagHandler objectLayerTagHandler in objectLayerTagHandlers) {
				// if tag defined
				if (objectLayerTagHandler.useTag) {
					// abort loop if not equal
					if (objectLayerTagHandler.tag != interactableObject.tag) {
						continue;
					}
				}
				// if layer defined
				if (objectLayerTagHandler.useLayer) {
					if (objectLayerTagHandler.layer != interactableObject.gameObject.layer) {
						continue;
					}
				}
				matches.Add(objectLayerTagHandler);
			}
			return matches;
		}
		#endregion

		#region Object Handling based on Object Layer and/or Tag
		public void HandleLayerTag_UseStartEvent (InputDeviceData deviceData) {
			foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
				foreach (ObjectLayerTagHandler objectLayerTagHandler in GetMatchingObjectLayerTagHandlers(interactableObject)) {
					objectLayerTagHandler.interactableObject.__OnUseStart(deviceData);
				}
			}
		}
		public void HandleLayerTag_UseRepeatEvent (InputDeviceData deviceData) {
			foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
				foreach (ObjectLayerTagHandler objectLayerTagHandler in GetMatchingObjectLayerTagHandlers(interactableObject)) {
					objectLayerTagHandler.interactableObject.__OnUseRepeat(deviceData);
				}
			}
		}
		public void HandleLayerTag_UseStopEvent (InputDeviceData deviceData) {
			foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
				foreach (ObjectLayerTagHandler objectLayerTagHandler in GetMatchingObjectLayerTagHandlers(interactableObject)) {
					objectLayerTagHandler.interactableObject.__OnUseStop(deviceData);
				}
			}
		}

		public void HandleLayerTag_GrabStartEvent (InputDeviceData deviceData) {
			foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
				foreach (ObjectLayerTagHandler objectLayerTagHandler in GetMatchingObjectLayerTagHandlers(interactableObject)) {
					objectLayerTagHandler.interactableObject.__OnGrabStart(deviceData);
				}
			}
		}
		public void HandleLayerTag_GrabRepeatEvent (InputDeviceData deviceData) {
			foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
				foreach (ObjectLayerTagHandler objectLayerTagHandler in GetMatchingObjectLayerTagHandlers(interactableObject)) {
					objectLayerTagHandler.interactableObject.__OnGrabRepeat(deviceData);
				}
			}
		}
		public void HandleLayerTag_GrabStopEvent (InputDeviceData deviceData) {
			foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
				foreach (ObjectLayerTagHandler objectLayerTagHandler in GetMatchingObjectLayerTagHandlers(interactableObject)) {
					objectLayerTagHandler.interactableObject.__OnGrabStop(deviceData);
				}
			}
		}

		public void HandleLayerTag_TouchStartEvent (InputDeviceData deviceData) {
			foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
				foreach (ObjectLayerTagHandler objectLayerTagHandler in GetMatchingObjectLayerTagHandlers(interactableObject)) {
					objectLayerTagHandler.interactableObject.__OnTouchStart(deviceData);
				}
			}
		}
		public void HandleLayerTag_TouchRepeatEvent (InputDeviceData deviceData) {
			foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
				foreach (ObjectLayerTagHandler objectLayerTagHandler in GetMatchingObjectLayerTagHandlers(interactableObject)) {
					objectLayerTagHandler.interactableObject.__OnTouchRepeat(deviceData);
				}
			}
		}
		public void HandleLayerTag_TouchStopEvent (InputDeviceData deviceData) {
			foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
				foreach (ObjectLayerTagHandler objectLayerTagHandler in GetMatchingObjectLayerTagHandlers(interactableObject)) {
					objectLayerTagHandler.interactableObject.__OnTouchStop(deviceData);
				}
			}
		}
		#endregion

		#region Object Handling Touch, Grab, Use
        /// <summary>
        /// events are generated and sent to interactableObjects
        /// repeat events are run in update, based on list
        /// start & stop events are generated dynamically from inputs
        /// </summary>
        List<InteractableObject> usedInteractableObjects = new List<InteractableObject> ();
        public void DoUseHoveredAndGrabbedObjectStart (InputDeviceData deviceData) {
			HandleLayerTag_UseStartEvent(deviceData);
            if (deviceData.grabbedInteractableObject) {
                deviceData.grabbedInteractableObject.__OnUseStart (deviceData);
                usedInteractableObjects.Add (deviceData.grabbedInteractableObject);
            }
            foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
                // dont call the grabbed object twice
                if (interactableObject == deviceData.grabbedInteractableObject)
                    continue;
                interactableObject.__OnUseStart (deviceData);
                usedInteractableObjects.Add (interactableObject);
            }
            foreach (InteractableObject interactableObject in deviceData.extraReceiveEventObjects) {
                if (deviceData.hoveredInteractableObjects.Contains (interactableObject)) {
                    // skip
                } else if (interactableObject == deviceData.grabbedInteractableObject) {
                    // skip
                } else {
                    interactableObject.__OnUseStart (deviceData);
                    usedInteractableObjects.Add (interactableObject);
                }
            }
        }
        public void DoUseRepeat (InputDeviceData deviceData) {
			HandleLayerTag_UseRepeatEvent(deviceData);
            foreach (InteractableObject interactableObject in usedInteractableObjects) {
                interactableObject.__OnUseRepeat (deviceData);
            }
        }
        public void DoUseHoveredAndGrabbedObjectStop (InputDeviceData deviceData) {
			HandleLayerTag_UseStopEvent(deviceData);
            foreach (InteractableObject interactableObject in usedInteractableObjects) {
                interactableObject.__OnUseStop (deviceData);
            }
            usedInteractableObjects = new List<InteractableObject> ();
            //if (deviceData.grabbedInteractableObject) { 
            //    deviceData.grabbedInteractableObject.__OnUseStop(deviceData);
            //    usedInteractableObjects.Remove(deviceData.grabbedInteractableObject);
            //}
            //foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
            //    // dont call the grabbed object twice
            //    if (interactableObject == deviceData.grabbedInteractableObject)
            //        continue;
            //    interactableObject.__OnUseStop(deviceData);
            //    usedInteractableObjects.Remove(interactableObject);
            //}
        }

        /// <summary>
        /// events are generated and sent to interactableObjects
        /// repeat events are run in update, based on list
        /// start & stop events are generated dynamically from inputs
        /// </summary>
        List<InteractableObject> grabbedInteractableObjects = new List<InteractableObject> ();
        public void DoGrabHoveredObjectStart (InputDeviceData deviceData) {
			HandleLayerTag_GrabStartEvent(deviceData);
            // TODO: testing might not be useful here
            if (deviceData.grabbedInteractableObject) {
                deviceData.grabbedInteractableObject.__OnGrabStart (deviceData);
                grabbedInteractableObjects.Add (deviceData.grabbedInteractableObject);
            }
            // must be here
            foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
                interactableObject.__OnGrabStart (deviceData);
                grabbedInteractableObjects.Add (interactableObject);
            }
            // New: testing if this is useful
            foreach (InteractableObject interactableObject in deviceData.extraReceiveEventObjects) {
                if (deviceData.hoveredInteractableObjects.Contains (interactableObject)) {
                    // skip
                } else if (interactableObject == deviceData.grabbedInteractableObject) {
                    // skip
                } else {
                    interactableObject.__OnGrabStart (deviceData);
                    grabbedInteractableObjects.Add (interactableObject);
                }
            }
        }
        public void DoGrabRepeat (InputDeviceData deviceData) {
			HandleLayerTag_GrabRepeatEvent(deviceData);
            foreach (InteractableObject interactableObject in grabbedInteractableObjects) {
                interactableObject.__OnGrabRepeat (deviceData);
            }
        }
        public void DoGrabGrabbedObjectStop (InputDeviceData deviceData) {
			HandleLayerTag_GrabStopEvent(deviceData);
            foreach (InteractableObject interactableObject in grabbedInteractableObjects) {
                interactableObject.__OnGrabStop (deviceData);
            }
            grabbedInteractableObjects = new List<InteractableObject> ();
            //foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
            //    if (interactableObject == deviceData.grabbedInteractableObject)
            //        continue;
            //    interactableObject.__OnGrabStop(deviceData);
            //}
            //if (deviceData.grabbedInteractableObject)
            //    deviceData.grabbedInteractableObject.__OnGrabStop(deviceData);
        }

        /// <summary>
        /// events are generated and sent to interactableObjects
        /// repeat events are run in update, based on list
        /// start & stop events are generated dynamically from inputs
        /// </summary>
        List<InteractableObject> touchedInteractableObjects = new List<InteractableObject> ();
        public void DoTouchHoveredAndGrabbedObjectStart (InputDeviceData deviceData) {
			HandleLayerTag_TouchStartEvent(deviceData);
            if (deviceData.grabbedInteractableObject) {
                deviceData.grabbedInteractableObject.__OnTouchStart (deviceData);
                touchedInteractableObjects.Add (deviceData.grabbedInteractableObject);
            }
            foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
                // dont call the grabbed object twice
                if (interactableObject == deviceData.grabbedInteractableObject)
                    continue;
                interactableObject.__OnTouchStart (deviceData);
                touchedInteractableObjects.Add (interactableObject);
            }
            foreach (InteractableObject interactableObject in deviceData.extraReceiveEventObjects) {
                if (deviceData.hoveredInteractableObjects.Contains (interactableObject)) {
                    // skip
                } else if (interactableObject == deviceData.grabbedInteractableObject) {
                    // skip
                } else {
                    interactableObject.__OnTouchStart (deviceData);
                    touchedInteractableObjects.Add (interactableObject);
                }
            }
        }
        public void DoTouchRepeat (InputDeviceData deviceData) {
			HandleLayerTag_TouchRepeatEvent(deviceData);
            foreach (InteractableObject interactableObject in touchedInteractableObjects) {
                interactableObject.__OnTouchRepeat (deviceData);
            }
        }
        public void DoTouchHoveredAndGrabbedObjectStop (InputDeviceData deviceData) {
			HandleLayerTag_TouchStopEvent(deviceData);
            foreach (InteractableObject interactableObject in touchedInteractableObjects) {
                interactableObject.__OnTouchStop (deviceData);
            }
            touchedInteractableObjects = new List<InteractableObject> ();
            //if (deviceData.grabbedInteractableObject)
            //    deviceData.grabbedInteractableObject.__OnTouchStop(deviceData);
            //foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
            //    // dont call the grabbed object twice
            //    if (interactableObject == deviceData.grabbedInteractableObject)
            //        continue;
            //    interactableObject.__OnTouchStop(deviceData);
            //}
        }
		#endregion

		#region Object Handling Event One, Two, Three separately for Grabbed & Hovered state
        List<InteractableObject> eventOneHoveredInteractableObjects = new List<InteractableObject> ();
        public void DoEventOneHoveredObjectsStart (InputDeviceData deviceData) {
            foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
                interactableObject.__OnEventOneStart (deviceData);
                eventOneHoveredInteractableObjects.Add (interactableObject);
            }
        }
        public void DoEventOneHoveredObjectsStop (InputDeviceData deviceData) {
            foreach (InteractableObject interactableObject in eventOneHoveredInteractableObjects) {
                interactableObject.__OnEventOneStop (deviceData);
            }
            eventOneHoveredInteractableObjects = new List<InteractableObject> ();
            //foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
            //    interactableObject.__OnEventOneStop(deviceData);
            //}
        }

        List<InteractableObject> eventOneGrabbedInteractableObjects = new List<InteractableObject> ();
        public void DoEventOneGrabbedObjectStart (InputDeviceData deviceData) {
            if (deviceData.grabbedInteractableObject) {
                deviceData.grabbedInteractableObject.__OnEventOneStart (deviceData);
                eventOneGrabbedInteractableObjects.Add (deviceData.grabbedInteractableObject);
            }
        }
        public void DoEventOneGrabbedObjectStop (InputDeviceData deviceData) {
            foreach (InteractableObject interactableObject in eventOneGrabbedInteractableObjects) {
                interactableObject.__OnEventOneStop (deviceData);
            }
            eventOneGrabbedInteractableObjects = new List<InteractableObject> ();
            //if (deviceData.grabbedInteractableObject) { 
            //    deviceData.grabbedInteractableObject.__OnEventOneStop(deviceData);
            //}
        }

        List<InteractableObject> eventTwoHoveredInteractableObjects = new List<InteractableObject> ();
        public void DoEventTwoHoveredObjectsStart (InputDeviceData deviceData) {
            foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
                interactableObject.__OnEventTwoStart (deviceData);
                eventTwoHoveredInteractableObjects.Add (interactableObject);
            }
        }
        public void DoEventTwoHoveredObjectsStop (InputDeviceData deviceData) {
            foreach (InteractableObject interactableObject in eventTwoHoveredInteractableObjects) {
                interactableObject.__OnEventTwoStop (deviceData);
            }
            eventTwoHoveredInteractableObjects = new List<InteractableObject> ();
            //foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
            //    interactableObject.__OnEventTwoStop(deviceData);
            //}
        }

        List<InteractableObject> eventTwoGrabbedInteractableObjects = new List<InteractableObject> ();
        public void DoEventTwoGrabbedObjectStart (InputDeviceData deviceData) {
            if (deviceData.grabbedInteractableObject) {
                deviceData.grabbedInteractableObject.__OnEventTwoStart (deviceData);
                eventTwoGrabbedInteractableObjects.Add (deviceData.grabbedInteractableObject);
            }
        }
        public void DoEventTwoGrabbedObjectStop (InputDeviceData deviceData) {
            foreach (InteractableObject interactableObject in eventTwoGrabbedInteractableObjects) {
                interactableObject.__OnEventTwoStop (deviceData);
            }
            eventTwoGrabbedInteractableObjects = new List<InteractableObject> ();
            //if (deviceData.grabbedInteractableObject) { 
            //    deviceData.grabbedInteractableObject.__OnEventTwoStop(deviceData);
            //}
        }

        List<InteractableObject> eventThreeHoveredInteractableObjects = new List<InteractableObject> ();
        public void DoEventThreeHoveredObjectsStart (InputDeviceData deviceData) {
            foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
                interactableObject.__OnEventThreeStart (deviceData);
                eventThreeHoveredInteractableObjects.Add (interactableObject);
            }
        }
        public void DoEventThreeHoveredObjectsStop (InputDeviceData deviceData) {
            foreach (InteractableObject interactableObject in eventThreeHoveredInteractableObjects) {
                interactableObject.__OnEventThreeStop (deviceData);
            }
            eventThreeHoveredInteractableObjects = new List<InteractableObject> ();
            //foreach (InteractableObject interactableObject in deviceData.hoveredInteractableObjects) {
            //    interactableObject.__OnEventThreeStop(deviceData);
            //}
        }

        List<InteractableObject> eventThreeGrabbedInteractableObjects = new List<InteractableObject> ();
        public void DoEventThreeGrabbedObjectStart (InputDeviceData deviceData) {
            if (deviceData.grabbedInteractableObject) {
                deviceData.grabbedInteractableObject.__OnEventThreeStart (deviceData);
                eventThreeGrabbedInteractableObjects.Add (deviceData.grabbedInteractableObject);
            }
        }
        public void DoEventThreeGrabbedObjectStop (InputDeviceData deviceData) {
            foreach (InteractableObject interactableObject in eventThreeGrabbedInteractableObjects) {
                interactableObject.__OnEventThreeStop (deviceData);
            }
            eventThreeGrabbedInteractableObjects = new List<InteractableObject> ();
            //if (deviceData.grabbedInteractableObject) { 
            //    deviceData.grabbedInteractableObject.__OnEventThreeStop(deviceData);
            //}
        }
		#endregion
	}
}
//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NetXr {
    #if UNITY_EDITOR
    using UnityEditor;
    using System.Reflection;

    [CustomEditor(typeof(InputModule))]
    public class InputModuleInspector : Editor {
        public int executionOrder = -10000;

        public void OnEnable() {
            InputModule myTarget = (InputModule)target;
            // First you get the MonoScript of your MonoBehaviour
            MonoScript monoScript = MonoScript.FromMonoBehaviour(myTarget);
            // Getting the current execution order of that MonoScript
            int currentExecutionOrder = MonoImporter.GetExecutionOrder(monoScript);
            if (currentExecutionOrder != executionOrder) {
                // Changing the MonoScript's execution order
                MonoImporter.SetExecutionOrder(monoScript, executionOrder);
            }
        }


        public override void OnInspectorGUI() {
            // Show default inspector property editor
            DrawDefaultInspector();
        }
    }
    #endif


    [RequireComponent(typeof(EventSystem), typeof(InputDeviceManager))]
    [AddComponentMenu("Event/Extensions/WorldspaceInputModule")]
    public class InputModule : BaseInputModule { // PointerInputModule {
        #region Singleton
        private static InputModule instance = null;
        public static InputModule Instance {
            get {
                if (instance == null) {
                    instance = ((InputModule)FindObjectOfType(typeof(InputModule)));
                }
                return instance;
            }
        }
        #endregion

        public bool displayDebugRays = false;

        internal Camera raycastCamera = null;

        /// <summary>
        /// 3d drag threshold
        /// </summary>
        private static float threshold3d = 0.01f;

        protected InputModule() { }

        protected override void Awake() {
        }

        protected override void Start() {
        } 
        public bool IsInitialized () {
            return (raycastCamera != null);
        }

        public override void ActivateModule() {
            StandaloneInputModule StandAloneSystem = GetComponent<StandaloneInputModule>();

            if (StandAloneSystem != null && StandAloneSystem.enabled) {
                Debug.LogError("Aimer Input Module is incompatible with the StandAloneInputSystem, " +
                    "please remove it from the Event System in this scene or disable it when this module is in use");
            }

            SetupUiCamera();

            //WorldspaceInputDeviceManager.Instance.UpdateReticles();
        }

        public override void DeactivateModule() {
            base.DeactivateModule();
            ClearSelection();
        }

        //public void FixedUpdate() {
        //    DragObjectUpdate(GrabbedObjectUpdateType.fixedupdate);
        //}
        //public void LateUpdate() {
        //    DragObjectUpdate(GrabbedObjectUpdateType.lateupdate);
        //}
        public void Update() {
            ProcessUpdate();
            DragObjectUpdate(GrabbedObjectUpdateType.update);
        }

        //public RenderTexture cameraRenderTex;
        #region UI RAYCAST
        /// <summary>
        /// Setup a camera for vr interactions to raycast from
        /// </summary>
        private void SetupUiCamera() {
            //Debug.Log("WorldspaceInputModule.SetupUiCamera");
            // Create a new camera that will be used for raycasts
            raycastCamera = new GameObject("UI Camera", typeof(Camera)).GetComponent<Camera>();
            raycastCamera.clearFlags = CameraClearFlags.Nothing;
            raycastCamera.cullingMask = 0;
            raycastCamera.fieldOfView = 5;
            raycastCamera.nearClipPlane = 0.01f;
            raycastCamera.enabled = false;

            RenderTexture cameraRenderTex = new RenderTexture(2048, 2048, 24);
            cameraRenderTex.Create();
            raycastCamera.targetTexture = cameraRenderTex;
            RenderTexture.active = cameraRenderTex;
            
            Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
            foreach (Canvas canvas in canvases) {
                canvas.worldCamera = raycastCamera;
            }
        }

        /// <summary>
        /// run the ui raycast loop
        /// </summary>
        public override void Process() {
            //ProcessUpdate();
        }

        /// <summary>
        /// run the inputdevice update (almost everything)
        /// </summary>
        public void ProcessUpdate() {
            if (IsInitialized()) { 
                for (int i = 0; i < InputDeviceManager.Instance.GetDeviceCount(); i++) {
                    InputDevice inputDevice = InputDeviceManager.Instance.GetDevice(i);
                    if (inputDevice.isInitialized) {
                        // update camera must run even if no ui Raycast is done for positioning of physic ray
                        inputDevice.UpdateCamera(raycastCamera);
                        ProcessUiCast(inputDevice);
                        ProcessPhysicsCast(inputDevice);
                        //inputDevice.DragObject(); // is done eigther in fixedupdate, update or lateupdate

                        // update visual elements of ray and sphere casts according to active elements
                        inputDevice.UpdateVisuals();
                    } else {
                        Debug.LogWarning("WorldspaceInputModule.ProcessUpdate: " + inputDevice.deviceType + " init " + inputDevice.isInitialized);
                    }
                }
            } else {
                Debug.LogWarning("WorldspaceInputModule.ProcessUpdate: not yet initialized!");
            }
        }

        /// <summary>
        /// run the drag update (move objects around)
        /// </summary>
        public void DragObjectUpdate (GrabbedObjectUpdateType updateType) {
            for (int i = 0; i < InputDeviceManager.Instance.GetDeviceCount(); i++) {
                InputDevice inputDevice = InputDeviceManager.Instance.GetDevice(i);
                if ((inputDevice.isInitialized)) { // && (inputDevice.grabbedObjectUpdateType == updateType)) {
                    inputDevice.DragObjectUpdate();
                }
            }
        }

        /// <summary>
        /// raycast the ui's in the scene and return pointer data
        /// </summary>
        public void ProcessUiCast (InputDevice inputDevice) {
            PointerEventData3D pointerData = GetPointerEventData(inputDevice.deviceId, inputDevice, raycastCamera);

            bool pressEvent = false;
            bool releaseEvent = false;

            if (inputDevice.uiCastActive && inputDevice.deviceActive) {

                // check the press states
                bool currentlyPressed = false;
                bool currentlyReleased = false;

                // inputDevice.UpdateReticlePosition(); // i do this later now, maybe causes problems...

                inputDevice.UpdateInputAxis(ref currentlyPressed, ref currentlyReleased);
                inputDevice.UpdateReticleCountdown(pointerData, ref currentlyPressed, ref currentlyReleased);
                inputDevice.UpdateReticleTouching(pointerData, ref currentlyPressed, ref currentlyReleased);

                // convert press states to press events
                pressEvent = (!inputDevice.prevPressed && currentlyPressed);
                releaseEvent = (!inputDevice.prevReleased && currentlyReleased);

                //if (!inputDevice.prevPressed && currentlyPressed) {
                //    pressEvent = true;
                //}
                inputDevice.prevPressed = (currentlyPressed && !currentlyReleased); // we consider a released state as a prevReleased as well (continuous press&released active will send events every frame)
                inputDevice.prevReleased = currentlyReleased;
                //if (!currentlyPressed || currentlyReleased) {
                //    inputDevice.prevPressed = false;
                //}
                //if (currentlyReleased || !currentlyPressed) {
                //    inputDevice.prevReleased = false;
                //}
            }
            // if device is deactivated or ui cast not active, release 
            else {
                pressEvent = false;
                inputDevice.prevPressed = false;

                // create a release event once if previously pressed or previously not released
                releaseEvent = inputDevice.prevPressed || !inputDevice.prevReleased;
                inputDevice.prevReleased = true;
            }

            // if we have a new press reset delta to 0, usually set in GetPointerEventData but we have no pressed states there
            if (pressEvent) {
                //Debug.Log("WorldspaceInputModule.Process: pressEvent " + inputDevice.ToString());
                pointerData.delta = Vector2.zero;
                pointerData.delta3d = Vector3.zero;
            }
            // pointerData.scrollDelta could be read and set as well

            if (releaseEvent) {
                //Debug.Log("WorldspaceInputModule.Process: releaseEvent " + inputDevice.ToString());
            }

            ProcessUiInteraction(pointerData, pressEvent, releaseEvent, ref inputDevice.objectUnderAimer);

            if (releaseEvent) { // was originally currentlyReleased, but this is the same atm
                //RemovePointerData(pointerData); // dont do this here anymore, should be in device deactivation maybe...
            } else {
                ProcessMove(pointerData);
                ProcessDrag(pointerData);
            }
        }

        /// <summary>
        /// raycast the scene and return pointer data
        /// </summary>
        protected PointerEventData3D GetPointerEventData(int id, InputDevice inputDevice, Camera cam) {
            //Debug.Log("WorldspaceInputModule.GetPointerEventData: created new PointerEventData3D");
            PointerEventData3D pointerData;
            if (!GetPointerData(id, out pointerData, true)) {
                //Debug.Log("WorldspaceInputModule.GetPointerEventData: created new PointerEventData3D");
            }
            pointerData.Reset();

            // other system read pressed & released here, but we cant because gaze needs hover object first

            Vector2 pointerPosition = inputDevice.GetPointerScreenpointPosition();
            // https://forum.unity3d.com/threads/using-the-controller-to-interact-with-unity-ui.443927/
            pointerData.delta = pointerPosition - pointerData.position; // we reset to 0 if pressed once we got the inputs
            pointerData.position = pointerPosition;

            // TODO: RETO maybe needs to be replaced with GraphicRaycaster?
            //GraphicRaycaster gr = new GraphicRaycaster(); // not working, it's a monobehaviour that is on the Canvas
            //gr.Raycast(pointerData, m_RaycastResultCache);
            EventSystem.current.RaycastAll(pointerData, m_RaycastResultCache);

            RaycastResult rayResult = FindFirstRaycast(m_RaycastResultCache);
            pointerData.pointerCurrentRaycast = rayResult;
            m_RaycastResultCache.Clear();

            // calculate distance of hit
            float intersectionDistance = pointerData.pointerCurrentRaycast.distance + cam.nearClipPlane;

            // object hit within the checked distance
            if (intersectionDistance <= inputDevice.uiRayLength) {
            }
            // object hit is out of ui check distance...
            else {
                rayResult.gameObject = null;
                intersectionDistance = inputDevice.uiRayLength;
            }

            // if no collision we set a long distance to intersectionDistance
            //if (rayResult.gameObject == null) {
            //    if (cam.farClipPlane < 1) {
            //        intersectionDistance = inputDevice.uiRayLength;
            //    } else {
            //        intersectionDistance = cam.farClipPlane;
            //    }
            //}

            // store informations to device data which can be used in event sending
            inputDevice.deviceData.uiHit = (rayResult.gameObject != null);
            inputDevice.deviceData.source = cam.transform.position;
            inputDevice.deviceData.uiHitDistance = intersectionDistance;

            if (inputDevice.deviceData.uiHit) { 
                inputDevice.deviceData.direction = cam.ViewportPointToRay((Vector3)inputDevice.GetPointerViewportPosition()).direction;
                inputDevice.deviceData.uiHitRotation = rayResult.gameObject.transform.rotation;
                //Debug.Log("ui hit normal " + rayResult.worldNormal);
                inputDevice.deviceData.uiHitNormal = rayResult.worldNormal;
            } else { 
                inputDevice.deviceData.direction = cam.transform.forward;
            }

            // how much was the cursor moved in this frame
            Vector3 intersectionPosition = cam.transform.position + inputDevice.deviceData.uiHitDistance * inputDevice.deviceData.direction;
            pointerData.delta3d = intersectionPosition - pointerData.position3d;
            pointerData.position3d = intersectionPosition;

            return pointerData;
        }

        /// <summary>
        /// handle press and release on the object
        /// </summary>
        private void ProcessUiInteraction(PointerEventData3D pointerEvent, bool pressed, bool released, ref GameObject objectUnderAimer) {
            GameObject currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

            objectUnderAimer = ExecuteEvents.GetEventHandler<ISubmitHandler>(currentOverGo); //we only want objects that we can submit on.

            if (pressed) {
                //Debug.Log("WorldspaceInputModule.WorldspaceInputModule.ProcessInteraction: pressed");
                pointerEvent.eligibleForClick = true;
                pointerEvent.delta = Vector2.zero;
                pointerEvent.dragging = false;
                pointerEvent.useDragThreshold = true;
                pointerEvent.pressPosition = pointerEvent.position;
                pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;

                pointerEvent.delta3d = Vector3.zero;
                pointerEvent.pressPosition3d = pointerEvent.position3d;

                DeselectIfSelectionChanged(currentOverGo, pointerEvent);

                if (pointerEvent.pointerEnter != currentOverGo) {
                    // send a pointer enter to the touched element if it isn't the one to select...
                    HandlePointerExitAndEnter(pointerEvent, currentOverGo);
                    pointerEvent.pointerEnter = currentOverGo;
                }

                // search for the control that will receive the press
                // if we can't find a press handler set the press
                // handler to be what would receive a click.
                GameObject newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.pointerDownHandler);

                // didnt find a press handler... search for a click handler
                //if (newPressed == null) {
                //newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.pointerDownHandler);
                if (newPressed == null) {
                    newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);
                }
                /*else {
                    pointerEvent.eligibleForClick = false;
                }*/

                float time = Time.unscaledTime;

                if (newPressed == pointerEvent.lastPress) {
                    var diffTime = time - pointerEvent.clickTime;
                    if (diffTime < 0.3f)
                        ++pointerEvent.clickCount;
                    else
                        pointerEvent.clickCount = 1;

                    pointerEvent.clickTime = time;
                } else {
                    pointerEvent.clickCount = 1;
                }

                /*if (newPressed != pointerEvent.pointerPress) {
                    pointerEvent.pointerPress = newPressed;
                    pointerEvent.rawPointerPress = currentOverGo;
                    pointerEvent.clickCount = 0;
                }*/

                pointerEvent.pointerPress = newPressed;
                pointerEvent.rawPointerPress = currentOverGo;

                pointerEvent.clickTime = time;

                // Save the drag handler as well
                pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

                if (pointerEvent.pointerDrag != null) {
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);
                    //ExecuteEvents.Execute<IBeginDragHandler>(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);
                    //pointerEvent.dragging = true;
                }
            }

            if (released) {
                //Debug.Log("WorldspaceInputModule.WorldspaceInputModule.ProcessInteraction: released");
                ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

                // see if we mouse up on the same element that we clicked on...
                GameObject pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                // PointerClick
                if (pointerEvent.pointerPress == pointerUpHandler && pointerEvent.eligibleForClick) {
                    ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerClickHandler);
                } else if (pointerEvent.pointerDrag != null && pointerEvent.dragging) {
                    //Debug.Log("WorldspaceInputModule.ProcessPressInteraction: drag drop");
                    ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.dropHandler);
                }

                pointerEvent.eligibleForClick = false;
                pointerEvent.pointerPress = null;
                pointerEvent.rawPointerPress = null;

                if (pointerEvent.pointerDrag != null && pointerEvent.dragging) {
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);
                    //Debug.Log("WorldspaceInputModule.ProcessPressInteraction: stop drag 1");

                    pointerEvent.dragging = false;
                    pointerEvent.pointerDrag = null;
                }

                if (pointerEvent.pointerDrag != null) {
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);
                    //Debug.Log("WorldspaceInputModule.ProcessPressInteraction: stop drag 2");

                    pointerEvent.pointerDrag = null;
                }

                // send exit events as we need to simulate this on touch up on touch device
                ExecuteEvents.ExecuteHierarchy(pointerEvent.pointerEnter, pointerEvent, ExecuteEvents.pointerExitHandler);
                pointerEvent.pointerEnter = null;
            }

            // Drag notification
            /*bool moving = true; // could be created from positional change tracking
            if (pointerEvent.dragging && moving && pointerEvent.pointerDrag != null) {
                // Before doing drag we should cancel any pointer down state
                // And clear selection!
                if (pointerEvent.pointerPress != pointerEvent.pointerDrag) {
                    ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

                    pointerEvent.eligibleForClick = false;
                    pointerEvent.pointerPress = null;
                    pointerEvent.rawPointerPress = null;
                }
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.dragHandler);
            }*/
        }

        /// <summary>
        /// Improved raycast method for world space canvases
        /// </summary>
        //internal new static RaycastResult FindFirstRaycast(List<RaycastResult> candidates) {
        //    RaycastResult first = new RaycastResult();
        //    float distance = float.PositiveInfinity;
        //    for (var i = 0; i < candidates.Count; ++i) {
        //        // skip empty ones
        //        if (candidates[i].gameObject == null)
        //            continue;

        //        if ((distance - candidates[i].distance) > float.Epsilon) {
        //            first = candidates[i];
        //            distance = candidates[i].distance;
        //        }
        //        //return candidates[i];
        //    }

        //#if false
        //        string log = "" + candidates.Count + " : ";
        //        foreach (RaycastResult raycastResult in candidates) {
        //            log += ", " + raycastResult.gameObject.name + "(" + raycastResult.distance + ")";
        //        }
        //        Debug.Log ("FindFirstRaycast " + (first.gameObject != null ? first.gameObject.name : "null") + " from " + log);
        //#endif

        //            return first;
        //        }
        #endregion

        #region PHYSIC RAYCAST
        /// <summary>
        /// raycast the scene for normal collisions (not ui)
        /// </summary>
        internal void ProcessPhysicsCast(InputDevice inputDevice) {
            // check for objects in raycast
            List<InteractableObject> newTouchedObjects = new List<InteractableObject>();
            Vector3 worldspaceHit = Vector3.one;

            if (inputDevice.physicsCastActive && inputDevice.deviceActive) {
                if (inputDevice.uiRaySource == null) {
                    Debug.LogError("WorldspaceInputModule.ProcessPhysics: inputDevice.raySource is null on " + inputDevice.ToString());
                    return;
                }
                if (inputDevice.physicsRaySource == null) {
                    Debug.LogError("WorldspaceInputModule.ProcessPhysics: inputDevice.physicsRaySource is null on " + inputDevice.ToString());
                    return;
                }
                if (inputDevice.sphereCastPoint == null) {
                    Debug.LogError("WorldspaceInputModule.ProcessPhysics: inputDevice.sphereCastPoint is null on " + inputDevice.ToString());
                    return;
                }

                // check for objects in raycast
                //List<InteractableObject> newTouchedObjects = null;
                switch (inputDevice.physicsCastType) {
                    case PhysicsCastType.disabled:
                        break;
                    case PhysicsCastType.parabola:
                        ParabolacastPhysicsScene(inputDevice, ref newTouchedObjects, ref worldspaceHit);
                        break;
                    case PhysicsCastType.ray:
                        RaycastPhysicsScene(inputDevice, ref newTouchedObjects, ref worldspaceHit);
                        break;
                    case PhysicsCastType.sphere:
                        SpherecastPhysicsScene(inputDevice, ref newTouchedObjects, ref worldspaceHit);
                        break;
                }

                for (int i = newTouchedObjects.Count - 1; i >= 0; i--) {
                    InteractableObject newTObj = newTouchedObjects[i];
                    int oldIndex = inputDevice.deviceData.hoveredInteractableObjects.IndexOf(newTObj);
                    // objects in old and new list are, touch staying
                    if (oldIndex != -1) {
                        inputDevice.deviceData.hoveredInteractableObjects[oldIndex].OnHoverRepeat(inputDevice.deviceData);
                        inputDevice.deviceData.hoveredInteractableObjects.RemoveAt(oldIndex);
                    }
                    // new contacts if not in old list, but in new
                    else {
                        newTObj.OnHoverEnter(inputDevice.deviceData);
                    }
                }
            }
            // dont create new list if deactivated, will release everything we have atm.
            else {
                if (inputDevice.deviceData.hoveredInteractableObjects.Count > 0) {
                    Debug.Log("WorldspaceInputModule.ProcessPhysicsCast: device " + inputDevice.ToString() + " deactivated, untouching objects");
                }
            }

            // what remains in the list are contacts we lost
            for (int i = 0; i < inputDevice.deviceData.hoveredInteractableObjects.Count; i++) {
                inputDevice.deviceData.hoveredInteractableObjects[i].OnHoverExit(inputDevice.deviceData);
            }
            inputDevice.deviceData.hoveredInteractableObjects = newTouchedObjects;
            inputDevice.deviceData.worldspaceHit = worldspaceHit;
        }

        /// <summary>
        /// search for a InteractableObject on the searchObject
        /// </summary>
        private void SearchInteractableObjectComponent(GameObject searchObject, ref List<InteractableObject> collisionInteractableObjects) {
            InteractableObject interactableObject = searchObject.GetComponent<InteractableObject>();
            if (interactableObject != null) {
                collisionInteractableObjects.Add(interactableObject);
            } else {
                interactableObject = searchObject.GetComponentInChildren<InteractableObject>();
                if (interactableObject != null) {
                    collisionInteractableObjects.Add(interactableObject);
                } else {
                    interactableObject = searchObject.GetComponentInParent<InteractableObject>();
                    if (interactableObject != null) {
                        collisionInteractableObjects.Add(interactableObject);
                    }
                }
            }
        }

        /// <summary>
        /// make a physics raycast into the scene to detect interactableobjects
        /// </summary>
        private void RaycastPhysicsScene(InputDevice inputDevice, ref List<InteractableObject> collisionInteractableObjects, ref Vector3 worldspaceHit) {
            Ray ray = new Ray(inputDevice.physicsRaySource.transform.position, inputDevice.physicsRaySource.transform.forward);
            //Debug.Log("NetworkPlayerController.RaycastScene "+ ray);
            //List<NetworkInteractableObject> collisionInteractableObjects = new List<NetworkInteractableObject>();
            //collisionInteractableObjects = new List<InteractableObject>();
            worldspaceHit = Vector3.zero;

            RaycastHit raycastHit;
            if (InputModule.Instance.displayDebugRays) {
                Debug.DrawRay(ray.origin, ray.direction * inputDevice.physicsRayLength, Color.blue);
            }
            if (Physics.Raycast(ray, out raycastHit, inputDevice.physicsRayLength)) {
                if (InputModule.Instance.displayDebugRays) {
                    Debug.DrawRay(ray.origin, raycastHit.point - ray.origin, Color.red);
                }
                worldspaceHit = raycastHit.point;
                //Debug.Log("NetworkPlayerController.RaycastScene: has hit " + raycastHit.collider.name);
                if (raycastHit.collider.gameObject) {
                    //Debug.Log("NetworkPlayerController.Raycast: no AtomObject script ");
                    SearchInteractableObjectComponent(raycastHit.collider.gameObject, ref collisionInteractableObjects);
                }
            }
            //return collisionInteractableObjects;
        }

        /// <summary>
        /// make a physics sphere cast into the world to find InteractableObjects
        /// </summary>
        private void SpherecastPhysicsScene(InputDevice inputDevice, ref List<InteractableObject> collisionInteractableObjects, ref Vector3 worldspaceHit) {
            Vector3 position = inputDevice.sphereCastPoint.transform.position;
            float radius = inputDevice.sphereCastRadius;

            // RaycastHit raycastHit;
            Collider[] hitColliders = Physics.OverlapSphere(position, radius);
            if (hitColliders.Length > 0) {
                foreach (Collider coll in hitColliders) {
                    // TODO: Reto, should return the closest InteractableObject, not the first one in the list (might be correct anyway)
                    if (coll.gameObject) {
                        SearchInteractableObjectComponent(coll.gameObject, ref collisionInteractableObjects);
                    }
                }
            }
            //return collisionInteractableObjects;
        }

        /// <summary>
        /// do a parabola cast into the scene
        /// based on http://wiki.unity3d.com/index.php/Trajectory_Simulation
        /// </summary>
        /// <param name="inputDevice"></param>
        /// <param name="collisionInteractableObjects"></param>
        /// <param name="worldspaceHit"></param>
        private void ParabolacastPhysicsScene(InputDevice inputDevice, ref List<InteractableObject> collisionInteractableObjects, ref Vector3 worldspaceHit) {
            Vector3 source = inputDevice.physicsRaySource.transform.position;
            Vector3 direction = inputDevice.physicsRaySource.transform.forward;

            float rayVelocity = inputDevice.parabolicRayVelocity;
            int maxSegmentCount = 30;
            float segmentLength = 0.5f;

            //Vector3[] segments = new Vector3[maxSegmentCount];
            List<Vector3> segments = new List<Vector3>();

            // The first line point is wherever the player's cannon, etc is
            segments.Add(source);

            // The initial velocity
            Vector3 segVelocity = direction * rayVelocity;

            // reset our hit object
            Collider _hitObject = null;

            for (int i = 1; i < maxSegmentCount; i++) {
                // Time it takes to traverse one segment of length segScale (careful if velocity is zero)
                float segTime = (segVelocity.sqrMagnitude != 0) ? segmentLength / segVelocity.magnitude : 0;

                // Add velocity from gravity for this segment's timestep
                segVelocity = segVelocity + Physics.gravity * segTime;

                // Check to see if we're going to hit a physics object
                RaycastHit hit;
                if (Physics.Raycast(segments[i - 1], segVelocity, out hit, segmentLength)) {
                    // remember who we hit
                    _hitObject = hit.collider;

                    // set next position to the position where we hit the physics object
                    segments.Add(segments[i - 1] + segVelocity.normalized * hit.distance);

                    break;

                    // reflect / bounce on ground
                    // if (false) {
                    //     // correct ending velocity, since we didn't actually travel an entire segment
                    //     segVelocity = segVelocity - Physics.gravity * (segmentLength - hit.distance) / segVelocity.magnitude;
                    //     // flip the velocity to simulate a bounce
                    //     segVelocity = Vector3.Reflect(segVelocity, hit.normal);
                    // }
                }
                // If our raycast hit no objects, then set the next position to the last one plus v*t
                else {
                    segments.Add(segments[i - 1] + segVelocity * segTime);
                }
            }
            
            if (_hitObject != null) {
                SearchInteractableObjectComponent(_hitObject.gameObject, ref collisionInteractableObjects);
                worldspaceHit = segments[segments.Count - 1];

                inputDevice.parabolaPositions = segments.ToArray();
            } else {
                inputDevice.parabolaPositions = new Vector3[0];
            }

            //teleporterData.validTargetPosition = (_hitObject != null);
            //if (_hitObject != null) {
            //    teleporterData.parabolaLR.numPositions = segments.Count;
            //    teleporterData.parabolaLR.SetPositions(segments.ToArray());
            //    teleporterData.targetPosition = segments[segments.Count - 1];
            //    //return segments[segments.Count - 1];
            //} else {
            //    teleporterData.parabolaLR.numPositions = 0;
            //}
            //Debug.Log("NETXR.SimulateTrajectory: valid target " + _hitObject + " " + segments.Count + " " + teleporterData.targetPosition);
        }
        #endregion

        #region UNMODIFIED FROM POINTERINPUTMODULE
        protected Dictionary<int, PointerEventData3D> m_PointerData = new Dictionary<int, PointerEventData3D>();

        // walk up the tree till a common root between the last entered and the current entered is foung
        // send exit events up to (but not inluding) the common root. Then send enter events up to
        // (but not including the common root).
        protected new void HandlePointerExitAndEnter(PointerEventData currentPointerData, GameObject newEnterTarget) {
            // if we have no target / pointerEnter has been deleted
            // just send exit events to anything we are tracking
            // then exit
            if (newEnterTarget == null || currentPointerData.pointerEnter == null) {
                for (var i = 0; i < currentPointerData.hovered.Count; ++i)
                    ExecuteEvents.Execute(currentPointerData.hovered[i], currentPointerData, ExecuteEvents.pointerExitHandler);

                currentPointerData.hovered.Clear();

                if (newEnterTarget == null) {
                    currentPointerData.pointerEnter = newEnterTarget;
                    return;
                }
            }

            // if we have not changed hover target
            if (currentPointerData.pointerEnter == newEnterTarget && newEnterTarget)
                return;

            GameObject commonRoot = FindCommonRoot(currentPointerData.pointerEnter, newEnterTarget);

            // and we already an entered object from last time
            if (currentPointerData.pointerEnter != null) {
                // send exit handler call to all elements in the chain
                // until we reach the new target, or null!
                Transform t = currentPointerData.pointerEnter.transform;

                while (t != null) {
                    // if we reach the common root break out!
                    if (commonRoot != null && commonRoot.transform == t)
                        break;

                    ExecuteEvents.Execute(t.gameObject, currentPointerData, ExecuteEvents.pointerExitHandler);
                    currentPointerData.hovered.Remove(t.gameObject);
                    t = t.parent;
                }
            }

            // now issue the enter call up to but not including the common root
            currentPointerData.pointerEnter = newEnterTarget;
            if (newEnterTarget != null) {
                Transform t = newEnterTarget.transform;

                while (t != null && t.gameObject != commonRoot) {
                    ExecuteEvents.Execute(t.gameObject, currentPointerData, ExecuteEvents.pointerEnterHandler);
                    currentPointerData.hovered.Add(t.gameObject);
                    t = t.parent;
                }
            }
        }

        private static bool ShouldStartDrag(Vector3 pressPos, Vector3 currentPos, float threshold, bool useDragThreshold) {
            if (!useDragThreshold)
                return true;

            bool result = (pressPos - currentPos).magnitude >= threshold3d;
            //Debug.Log ("WorldspaceInputModule.ShouldStartDrag: " + result + " " + pressPos + " " + currentPos + " " + (pressPos - currentPos).sqrMagnitude + " " + threshold);
            return result;
        }

        protected virtual void ProcessMove(PointerEventData3D pointerEvent) {
            //var targetGO = (Cursor.lockState == CursorLockMode.Locked ? null : pointerEvent.pointerCurrentRaycast.gameObject);
            var targetGO = (pointerEvent.pointerCurrentRaycast.gameObject);
            HandlePointerExitAndEnter(pointerEvent, targetGO);
        }

        protected virtual void ProcessDrag(PointerEventData3D pointerEvent) {
            if (//!pointerEvent.IsPointerMoving() ||
                //Cursor.lockState == CursorLockMode.Locked ||
                pointerEvent.pointerDrag == null) {
                //Debug.Log("WorldspaceInputModule.ProcessDrag: pointer not moving "+ pointerEvent.IsPointerMoving()+" "+(pointerEvent.pointerDrag == null));
                return;
            }
            //Debug.Log("WorldspaceInputModule.ProcessDrag");

            if (!pointerEvent.dragging
                    && ShouldStartDrag(pointerEvent.pressPosition3d, pointerEvent.position3d, eventSystem.pixelDragThreshold, pointerEvent.useDragThreshold)
                    ) {
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.beginDragHandler);
                //Debug.Log("WorldspaceInputModule.ProcessDrag: start dragging");
                pointerEvent.dragging = true;
            }

            // Drag notification
            if (pointerEvent.dragging) {
                // Before doing drag we should cancel any pointer down state
                // And clear selection!
                if (pointerEvent.pointerPress != pointerEvent.pointerDrag) {
                    ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

                    pointerEvent.eligibleForClick = false;
                    pointerEvent.pointerPress = null;
                    pointerEvent.rawPointerPress = null;
                }
                //Debug.Log("WorldspaceInputModule.ProcessDrag: dragging");
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.dragHandler);
            }
        }

        protected bool GetPointerData(int id, out PointerEventData3D data, bool create) {
            if (!m_PointerData.TryGetValue(id, out data) && create) {
                data = new PointerEventData3D(eventSystem) {
                    pointerId = id,
                };
                m_PointerData.Add(id, data);
                return true;
            }
            return false;
        }

        protected void ClearSelection() {
            Debug.LogWarning("WorldspaceInputModule.ClearSelection: resetting all pointers!");
            var baseEventData = GetBaseEventData();

            foreach (var pointer in m_PointerData.Values) {
                // clear all selection
                HandlePointerExitAndEnter(pointer, null);
            }

            m_PointerData.Clear();
            eventSystem.SetSelectedGameObject(null, baseEventData);
        }

        protected void RemovePointerData(PointerEventData3D data) {
            Debug.Log("WorldspaceInputModule.ClearSelection: removing pointer "+ data.pointerId);
            m_PointerData.Remove(data.pointerId);
        }

        protected void DeselectIfSelectionChanged(GameObject currentOverGo, BaseEventData pointerEvent) {
            // Selection tracking
            var selectHandlerGO = ExecuteEvents.GetEventHandler<ISelectHandler>(currentOverGo);
            // if we have clicked something new, deselect the old thing
            // leave 'selection handling' up to the press event though.
            if (selectHandlerGO != eventSystem.currentSelectedGameObject)
                eventSystem.SetSelectedGameObject(null, pointerEvent);
        }
        #endregion
    }
}
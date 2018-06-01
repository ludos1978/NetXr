//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Linq;

namespace NetXr {

/*    // THIS IS NO MONOBEHAVIOUR...
 *    #region Inspector
#if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(InputDevice))]
    public class InputDeviceInspector : Editor {
        public int executionOrder = -8000;

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
    }
#endif
    #endregion*/

    #region ENUMS
    public enum PointerOffsetMode {
        undefined,
        screenCenter,
        vrCenter,
        mousePosition
    }

    public enum ActivationMethod {
        InputManagerAxis,
        HoverTimer,
        TouchDistance,
        ExternalInput
    }

    public enum InputDeviceType {
        Mouse,
        Camera,
        Vive,
        Leap,
    }

    public enum InputDeviceHand {
        Undefined,
        Left,
        Right
    }

    public enum UiCastVisualizeSetting {
        Never,
        UiHit,
        Allways
    }

    public enum PhysicsCastVisualizeSetting {
        Never,
        Allways
    }

    public enum PhysicsCastType {
        disabled,
        ray,
        parabola,
        sphere
    }

    public enum GrabbedObjectUpdateType {
        disabled,
        update,
        lateupdate,
        fixedupdate
    }

    #endregion

    #region UnityEvents for InputController callbacks
    [System.Serializable]
    public class UnityDeviceEvent : UnityEvent<InputDevice> { }
    #endregion

    public class InputDeviceData {
        // the associated WorldspaceInputDevice
        public InputDevice inputDevice;
        // the position of this controller
        public Vector3 source;
        // the direction this controller is facing
        public Vector3 direction;
        // the input device movement (for physical objects, taking the velocity on release)
        public Vector3 deviceVelocity = Vector3.zero;
        public Vector3 deviceAngularVelocity = Vector3.zero;

        // value given from the controller (such as trigger pull, or touchpad position)
        public float inputValueX;
        public float inputValueY;

        // has a ui been hit by the ray
        public bool uiHit = false;
        // how far away has the ui been hit
        public float uiHitDistance = 0;
        // a world space hit position
        public Vector3 worldspaceHit;
        public Vector3 uiHitNormal;
        // the direction a ui is facing which has been hit
        public Quaternion uiHitRotation;

        // the InteractableObjects hovered by this device
        public List<InteractableObject> hoveredInteractableObjects = new List<InteractableObject>();
        // the InteractableObjects grabbed by this device
        public InteractableObject grabbedInteractableObject = null;
        // allows adding InteractableObjects which will receive the use, touch and attach events
        public List<InteractableObject> extraReceiveEventObjects = new List<InteractableObject>();

        public override string ToString() {
            return "DeviceData-(" + inputDevice + ")";
        }
    }

    /// <summary>
    /// A WorldspaceInputDevice is the visual representation of a device in the Virtual Envrionment
    /// it handles the rays, controls the interactions with objects in the world, ...
    /// </summary>
    [System.Serializable]
    public class InputDevice {
        [Header("What type and hand is this, can be used to search for an input device")]
        public InputDeviceType deviceType;
        public InputDeviceHand deviceHand;

        /// <summary>
        /// the deviceIdentifier is a unique value that can only be defined once and not modifed afterwards
        /// </summary>
        private int _deviceId = -1;
        public int deviceId {
            get {
                return _deviceId;
            }
            set {
                if (_deviceId == -1) {
                    if (value < 0)
                        Debug.LogError("WorldspaceInputDevice.deviceId: must be positive");
                    _deviceId = value;
                } else {
                    Debug.LogError("WorldspaceInputDevice.deviceId: cannot modify value once set");
                }
            }
        }

        [HideInInspector]
        internal bool isInitialized = false;
        private bool _deviceActive = false;
        public bool uiCastActive = false;
        public bool physicsCastActive = false;

        [HideInInspector]
        public bool deviceActive {
            get {
                return _deviceActive;
            }
            set {
                if (_deviceActive != value) {
                    _deviceActive = value;
                    if (value) {
                        OnSetDeviceActiveEvent();
                        onSetDeviceActiveEvent.Invoke(this);
                    } else {
                        OnSetDeviceInactiveEvent();
                        onSetDeviceInactiveEvent.Invoke(this);
                    }
                }
            }
        }

        /// <summary>
        /// to store data that can be given with an event trigger (mouse button presses etc.)
        /// </summary>
        public InputDeviceData deviceData = new InputDeviceData();

        [Header("retrievable value on searching the device")]
        public InputController inputController;

        [Header("The Controller itself")]
        public Transform controller;
        [Header("Source of the Ui Ray")]
        public Transform uiRaySource;
        [Header("Source of the Physics Ray")]
        public Transform physicsRaySource;
        [Header("Source of the Sphere Casts")]
        public Transform sphereCastPoint;
        [Header("Position of the Attachement point")]
        public Transform grabAttachementPoint;

        //public GrabbedObjectUpdateType grabbedObjectUpdateType = GrabbedObjectUpdateType.update;

        public PhysicsCastType physicsCastType = PhysicsCastType.disabled;
        public float sphereCastRadius = 0.1f;

        [Header("Reticle Style")]
        public Sprite overrideReticleSprite;
        [HideInInspector]
        private Image reticle;
        public bool showReticle = true;
        //public float minReticleSize = 0.005f;
        //public float maxReticleSize = 0.02f;
        public float reticleSize = 0.05f;
        public Color reticleActiveColor = Color.white;
        public Color reticleInactiveColor = Color.gray;

        [Header("Ray Style")]
        private LineRenderer _physicsRay;
        public LineRenderer physicsRay { get { return _physicsRay; } }
        private LineRenderer _uiRay;
        public LineRenderer uiRay { get { return _uiRay; } }
        public UiCastVisualizeSetting showUiRaySetting = UiCastVisualizeSetting.UiHit;
        public PhysicsCastVisualizeSetting showPhysicsRaySetting = PhysicsCastVisualizeSetting.Never;
        public float uiRayLength = 3f;
        public float physicsRayLength = 30.0f;
        // positions of the parabola, calculated in ParabolacastPhysicsScene if physicsCastType == PhysicsCastType.parabola
        internal Vector3[] parabolaPositions = new Vector3[0];
        public float parabolicRayVelocity = 10.0f;

        private GameObject sphere;

        public Material uiRayMaterial;
        public float uiRayWidth = 0.005f;
        public Gradient uiRayColors = new Gradient();
        //public Color uiRayColor = Color.yellow;
        public Material physicsRayMaterial;
        public float physicsRayWidth = 0.005f;
        public Gradient physicRayColors = new Gradient();
        //public Color physicsRayColor = Color.blue;

        [HideInInspector]
        public GameObject objectUnderAimer;

        [Header("UI Interaction Related settings")]
        /// <summary>
        /// 
        /// </summary>
        public PointerOffsetMode uiPointerOffsetMode = PointerOffsetMode.undefined;

        /// <summary>
        /// how is a ui element triggered (based on distance, timer, inputManagerAxis or external variable definition
        /// to define externally set externalpressed and externalreleased
        /// </summary>
        public ActivationMethod uiActivationMethod = ActivationMethod.TouchDistance;

        /// <summary>
        /// set from an external script if the internal method of pressing/releasing does not work for you
        /// </summary>
        [HideInInspector]
        public bool externalPressed = false;
        [HideInInspector]
        public bool externalReleased = false;

        /// <summary>
        /// was the button previously pressed
        /// </summary>
        [HideInInspector]
        public bool prevPressed = false;
        [HideInInspector]
        public bool prevReleased = false;

        /// <summary>
        /// The Input axis name used to activate the object under the reticle.
        /// </summary>
        [Header("only used if activationMethod.InputManagerAxis, can be axis or button")]
        public string uiActivationAxis = "Submit";

        [Header("only used if activationMethod.HoverTimer")]
        [Tooltip("if > 0 hovering over an object activates it after this delay")]
        public float hoverActivationTime = 0.9f;
        private float hoverActivationTimer;

        [Header("only used if activationMethod.TouchDistance")]
        public float minTouchDistance = 0.02f;
        public float maxTouchDistance = 0.03f;

        private GameObject prevRayhitObject;
        private GameObject prevSubmittableRayhitObject;
        private GameObject prevHoverObject;
        private GameObject prevPressObject;
        private GameObject prevDragObject;
        // private float prevIntersectionDistance = float.MaxValue;

        public UnityDeviceEvent onSetDeviceInactiveEvent = new UnityDeviceEvent();
        public UnityDeviceEvent onSetDeviceActiveEvent = new UnityDeviceEvent();

        public Vector3 prevPosition;
        public Vector3 prevRotation;

        //PointerEventData3D pointerData = new PointerEventData3D();

        /// <summary>
        /// called from WorldspaceInputDeviceManager
        /// </summary>
        internal int Initialize() {
            if (!isInitialized) {
                deviceId = InputDeviceManager.Instance.GetDeviceId();
                isInitialized = true;
                Debug.Log("WorldspaceInputDevice.Initialize: " + this.ToString());
                deviceData = new InputDeviceData();
                deviceData.inputDevice = this;
                SetupVisuals();
            }
            return deviceId;
        }

        /// <summary>
        /// when the device is set to active state
        /// </summary>
        private void OnSetDeviceActiveEvent() {
            //Debug.Log("WorldspaceInputDevice.OnSetDeviceActiveEvent");
        }

        /// <summary>
        /// when the device is set to inactive state
        /// </summary>
        private void OnSetDeviceInactiveEvent() {
            DetachObject();
            //deviceData.grabbedInteractableObject = null;
            //Debug.Log("WorldspaceInputDevice.OnSetDeviceInactiveEvent");
        }

        /// <summary>
        /// setup the reticle for this input device
        /// </summary>
        /// <param name="parentTransform"></param>
        /// <param name="defaultReticleSprite"></param>
        internal void SetupVisuals() {
            if (reticle == null) {
                if ((grabAttachementPoint == null) || (uiRaySource == null) || (physicsRaySource == null)) {
                    Debug.LogError("WorldspaceInputDevice.SetupVisuals: something is null: grabAttachementPoint = '" + grabAttachementPoint + "' raySource = '" + uiRaySource + "' physicsRaySource = '" + physicsRaySource + "' in " + this);
                }

                string label = deviceId.ToString();
                label += "-" + ((controller != null) ? controller.name : "");
                label += "-" + deviceType + "/" + deviceHand;
                string uiRayLabel = "UiRay" + label;
                string physicsRayLabel = "PhysicsRay" + label;
                string sphereLabel = "Sphere" + label;

                GameObject uiRayGo = new GameObject(uiRayLabel, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasRenderer), typeof(Image), typeof(LineRenderer));
                uiRayGo.transform.SetParent(InputModule.Instance.transform);

                uiRayGo.GetComponent<RectTransform>().sizeDelta = new Vector2(3, 3);
                uiRayGo.GetComponent<RectTransform>().localScale = Vector3.one * 0.01f;
                uiRayGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                uiRayGo.GetComponent<Canvas>().sortingOrder = 1;

                // reticle setup
                reticle = uiRayGo.GetComponent<Image>();
                if (InputDeviceManager.Instance.defaultReticleSprite != null) {
                    reticle.sprite = InputDeviceManager.Instance.defaultReticleSprite;
                }
                if (overrideReticleSprite != null) {
                    reticle.sprite = overrideReticleSprite;
                }
                reticle.type = Image.Type.Filled;
                reticle.fillOrigin = 2; // top
                reticle.fillClockwise = false;

                // line to target setup
                _uiRay = uiRayGo.GetComponent<LineRenderer>();
                _uiRay.widthMultiplier = uiRayWidth;
                //_uiRay.startColor = uiRayColor;
                //_uiRay.endColor = uiRayColor;
                _uiRay.colorGradient = uiRayColors;
                _uiRay.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _uiRay.receiveShadows = false;
                if (uiRayMaterial) {
                    _uiRay.material = uiRayMaterial;
                } else {
                    _uiRay.material = new Material(Shader.Find("Transparent/Diffuse"));
                }

                GameObject physicsRayGo = new GameObject(physicsRayLabel, typeof(LineRenderer));
                physicsRayGo.transform.SetParent(InputModule.Instance.transform);

                // line to target setup
                _physicsRay = physicsRayGo.GetComponent<LineRenderer>();
                _physicsRay.widthMultiplier = physicsRayWidth;
                //_physicsRay.startColor = physicsRayColor;
                //_physicsRay.endColor = physicsRayColor;
                _physicsRay.colorGradient = physicRayColors;
                _physicsRay.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _physicsRay.receiveShadows = false;
                if (physicsRayMaterial) {
                    _physicsRay.material = physicsRayMaterial;
                } else {
                    _physicsRay.material = new Material(Shader.Find("Transparent/Diffuse"));
                }

                sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = sphereLabel;
                sphere.transform.SetParent(InputModule.Instance.transform);
                sphere.SetActive(false);
                Object.Destroy(sphere.GetComponent<Collider>());

                sphere.transform.localScale = Vector3.one * sphereCastRadius;
                //sphere.transform.position = grabAttachementPoint.transform.position;
                //sphere.transform.rotation = grabAttachementPoint.transform.rotation;

                Material material = sphere.GetComponent<Renderer>().material;
                material.color = new Color(.1f, .1f, .1f, .1f);
                material.SetFloat("_Mode", 2);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
            }
        }

        #region POINTER ON SCREEN POSITION
        /// <summary>
        /// return the pointer screen position in 0..screenSize coordinates
        /// </summary>
        /// <returns></returns>
        internal Vector2 GetPointerScreenpointPosition() {
            Vector2 pos = Vector2.zero;
            switch (uiPointerOffsetMode) {
                case PointerOffsetMode.undefined:
                    // could be moved to initialization
                    if (deviceType == InputDeviceType.Camera)
                        goto case PointerOffsetMode.vrCenter;
                    else if (deviceType == InputDeviceType.Leap)
                        goto case PointerOffsetMode.vrCenter;
                    else if (deviceType == InputDeviceType.Vive)
                        goto case PointerOffsetMode.vrCenter;
                    else if (deviceType == InputDeviceType.Mouse)
                        goto case PointerOffsetMode.mousePosition;
                    // else if (deviceType == null)
                    //     Debug.LogWarning("WorldspaceInputDevice.GetPointerScreenpointPosition: no uiPointerOffsetMode and no deviceType defined, unable to define raycast direction!");
                    break;
                case PointerOffsetMode.vrCenter:
                    if (InputModule.Instance.raycastCamera.targetTexture) {
                        pos = new Vector2(
                            (InputModule.Instance.raycastCamera.targetTexture.width * 0.5f),
                            (InputModule.Instance.raycastCamera.targetTexture.height * 0.5f));
                    }
                    else {
                        pos = new Vector2(UnityEngine.XR.XRSettings.eyeTextureWidth * 0.5f, UnityEngine.XR.XRSettings.eyeTextureHeight * 0.5f);
                    }
                    //pos = new Vector2(VRSettings.eyeTextureWidth * 0.5f, VRSettings.eyeTextureHeight * 0.5f);

                    //Debug.Log("WorldspaceInputDevice.GetPointerScreenpointPosition: using vrCenter: " +pos);
                    // http://answers.unity3d.com/questions/1227608/issues-with-eventsystem-raycastall-and-vr-in-54.html
                    break;
                case PointerOffsetMode.screenCenter:
                    if (InputModule.Instance.raycastCamera.targetTexture)
                    {
                        pos = new Vector2(
                            (InputModule.Instance.raycastCamera.targetTexture.width * 0.5f),
                            (InputModule.Instance.raycastCamera.targetTexture.height * 0.5f));
                    } else { 
                        pos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                    }
                    //Debug.Log("WorldspaceInputDevice.GetPointerScreenpointPosition: using screenCenter: " + pos);
                    break;
                case PointerOffsetMode.mousePosition:
                    pos = Input.mousePosition;
                    break;
            }
            return pos;
        }

        /// <summary>
        /// return the pointer viewport position in 0..1 coordinates
        /// </summary>
        /// <returns></returns>
        internal Vector2 GetPointerViewportPosition() {
            Vector2 pos = Vector2.zero;
            switch (uiPointerOffsetMode) {
                case PointerOffsetMode.undefined:
                    // could be moved to initialization
                    if (deviceType == InputDeviceType.Camera)
                        goto case PointerOffsetMode.vrCenter;
                    else if (deviceType == InputDeviceType.Leap)
                        goto case PointerOffsetMode.vrCenter;
                    else if (deviceType == InputDeviceType.Vive)
                        goto case PointerOffsetMode.vrCenter;
                    else if (deviceType == InputDeviceType.Mouse)
                        goto case PointerOffsetMode.mousePosition;
                    // else if (deviceType == null)
                    //     Debug.LogWarning("WorldspaceInputDevice.GetPointerViewportPosition: no uiPointerOffsetMode and no deviceType defined, unable to define raycast direction!");
                    break;
                case PointerOffsetMode.vrCenter:
                    pos = new Vector2(0.5f, 0.5f);
                    break;
                case PointerOffsetMode.screenCenter:
                    pos = new Vector2(0.5f, 0.5f); ;
                    break;
                case PointerOffsetMode.mousePosition:
                    if (InputModule.Instance.raycastCamera.targetTexture) {
                        pos = new Vector2(
                            (Input.mousePosition.x / InputModule.Instance.raycastCamera.targetTexture.width),
                            (Input.mousePosition.y / InputModule.Instance.raycastCamera.targetTexture.height));
                    }
                    else {
                        if (PlayerSettingsController.Instance.vrEnabled) {
                            Debug.Log("WorldspaceInputDevice.GetPointerViewportPosition: " + PlayerSettingsController.Instance.vrEnabled + " mouse " + Input.mousePosition + " eyeTex " + UnityEngine.XR.XRSettings.eyeTextureWidth + "/" + UnityEngine.XR.XRSettings.eyeTextureHeight);
                            //if (UnityEngine.VR.VRSettings.enabled) {
                            pos = new Vector2(
                                (Input.mousePosition.x / UnityEngine.XR.XRSettings.eyeTextureWidth),
                                (Input.mousePosition.y / UnityEngine.XR.XRSettings.eyeTextureHeight));
                        }
                        else {
                            Debug.Log("WorldspaceInputDevice.GetPointerViewportPosition: " + PlayerSettingsController.Instance.vrEnabled + " mouse " + Input.mousePosition + " screen " + Screen.width + "/" + Screen.height);
                            pos = new Vector2(
                                (Input.mousePosition.x / Screen.width),
                                (Input.mousePosition.y / Screen.height));
                        }
                    }
                    break;
            }
            return pos;
        }
        #endregion

        /// <summary>
        /// apply source position and direction to raycasting camera
        /// </summary>
        /// <param name="raycastCamera"></param>
        internal void UpdateCamera(Camera raycastCamera) {
            if ((controller == null) || (raycastCamera == null)) {
                Debug.LogError("WorldspaceInputDevice.UpdateCamera: missing source " + deviceType + " or raycastCamera " + raycastCamera + " hand: " + deviceHand + "! Disabling it!");
                this.deviceActive = false;
                return;
            }

            deviceData.deviceVelocity = (this.grabAttachementPoint.transform.position - prevPosition) / Time.deltaTime;
            deviceData.deviceAngularVelocity = (this.grabAttachementPoint.transform.rotation.eulerAngles - prevRotation) / Time.deltaTime;
            prevPosition = this.grabAttachementPoint.transform.position;
            prevRotation = this.grabAttachementPoint.transform.rotation.eulerAngles;

            Camera srcCam = controller.GetComponent<Camera>();
            Vector3 uiSourcePosition = uiRaySource.transform.position;
            float nearClipPlane = 0.01f; float farClipPlane = 100.0f;
            // move back ray source if activation is distance based, would make problems with gaze controllers, but it's not usual to use distance based gaze controlling
            if (uiActivationMethod == ActivationMethod.TouchDistance) {
                uiSourcePosition -= uiRaySource.transform.forward * minTouchDistance;
                nearClipPlane = minTouchDistance * 0.1f;
                farClipPlane = maxTouchDistance + minTouchDistance;
            }

            if (srcCam) {
                //if ((raycastCamera == null) || (raySource == null)) {
                //    Debug.LogError("WorldspaceInputDevice.UpdateCamera: raycastCamera " + raycastCamera + " raySource " + raySource);
                //}
                raycastCamera.transform.position = uiRaySource.transform.position; // TODO: Reto maybe needs to be controller.transform.position here
                raycastCamera.transform.rotation = uiRaySource.transform.rotation;
                //raycastCamera.clearFlags = srcCam.clearFlags;
                //raycastCamera.cullingMask = srcCam.cullingMask;
                raycastCamera.clearFlags = CameraClearFlags.Nothing;
                raycastCamera.cullingMask = 0;
                raycastCamera.fieldOfView = srcCam.fieldOfView;
                raycastCamera.nearClipPlane = srcCam.nearClipPlane;
                raycastCamera.farClipPlane = srcCam.farClipPlane;
            } else {
                raycastCamera.transform.position = uiRaySource.transform.position;
                raycastCamera.transform.rotation = uiRaySource.transform.rotation;
                raycastCamera.clearFlags = CameraClearFlags.Nothing;
                raycastCamera.cullingMask = 0;
                //Debug.Log("raycastCamera " + raycastCamera.name);
                raycastCamera.fieldOfView = 5;
                raycastCamera.nearClipPlane = nearClipPlane;
                raycastCamera.farClipPlane = farClipPlane;
            }
        }

        void RayColors(Vector3 source, Vector3 direction, float currentLength, float fullGradientLength, Gradient gradient, out Vector3[] positions, out Gradient newGradient) {
            List<GradientColorKey> newColorKeys = new List<GradientColorKey>();
            List<GradientAlphaKey> newAlphaKeys = new List<GradientAlphaKey>();
            List<float> timings = new List<float>();
            foreach (GradientColorKey colorKey in gradient.colorKeys) {
                timings.Add(colorKey.time);
            }
            foreach (GradientAlphaKey alphaKey in gradient.alphaKeys) {
                timings.Add(alphaKey.time);
            }
            timings = timings.OrderBy(x => x).ToList<float>();


            float rayLengthRatio = (currentLength / fullGradientLength);

            List<Vector3> positionsList = new List<Vector3>();
            for (int i = 0; i < timings.Count; i++) {
                // color and alpha key timing (0..1) on the original gradient over the full length of the colorGradient/ray
                float origGradientTiming = timings[i];
                // 
                //float realDistance = origGradientTiming / fullGradientLength;
                float newGradientTiming = origGradientTiming / rayLengthRatio;

                Color col = gradient.Evaluate(origGradientTiming);

                // add intermediate points
                if (origGradientTiming <= rayLengthRatio) {
                    positionsList.Add(source + direction * origGradientTiming * currentLength);
                    newColorKeys.Add(new GradientColorKey(col, newGradientTiming));
                    newAlphaKeys.Add(new GradientAlphaKey(col.a, newGradientTiming));
                }
                // add last point and finish list generation
                else {
                    positionsList.Add(source + direction * currentLength);
                    newColorKeys.Add(new GradientColorKey(col, newGradientTiming));
                    newAlphaKeys.Add(new GradientAlphaKey(col.a, newGradientTiming));
                    break;
                }
            }
            //for (int i = 0; i < uiRayColors.colorKeys.Length; i++) {
            //    GradientColorKey colorKey = uiRayColors.colorKeys[i];
            //    positionsList.Add();
            //    //Vector3.Lerp(deviceData.source, deviceData.source + deviceData.direction * deviceData.uiHitDistance, colorKey.time * rayLengthRatio);
            //}

            positions = positionsList.ToArray();
            newGradient = new Gradient();
            newGradient.alphaKeys = newAlphaKeys.Take(positions.Length).ToArray();
            newGradient.colorKeys = newColorKeys.Take(positions.Length).ToArray();
        }

        #region VISUALS
        /// <summary>
        /// update the reticle position of ths input
        /// </summary>
        internal void UpdateVisuals() {
            #region ray / object - enable / disable
            bool reticleEnabled = deviceActive && uiCastActive;
            bool uiRayEnabled = false;
            switch (showUiRaySetting) {
                case UiCastVisualizeSetting.Never:
                    uiRayEnabled = false;
                    break;
                case UiCastVisualizeSetting.UiHit:
                    uiRayEnabled = uiCastActive && deviceData.uiHit;
                    break;
                case UiCastVisualizeSetting.Allways:
                    uiRayEnabled = uiCastActive;
                    break;
            }

            // is the physics cast ray visible
            bool physicsRayEnabled = false;
            switch (showPhysicsRaySetting) {
                case PhysicsCastVisualizeSetting.Allways:
                    physicsRayEnabled = physicsCastActive && ((physicsCastType == PhysicsCastType.ray) || (physicsCastType == PhysicsCastType.parabola));
                    break;
                case PhysicsCastVisualizeSetting.Never:
                    physicsRayEnabled = false;
                    break;
            }

            // is the sphere visible
            bool sphereEnabled = false;
            switch (showPhysicsRaySetting) {
                case PhysicsCastVisualizeSetting.Allways:
                    sphereEnabled = physicsCastActive && (physicsCastType == PhysicsCastType.sphere);
                    //sphereEnabled = sphereEnabled && (deviceData.hoveredInteractableObjects != null);
                    break;
                case PhysicsCastVisualizeSetting.Never:
                    sphereEnabled = false;
                    break;
            }

            //Debug.Log("WorldspaceInputDevice.UpdateVisuals: " + this + " " + reticleEnabled + " " + uiRayEnabled + " " + physicsRayEnabled + " " + sphereEnabled);

            //bool reticleEnabled, bool lineEnabled, bool sphereEnabled) {
            //Debug.Log("WorldspaceInputDevice.UpdateReticlePosition: type: " + deviceType + " hand: " + deviceHand + " " + isActive + " " + reticle.enabled + " " + line.enabled); // + deviceData.source +" " + deviceData.direction +" " + deviceData.uiHitDistance);
            reticle.enabled = _deviceActive && showReticle && deviceData.uiHit;

            reticle.enabled = reticleEnabled && deviceData.uiHit;
            if (reticle.enabled) {
                reticle.transform.position = deviceData.source + deviceData.uiHitDistance * deviceData.direction;
                reticle.transform.rotation = deviceData.uiHitRotation; // Quaternion.LookRotation(deviceData.uiHitNormal);

                float reticleScaling = (((Mathf.Sqrt(deviceData.uiHitDistance) / ((uiRayLength != 0) ? uiRayLength : 1))) * reticleSize); // (maxReticleSize - minReticleSize) + minReticleSize);
                reticle.transform.localScale = Vector3.one * reticleScaling;
                //reticle.transform.LookAt(reticle.transform.position + deviceData.uiHitNormal); //raySource
            }
            #endregion

            #region physics ray
            _physicsRay.enabled = physicsRayEnabled;
            if (_physicsRay.enabled) {
                switch (physicsCastType) {
                    case PhysicsCastType.disabled:
                        break;
                    case PhysicsCastType.sphere:
                        break;
                    case PhysicsCastType.parabola:
                        _physicsRay.positionCount = parabolaPositions.Length;
                        // parabolaPositions is calculated in WorldspaceInputModule.ParabolacastPhysicsScene
                        physicsRay.SetPositions(parabolaPositions);
                        break;
                    case PhysicsCastType.ray:
                        Vector3[] positions = null;
                        Gradient gradient = null;
                        RayColors(deviceData.source, deviceData.direction, (deviceData.source - deviceData.worldspaceHit).magnitude, physicsRayLength, physicRayColors, out positions, out gradient);

                        physicsRay.positionCount = positions.Length;
                        physicsRay.SetPositions(positions);
                        physicsRay.colorGradient = gradient;

                        //Vector3[] positions = new Vector3[physicRayColors.colorKeys.Length];
                        ////Debug.Log("WorldspaceInputDevice.UpdateReticlePosition: " + deviceData.source + " " + deviceData.uiHitDistance + " " + deviceData.direction);
                        //// when we hit an object (not full length)
                        //if (deviceData.hoveredInteractableObjects.Count > 0) {
                        //    // how long is the ray 0..1 in comparison to the usual full length when nothing is hit
                        //    float rayLengthRatio = ((deviceData.source - deviceData.worldspaceHit).magnitude / physicsRayLength);
                        //    for (int i = 0; i < physicRayColors.colorKeys.Length; i++) {
                        //        GradientColorKey colorKey = physicRayColors.colorKeys[i];
                        //        positions[i] = Vector3.Lerp(deviceData.source, deviceData.worldspaceHit, colorKey.time * rayLengthRatio);
                        //    }
                        //    _physicsRay.numPositions = positions.Length;
                        //    _physicsRay.SetPositions(positions);
                        //}
                        //// when we show the full length
                        //else {
                        //    for (int i = 0; i < physicRayColors.colorKeys.Length; i++) {
                        //        GradientColorKey colorKey = physicRayColors.colorKeys[i];
                        //        positions[i] = Vector3.Lerp(deviceData.source, deviceData.source + deviceData.direction * physicsRayLength, colorKey.time);
                        //    }
                        //    _physicsRay.numPositions = positions.Length;
                        //    _physicsRay.SetPositions(positions); // new Vector3[] { deviceData.source, deviceData.source + deviceData.direction * physicsRayLength });
                        //}
                        break;
                }
            } else {
                // hide line
                _physicsRay.positionCount = 0;
            }
            #endregion

            #region ui ray
            _uiRay.enabled = uiRayEnabled;
            if (_uiRay.enabled) {
                Vector3[] positions = null;
                Gradient gradient = null;
                RayColors(deviceData.source, deviceData.direction, deviceData.uiHitDistance, uiRayLength, uiRayColors, out positions, out gradient);

                _uiRay.positionCount = positions.Length;
                _uiRay.SetPositions(positions);
                _uiRay.colorGradient = gradient;

                //Vector3[] positions = new Vector3[uiRayColors.colorKeys.Length];
                //if (deviceData.hoveredInteractableObjects.Count > 0) {
                //    // how long is the ray 0..1 in comparison to the usual full length when nothing is hit
                //    float rayLengthRatio = (deviceData.uiHitDistance / uiRayLength);
                //    for (int i = 0; i < uiRayColors.colorKeys.Length; i++) {
                //        GradientColorKey colorKey = uiRayColors.colorKeys[i];
                //        positions[i] = Vector3.Lerp(deviceData.source, deviceData.source + deviceData.direction * deviceData.uiHitDistance, colorKey.time * rayLengthRatio);
                //    }
                //    _uiRay.numPositions = positions.Length;
                //    _uiRay.SetPositions(positions);
                //}
                //// when we show the full length
                //else {
                //    for (int i = 0; i < uiRayColors.colorKeys.Length; i++) {
                //        GradientColorKey colorKey = uiRayColors.colorKeys[i];
                //        positions[i] = Vector3.Lerp(deviceData.source, deviceData.source + deviceData.direction * uiRayLength, colorKey.time);
                //    }
                //    _uiRay.numPositions = positions.Length;
                //    _uiRay.SetPositions(positions); // new Vector3[] { deviceData.source, deviceData.source + deviceData.direction * physicsRayLength });
                //}

                //Vector3[] positions = new Vector3[uiRayColors.colorKeys.Length];
                //for (int i = 0; i < uiRayColors.colorKeys.Length; i++) {
                //    GradientColorKey colorKey = uiRayColors.colorKeys[i];
                //    positions[i] = Vector3.Lerp(deviceData.source, deviceData.source + deviceData.direction * uiRayLength, colorKey.time);
                //}
                //_uiRay.numPositions = positions.Length;
                //_uiRay.SetPositions(positions); // new Vector3[] { deviceData.source, deviceData.source + deviceData.uiHitDistance * deviceData.direction });

                //switch (physicsCastType) {
                //    case PhysicsCastType.disabled:
                //        break;
                //    case PhysicsCastType.sphere:
                //        break;
                //    case PhysicsCastType.parabola:
                //        _uiRay.numPositions = parabolaPositions.Length;
                //        // parabolaPositions is calculated in WorldspaceInputModule.ParabolacastPhysicsScene
                //        physicsRay.SetPositions(parabolaPositions);
                //        break;
                //    case PhysicsCastType.ray:
                //        _uiRay.numPositions = 2;
                //        //Debug.Log("WorldspaceInputDevice.UpdateReticlePosition: " + deviceData.source + " " + deviceData.uiHitDistance + " " + deviceData.direction);
                //        _uiRay.SetPositions(new Vector3[] { deviceData.source, deviceData.source + deviceData.uiHitDistance * deviceData.direction });
                //        break;
                //}
            } else {
                // hide line
                _uiRay.positionCount = 0;
            }
            #endregion

            #region physics sphere
            sphere.SetActive(sphereEnabled);
            if (sphere.activeSelf) {
                sphere.transform.localScale = Vector3.one * sphereCastRadius;
                sphere.transform.position = grabAttachementPoint.transform.position;
                sphere.transform.rotation = grabAttachementPoint.transform.rotation;
                sphere.SetActive(true);
            } else {
                sphere.SetActive(false);
            }
            #endregion
        }
        #endregion

        #region INPUT
        /// <summary>
        /// update input axises to activate a ui press
        /// </summary>
        /// <param name="pressed"></param>
        /// <param name="released"></param>
        internal void UpdateInputAxis(ref bool pressed, ref bool released) {
            if (uiActivationMethod == ActivationMethod.InputManagerAxis) {
                // Input.GetJoystickNames()[0]
                pressed = Input.GetButtonDown(uiActivationAxis);
                released = Input.GetButtonUp(uiActivationAxis);
            }
            if (uiActivationMethod == ActivationMethod.ExternalInput) {
                pressed = externalPressed;
                externalPressed = false;
                released = externalReleased;
                externalReleased = false;
            }
        }

        /// <summary>
        /// update the countdown of this input if hoverActivationTime is > 0 (gaze controller)
        /// </summary>
        /// <param name="pointerData"></param>
        /// <param name="reticleImage"></param>
        /// <param name="pressed"></param>
        /// <param name="released"></param>
        internal void UpdateReticleCountdown(PointerEventData3D pointerData, ref bool pressed, ref bool released) {
            if ((uiActivationMethod == ActivationMethod.HoverTimer) && (hoverActivationTime > 0)) {
                GameObject currentRayhitGo = pointerData.pointerCurrentRaycast.gameObject;
                GameObject currentSubmittableRayhitGo = ExecuteEvents.GetEventHandler<ISubmitHandler>(currentRayhitGo); // find pressable object (submittable)
                if (currentSubmittableRayhitGo == null) {
                    currentSubmittableRayhitGo = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentRayhitGo);
                }
                if (currentSubmittableRayhitGo == null) {
                    currentSubmittableRayhitGo = ExecuteEvents.GetEventHandler<IDragHandler>(currentRayhitGo);
                }
                // GameObject currentDraggableRayhitGo = ExecuteEvents.GetEventHandler<IDragHandler>(currentRayhitGo);

                // fill gaze counter
                float gaze = Mathf.Clamp01((Time.time - hoverActivationTimer) / hoverActivationTime);
                reticle.fillAmount = gaze;

                // if we stay on the same pressable object
                if (currentSubmittableRayhitGo == prevSubmittableRayhitObject) {
                    // if gaze activation timer reached & not already pressed
                    if ((gaze >= 1) && (prevPressObject != currentSubmittableRayhitGo)) {
                        pressed = true;
                        released = true;
                        prevPressObject = currentSubmittableRayhitGo;
                    }
                }
                // reset timer if the object we hover is changed
                else {
                    prevPressObject = null;
                    prevSubmittableRayhitObject = currentSubmittableRayhitGo;
                    hoverActivationTimer = Time.time;
                }

                // if it's submittable or draggable keep large size, else make small
                if ((currentSubmittableRayhitGo != null) || ExecuteEvents.GetEventHandler<IDragHandler>(currentRayhitGo)) {
                    //reticle.transform.localScale = Vector3.one * maxReticleSize;
                    reticle.material.color = reticleActiveColor;
                } else {
                    reticle.fillAmount = 1;
                    //reticle.transform.localScale = Vector3.one * minReticleSize;
                    reticle.material.color = reticleInactiveColor;
                }
            }
        }

        /// <summary>
        /// Create events for distance based activation (leap)
        /// </summary>
        /// <param name="pointerEvent"></param>
        /// <param name="pressed"></param>
        /// <param name="released"></param>
        internal void UpdateReticleTouching(PointerEventData3D pointerEvent, ref bool pressed, ref bool released) {
            if ((uiActivationMethod == ActivationMethod.TouchDistance)) {
                Camera cam = pointerEvent.enterEventCamera;
                if (cam != null) {
                    GameObject currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

                    //GameObject currentSubmittableRayhitGo = ExecuteEvents.GetEventHandler<ISubmitHandler>(currentOverGo);
                    //if (currentSubmittableRayhitGo == null) {
                    //    currentSubmittableRayhitGo = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);
                    //}
                    //GameObject currentDraggableRayhitGo = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

                    GameObject hitObject = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.pointerDownHandler);
                    if (hitObject == null) {
                        hitObject = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);
                    }


                    // Vector2 pointerPosition = GetPointerViewportPosition();
                    float intersectionDistance = pointerEvent.pointerCurrentRaycast.distance + cam.nearClipPlane;
                    // Vector3 intersectionPosition = cam.transform.position + cam.ViewportPointToRay((Vector3)pointerPosition).direction * intersectionDistance;

                    //Debug.Log("WorldspaceInputDevice.UpdateReticleTouching: " + this + " hover lastpress " + (newHover == pointerEvent.lastPress) +", hover lastEnter "+ (newHover == pointerEvent.pointerEnter));

                    // we hit an object
                    if (hitObject != null) {
                        // if a prev pressed object is defined and it matches the currently hovered object we are still pressing
                        if (hitObject == prevPressObject) {
                            if (intersectionDistance > maxTouchDistance) {
                                //Debug.Log("WorldspaceInputDevice.UpdateReticleTouching: " + this + " releasing pressed");
                                // release
                                released = true;
                                prevPressObject = null;
                            } else {
                                //Debug.Log("WorldspaceInputDevice.UpdateReticleTouching: " + this + " keeping pressed");
                                // keep pressed while over last pressed object
                                pressed = true;
                            }
                        } else {
                            if (intersectionDistance < minTouchDistance) {
                                //Debug.Log("WorldspaceInputDevice.UpdateReticleTouching:  " + this + " starting press");
                                pressed = true;
                                prevPressObject = hitObject;
                            }
                        }
                    }
                    // we did not hit an object
                    else {
                        // we lost contact to object
                        if (prevPressObject != null) {
                            //Debug.Log("WorldspaceInputDevice.UpdateReticleTouching: " + this + " press lost");
                            released = true;
                            prevPressObject = null;
                        }
                    }
                    // if a prev pressed object is defined and it matches the currently hovered object we are still pressing
                    //if (hitObject == prevPressObject) {
                    //    if (intersectionDistance > maxTouchDistance) { //&& (prevIntersectionDistance <= maxTouchDistance)) {
                    //        Debug.Log("WorldspaceInputDevice.UpdateReticleTouching: " + this + " releasing pressed");
                    //        //Debug.Log("WorldspaceInputDevice.UpdateReticleTouching: release");
                    //        // release
                    //        released = true;
                    //        prevPressObject = null;
                    //    } else {
                    //        Debug.Log("WorldspaceInputDevice.UpdateReticleTouching: " + this + " keeping pressed");
                    //        // keep pressed while over last pressed object
                    //        pressed = true;
                    //    }
                    //} else if (prevPressObject != null) {
                    //    // 
                    //}
                    //else if (hitObject == prevHoverObject) {
                    //    //if (prevHoverObject == newPressed) {
                    //    if (intersectionDistance < minTouchDistance) { // && (prevIntersectionDistance >= minTouchDistance)) {
                    //        Debug.Log("WorldspaceInputDevice.UpdateReticleTouching:  " + this + " starting press");
                    //        //Debug.Log("WorldspaceInputDevice.UpdateReticleTouching: press");
                    //        // press
                    //        pressed = true;
                    //        prevPressObject = hitObject;
                    //        //prevPressObject = newHover;
                    //    } else {
                    //        // hover start event
                    //        prevHoverObject = hitObject;
                    //    }
                    //    //else if ((intersectionDistance > maxTouchDistance) && (prevIntersectionDistance <= maxTouchDistance)) {
                    //    //    //Debug.Log("WorldspaceInputDevice.UpdateReticleTouching: release");
                    //    //    // release
                    //    //    released = true;
                    //    //    //prevPressObject = null;
                    //    //} else {
                    //    //    // same state
                    //    //    //Debug.Log("WorldspaceInputDevice.UpdateReticleTouching: unknown");
                    //    //}
                    //}
                    //else if (hitObject != null) {
                    //    // hover start event
                    //    prevHoverObject = hitObject;

                    //    Debug.Log("WorldspaceInputDevice.UpdateReticleTouching:  " + this + " object changed");

                    //    //released = true;
                    //    //prevPressObject = null;
                    //} else {
                    //}
                    /*else  {
                        Debug.Log("WorldspaceInputDevice.UpdateReticleTouching: switched");
                        prevHoverObject = newHover;
                        // testing
                        released = true;
                        prevPressObject = null;
                    }*/
                    // prevIntersectionDistance = intersectionDistance;

                    // update reticle
                    float reticleFill = Mathf.Clamp01(1 - ((intersectionDistance - minTouchDistance) / (maxTouchDistance - minTouchDistance)));
                    reticle.fillAmount = reticleFill;

                    // if it's submittable or draggable keep large size, else make small
                    if ((ExecuteEvents.GetEventHandler<ISubmitHandler>(currentOverGo) != null) || ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo)) {
                        //reticle.transform.localScale = Vector3.one * maxReticleSize;
                        reticle.material.color = reticleActiveColor;
                    } else {
                        reticle.fillAmount = 1;
                        //reticle.transform.localScale = Vector3.one * minReticleSize;
                        reticle.material.color = reticleInactiveColor;
                    }
                }
            }
        }
        #endregion

        #region INTERACTIVE OBJECT TOUCH & ATTACH
        /// <summary>
        /// attach a object to the controller, send grab-enter event to object
        /// index is which element from touched list to grab
        /// </summary>
        internal void AttachObject (int index) {
            Debug.Log("WorldspaceInputDevice.AttachObject: attaching object with index: "+index);
            if ((index >= 0) && (index < deviceData.hoveredInteractableObjects.Count)) {
                deviceData.grabbedInteractableObject = deviceData.hoveredInteractableObjects[index];
                deviceData.grabbedInteractableObject.OnDragEnter(this.deviceData);
            } else {
                Debug.LogError("WorldspaceInputDevice.AttachObject: index out of range of touched objects");
            }
        }

        internal void AttachObject (InteractableObject interactableObject) {
            if (deviceData.hoveredInteractableObjects.Contains(interactableObject)) {
                Debug.Log("WorldspaceInputDevice.AttachObject: attaching object: " + interactableObject);
                deviceData.grabbedInteractableObject = interactableObject;
                deviceData.grabbedInteractableObject.OnDragEnter(this.deviceData);
            } else {
                Debug.LogError("WorldspaceInputDevice.AttachObject: object not in touchedInteractableObjects: " + interactableObject);
            }
        }

        /// <summary>
        /// detach an object from the controller, send grab-exit event to object
        /// </summary>
        internal void DetachObject() {
            if (deviceData.grabbedInteractableObject) {
                deviceData.grabbedInteractableObject.OnDragExit(this.deviceData);
                deviceData.grabbedInteractableObject = null;
            }
            else if (deviceData.grabbedInteractableObject == null) { }
            else {
                Debug.LogError("WorldspaceInputDevice.DetachObject: requested to drop object without one grabbed");
            }
        }

        internal void DetachObject(InteractableObject interactableObject) {
            if (interactableObject == null)
                return;
            if (deviceData.grabbedInteractableObject == null) {
                Debug.LogError("WorldspaceInputDevice.DetachObject:no object attached to drop " + interactableObject);
                return;
            }
            if (deviceData.grabbedInteractableObject == interactableObject) {
                deviceData.grabbedInteractableObject.OnDragExit(this.deviceData);
                deviceData.grabbedInteractableObject = null;
            } else {
                Debug.LogError("WorldspaceInputDevice.DetachObject: requested to drop unattached object "+ interactableObject);
            }
        }

        /// <summary>
        /// drag the object around, send grab-repeating event to object
        /// </summary>
        internal void DragObjectProcessUpdate() {
            //Debug.Log("WorldspaceInputDevice.DragObject");
            if (deviceData.grabbedInteractableObject) {
                deviceData.grabbedInteractableObject.OnDragRepeat(this.deviceData);
                //deviceData.grabbedInteractableObject.ApplyTransformation(this.deviceData);
            }
        }
        /// <summary>
        /// drag the object around, apply the transformation
        /// </summary>
        internal void DragObjectUpdate() {
            //Debug.Log("WorldspaceInputDevice.DragObject");
            if (deviceData.grabbedInteractableObject) {
                //deviceData.grabbedInteractableObject.OnDragRepeat(this.deviceData);
                deviceData.grabbedInteractableObject.ApplyTransformation(this.deviceData);
            }
        }
        #endregion

        #region DEBUG
        /// <summary>
        /// used to give meaningful string to be used in debugging
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return "device: " + deviceType.ToString() + "-" + deviceHand.ToString() + (_deviceActive?" (active)":" (inactive)") + (uiCastActive ? " (uiCast active)" : " (uiCast inactive)") + (physicsCastActive ? " (physicsCast active)" : " (physicsCast inactive)");
        }
        #endregion
    }
}
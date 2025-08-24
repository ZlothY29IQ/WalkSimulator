using Cinemachine;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

namespace WalkSimulator.Rigging
{

    public class HeadDriver : MonoBehaviour
    {
        public static HeadDriver Instance;

        public Transform thirpyTarget;
        public Transform head;
        public GameObject cameraObject;
        public GameObject cameraTransform;

        private Vector3 offset = Vector3.zero;
        private Camera overrideCam;

        public bool turn = true;
        private bool _lockCursor;

        public bool LockCursor
        {
            get => _lockCursor;
            set
            {
                _lockCursor = value;
                if (_lockCursor)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
        }

        public bool FirstPerson
        {
            get => overrideCam.enabled;
            set => overrideCam.enabled = value;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            Cinemachine3rdPersonFollow val = Object.FindObjectOfType<Cinemachine3rdPersonFollow>();
            thirpyTarget = val.VirtualCamera.Follow;

            Camera componentInParent = val.gameObject.GetComponentInParent<Camera>();
            cameraObject = new GameObject("WalkSim First Person Camera");
            overrideCam = cameraObject.AddComponent<Camera>();

            overrideCam.fieldOfView = 90f;
            overrideCam.nearClipPlane = componentInParent.nearClipPlane;
            overrideCam.farClipPlane = componentInParent.farClipPlane;
            overrideCam.targetDisplay = componentInParent.targetDisplay;
            overrideCam.cullingMask = componentInParent.cullingMask;
            overrideCam.depth = componentInParent.depth + 1f;
            overrideCam.enabled = false;
        }

        private void LateUpdate()
        {
            cameraObject.transform.position = GTPlayer.Instance.headCollider.transform.TransformPoint(offset);
            cameraObject.transform.forward = head.forward;

            if (!turn) return;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            GTPlayer.Instance.Turn(mouseDelta.x / 10f);

            Vector3 eulerAngles = GorillaTagger.Instance.offlineVRRig.headConstraint.eulerAngles;
            eulerAngles.x -= mouseDelta.y / 10f;

            if (eulerAngles.x > 180f)
                eulerAngles.x -= 360f;

            eulerAngles.x = Mathf.Clamp(eulerAngles.x, -60f, 60f);
            GorillaTagger.Instance.offlineVRRig.headConstraint.eulerAngles = eulerAngles;

            thirpyTarget.localEulerAngles = new Vector3(eulerAngles.x, 0f, 0f);
            GTPlayer.Instance.headCollider.transform.localEulerAngles = new Vector3(eulerAngles.x, 0f, 0f);
        }

        private void OverrideHeadMovement()
        {
            Logging.Debug("Overriding head movement");
            head = GorillaTagger.Instance.offlineVRRig.head.rigTarget;
        }

        internal void ToggleCam()
        {
            overrideCam.enabled = !overrideCam.enabled;
        }

        private void OnEnable()
        {
            Logging.Debug("Enabled");
            if (Rig.Instance.Animator != null)
            {
                LockCursor = true;
                OverrideHeadMovement();
            }
        }

        private void OnDisable()
        {
            if (head != null)
            {
                LockCursor = false;
                GorillaTagger.Instance.offlineVRRig.head.rigTarget = head;
            }
        }
    }
}

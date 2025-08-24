using GorillaLocomotion;
using UnityEngine;
using WalkSimulator.Animators;

namespace WalkSimulator.Rigging
{

    public class Rig : MonoBehaviour
    {
        public Transform head;
        public Transform body;
        public HeadDriver headDriver;
        public HandDriver leftHand;
        public HandDriver rightHand;
        public Rigidbody rigidbody;

        public Vector3 targetPosition;
        public Vector3 lastNormal;
        public Vector3 lastGroundPosition;
        public bool onGround;
        public bool active;
        public bool useGravity = true;

        private float scale = 1f;
        private float speed = 4f;
        private float raycastLength = 1.3f;
        private float raycastRadius = 0.3f;
        private Vector3 raycastOffset = new Vector3(0f, 0.4f, 0f);

        private AnimatorBase _animator;

        public static Rig Instance { get; private set; }
        public Vector3 smoothedGroundPosition { get; set; }

        public AnimatorBase Animator
        {
            get => _animator;
            set
            {
                if (_animator != null && value != _animator)
                    _animator.Cleanup();

                _animator = value;

                if (_animator != null)
                {
                    ((Behaviour)_animator).enabled = true;
                    _animator.Setup();
                }

                ((Behaviour)leftHand).enabled = _animator != null;
                ((Behaviour)rightHand).enabled = _animator != null;
                ((Behaviour)headDriver).enabled = _animator != null;
            }
        }

        private void Awake()
        {
            Instance = this;

            head = GTPlayer.Instance.headCollider.transform;
            body = GTPlayer.Instance.bodyCollider.transform;
            rigidbody = GTPlayer.Instance.bodyCollider.attachedRigidbody;

            leftHand = new GameObject("WalkSim Left Hand Driver").AddComponent<HandDriver>();
            leftHand.Init(isLeft: true);
            leftHand.enabled = false;

            rightHand = new GameObject("WalkSim Right Hand Driver").AddComponent<HandDriver>();
            rightHand.Init(isLeft: false);
            rightHand.enabled = false;

            headDriver = new GameObject("WalkSim Head Driver").AddComponent<HeadDriver>();
            headDriver.enabled = false;
        }

        private void FixedUpdate()
        {
            scale = GTPlayer.Instance.scale;
            smoothedGroundPosition = Vector3.Lerp(smoothedGroundPosition, lastGroundPosition, 0.2f);

            OnGroundRaycast();

            Animator?.Animate();

            Move();
        }

        private void Move()
        {
            if (!active) return;

            rigidbody.velocity = Vector3.Lerp(rigidbody.velocity, (targetPosition - body.position) * speed, 1f);

            if (!useGravity)
                rigidbody.AddForce(-Physics.gravity * rigidbody.mass * scale);
        }

        private void OnGroundRaycast()
        {
            float rayLength = raycastLength * scale;
            Vector3 origin = body.TransformPoint(raycastOffset);
            float sphereRadius = raycastRadius * scale;

            RaycastHit hitRay;
            bool rayHit = Physics.Raycast(origin, Vector3.down, out hitRay, rayLength, GTPlayer.Instance.locomotionEnabledLayers);

            RaycastHit hitSphere;
            bool sphereHit = Physics.SphereCast(origin, sphereRadius, Vector3.down, out hitSphere, rayLength, GTPlayer.Instance.locomotionEnabledLayers);

            RaycastHit closestHit;

            if (rayHit && sphereHit)
                closestHit = hitRay.distance <= hitSphere.distance ? hitRay : hitSphere;
            else if (sphereHit)
                closestHit = hitSphere;
            else if (rayHit)
                closestHit = hitRay;
            else
            {
                onGround = false;
                return;
            }

            lastNormal = closestHit.normal;
            onGround = true;

            lastGroundPosition = closestHit.point;
            lastGroundPosition.x = body.position.x;
            lastGroundPosition.z = body.position.z;
        }
    }
}

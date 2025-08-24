using System.Collections;
using GorillaLocomotion;
using GorillaNetworking;
using UnityEngine;
using UnityEngine.InputSystem;
using WalkSimulator.Rigging;

namespace WalkSimulator.Animators
{
    public class InteractAnimator : AnimatorBase
    {
        public enum State
        {
            IDLE,
            WAIT,
            BUTTON,
            GRAB
        }

        private Transform reticle;
        private State state;

        public override void Animate()
        {
            reticle.gameObject.SetActive(true);
            HeadDriver.Instance.FirstPerson = true;
            AnimateBody();
            AnimateHands();
        }

        private void Update()
        {
            if (state == State.IDLE)
            {
                if (Mouse.current.leftButton.isPressed)
                {
                    state = State.WAIT;
                    StartCoroutine(Raycast(leftHand));
                }
                else if (Mouse.current.rightButton.isPressed)
                {
                    state = State.WAIT;
                    StartCoroutine(Raycast(rightHand));
                }
            }
        }

        private void AnimateBody()
        {
            rig.active = true;
            rig.useGravity = false;
            rig.targetPosition = body.position;
        }

        private IEnumerator Raycast(HandDriver hand)
        {
            Ray ray = new Ray(Camera.main.transform.position, reticle.position - Camera.main.transform.position);

            int buttonLayer = LayerMask.GetMask(new string[2]
            {
                "GorillaInteractable",
                "TransparentFX"
            });

            int layerMask = buttonLayer | GTPlayer.Instance.locomotionEnabledLayers;

            if (Physics.Raycast(ray, out RaycastHit hit, 0.82f, layerMask))
                yield return PressButton(hand, hit.point - Camera.main.transform.forward * 0.05f);

            state = State.IDLE;
        }


        private IEnumerator PressButton(HandDriver hand, Vector3 targetPosition)
        {
            state = State.BUTTON;

            hand.grip = true;
            hand.targetPosition = reticle.position;
            hand.lookAt = targetPosition;
            hand.up = hand.isLeft ? head.right : -head.right;

            yield return new WaitForSeconds(0.1f);

            hand.targetPosition = targetPosition;

            while (Vector3.Distance(hand.transform.position, targetPosition) > 0.05f)
            {
                yield return new WaitForFixedUpdate();
            }

            hand.targetPosition = reticle.position + Camera.main.transform.forward * 0.05f;
            yield return new WaitForSeconds(0.1f);

            hand.targetPosition = hand.DefaultPosition;
            state = State.IDLE;
        }

        private void AnimateHands()
        {
            if (state == State.IDLE)
            {
                leftHand.targetPosition = leftHand.DefaultPosition;
                rightHand.targetPosition = rightHand.DefaultPosition;

                leftHand.lookAt = leftHand.targetPosition + head.forward;
                rightHand.lookAt = rightHand.targetPosition + head.forward;

                leftHand.up = head.right;
                rightHand.up = -head.right;
            }
        }

        protected override void Start()
        {
            base.Start();

            // Create a tiny reticle
            reticle = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            reticle.localScale = Vector3.one * 0.001f;

            var renderer = reticle.GetComponent<MeshRenderer>();
            renderer.material.color = Color.white;
            renderer.material.shader = Shader.Find("GorillaTag/UberShader");

            reticle.GetComponent<Collider>().enabled = false;

            reticle.SetParent(Camera.main.transform);
            reticle.localPosition = Vector3.forward * 0.1f;
            reticle.gameObject.SetActive(false);
        }

        public override void Setup()
        {
            HeadDriver.Instance.LockCursor = true;
        }

        public override void Cleanup()
        {
            base.Cleanup();
            reticle.gameObject.SetActive(false);
            state = State.IDLE;
            StopAllCoroutines();
        }

        private void OnDestroy()
        {
            Destroy(reticle.gameObject);
        }
    }
}

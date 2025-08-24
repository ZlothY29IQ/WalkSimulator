using GorillaLocomotion;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using WalkSimulator.Rigging;
using WalkSimulator.Tools;

namespace WalkSimulator.Animators
{
    public class FlyAnimator : AnimatorBase
    {
        private float speed = 1f;
        private float minSpeed = 0f;
        private float maxSpeed = 5f;

        private int layersBackup;
        private bool noClipActive = false;

        private void Awake()
        {
            layersBackup = GTPlayer.Instance.locomotionEnabledLayers;
        }

        public override void Animate()
        {
            AnimateBody();
            AnimateHands();
        }

        private void Update()
        {
            speed += Mouse.current.scroll.ReadValue().y / 1000f;
            speed = Mathf.Clamp(speed, minSpeed, maxSpeed);

            if (Keyboard.current.nKey.wasPressedThisFrame)
            {
                noClipActive = !noClipActive;

                GTPlayer.Instance.locomotionEnabledLayers = noClipActive ? 536870912 : layersBackup;
                GTPlayer.Instance.headCollider.isTrigger = noClipActive;
                GTPlayer.Instance.bodyCollider.isTrigger = noClipActive;
            }
        }

        private void AnimateBody()
        {
            rig.active = true;
            rig.useGravity = false;
            rig.targetPosition = body.TransformPoint(InputHandler.inputDirection * speed);
        }

        private void AnimateHands()
        {
            float handFollow = WalkSimulator.Tools.Extensions.Map(speed, minSpeed, maxSpeed, 0f, 1f);

            leftHand.followRate = handFollow;
            rightHand.followRate = handFollow;

            leftHand.targetPosition = leftHand.DefaultPosition;
            rightHand.targetPosition = rightHand.DefaultPosition;

            leftHand.lookAt = leftHand.targetPosition + body.forward;
            rightHand.lookAt = rightHand.targetPosition + body.forward;

            leftHand.up = body.right;
            rightHand.up = -body.right;
        }

        public override void Cleanup()
        {
            base.Cleanup();

            leftHand.followRate = 0.1f;
            rightHand.followRate = 0.1f;

            GTPlayer.Instance.locomotionEnabledLayers = layersBackup;
        }

        public override void Setup()
        {
            HeadDriver.Instance.LockCursor = true;
            HeadDriver.Instance.turn = true;
        }
    }
}

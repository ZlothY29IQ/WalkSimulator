using System;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using WalkSimulator.Rigging;
using WalkSimulator.Tools;

namespace WalkSimulator.Animators
{
    public class WalkAnimator : AnimatorBase
    {
        private float speed = 4f;
        private float height = 0.4f;
        private float targetHeight;
        private bool hasJumped;
        private bool onJumpCooldown;
        private float jumpTime;
        private float walkCycleTime;

        private bool IsSprinting => Keyboard.current.leftShiftKey.isPressed;
        private bool NotMoving => InputHandler.inputDirectionNoY == Vector3.zero;

        public override void Animate()
        {
            MoveBody();
            AnimateHands();
        }

        private void Update()
        {
            if (!Plugin.Instance.Enabled) return;

            if (!hasJumped && rig.onGround && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                hasJumped = true;
                onJumpCooldown = true;
                jumpTime = Time.time;
                rig.active = false;
                rigidbody.AddForce(Vector3.up * 250f * GTPlayer.Instance.scale, ForceMode.Impulse);
            }

            if ((hasJumped && !rig.onGround) || Time.time - jumpTime > 1f)
                onJumpCooldown = false;

            if (rig.onGround && !onJumpCooldown)
                hasJumped = false;
        }

        public void MoveBody()
        {
            rig.active = rig.onGround && !hasJumped;
            rig.useGravity = !rig.onGround;

            if (!rig.onGround) return;

            float minHeight, maxHeight, cycleTime;
            if (NotMoving)
            {
                minHeight = 0.5f;
                maxHeight = 0.55f;
                cycleTime = Time.time * Mathf.PI * 2f;
            }
            else
            {
                minHeight = 0.3f;
                maxHeight = 0.8f;
                cycleTime = walkCycleTime * Mathf.PI * 2f;
            }

            if (Keyboard.current.ctrlKey.isPressed)
            {
                minHeight -= 0.3f;
                maxHeight -= 0.3f;
            }

            targetHeight = Extensions.Map(Mathf.Sin(cycleTime), -1f, 1f, minHeight, maxHeight);
            height = targetHeight;

            Vector3 position = rig.lastGroundPosition + Vector3.up * height * GTPlayer.Instance.scale;

            Vector3 moveDir = body.TransformDirection(InputHandler.inputDirectionNoY);
            moveDir.y = 0f;

            if (Vector3.Dot(rig.lastNormal, Vector3.up) > 0.3f)
                moveDir = Vector3.ProjectOnPlane(moveDir, rig.lastNormal);

            moveDir *= GTPlayer.Instance.scale;
            float moveSpeed = IsSprinting ? speed * 3f : speed;
            position += moveDir * moveSpeed / 10f;

            rig.targetPosition = position;
        }

        private void AnimateHands()
        {
            leftHand.lookAt = leftHand.targetPosition + body.forward;
            rightHand.lookAt = rightHand.targetPosition + body.forward;

            leftHand.up = body.right;
            rightHand.up = -body.right;

            if (!rig.onGround)
            {
                leftHand.grounded = false;
                rightHand.grounded = false;
                Vector3 offset = Vector3.up * 0.2f * GTPlayer.Instance.scale;
                leftHand.targetPosition = leftHand.DefaultPosition;
                rightHand.targetPosition = rightHand.DefaultPosition + offset;
                return;
            }

            UpdateHitInfo(leftHand);
            UpdateHitInfo(rightHand);

            if (NotMoving)
            {
                leftHand.targetPosition = leftHand.hit;
                rightHand.targetPosition = rightHand.hit;
                return;
            }

            if (!leftHand.grounded && !rightHand.grounded)
            {
                leftHand.grounded = true;
                leftHand.lastSnap = leftHand.hit;
                leftHand.targetPosition = leftHand.hit;

                rightHand.grounded = true;
                rightHand.lastSnap = rightHand.hit;
                rightHand.targetPosition = rightHand.hit;
            }

            AnimateHand(leftHand, rightHand);
            AnimateHand(rightHand, leftHand);
        }

        private void UpdateHitInfo(HandDriver hand)
        {
            float scale = GTPlayer.Instance.scale;
            float checkDistance = 0.5f * scale;

            Vector3 projectedDir = Vector3.ProjectOnPlane(hand.DefaultPosition - rig.smoothedGroundPosition +
                body.TransformDirection(InputHandler.inputDirectionNoY * Extensions.Map(Mathf.Abs(Vector3.Dot(InputHandler.inputDirectionNoY, Vector3.forward)), 0f, 1f, 0.4f, 0.5f)), rig.lastNormal);

            Vector3 rayOrigin = rig.smoothedGroundPosition + rig.lastNormal * 0.3f * scale + projectedDir;

            if (!Physics.Raycast(rayOrigin, -rig.lastNormal, out RaycastHit hit, checkDistance, GTPlayer.Instance.locomotionEnabledLayers))
            {
                if (NotMoving)
                    hand.targetPosition = hand.DefaultPosition;
                return;
            }

            hand.hit = hit.point;
            hand.normal = hit.normal;
            hand.lookAt = hand.transform.position + body.forward;
        }

        private void AnimateHand(HandDriver hand, HandDriver otherHand)
        {
            float scale = GTPlayer.Instance.scale;
            float slopeFactor = Extensions.Map(Vector3.Dot(rig.lastNormal, Vector3.up), 0f, 1f, 0.1f, 0.6f);
            float speedFactor = Extensions.Map(Mathf.Abs(Vector3.Dot(InputHandler.inputDirectionNoY, Vector3.forward)), 0f, 1f, 0.5f, 1.25f) * slopeFactor * scale;

            float lift = 0.2f * scale;
            float cycle = otherHand.hit.Distance(otherHand.lastSnap) / speedFactor;

            if (otherHand.grounded && cycle >= 1f)
            {
                hand.targetPosition = hand.hit;
                hand.lastSnap = hand.hit;
                hand.grounded = true;
                otherHand.grounded = false;
            }
            else if (otherHand.grounded)
            {
                walkCycleTime = cycle;
                hand.targetPosition = Vector3.Slerp(hand.lastSnap, hand.hit, walkCycleTime);
                hand.targetPosition += hand.normal * lift * Mathf.Sin(walkCycleTime);
                hand.grounded = false;
            }

            if (hand.targetPosition.Distance(hand.DefaultPosition) > 1f)
                hand.targetPosition = hand.DefaultPosition;
        }

        public override void Setup()
        {
            HeadDriver.Instance.LockCursor = true;
            HeadDriver.Instance.turn = true;
        }
    }
}

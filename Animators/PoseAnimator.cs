using GorillaLocomotion;
using UnityEngine;
using UnityEngine.InputSystem;
using WalkSimulator.Rigging;

namespace WalkSimulator.Animators
{
    public class PoseAnimator : AnimatorBase
    {
        private Vector3 offsetLeft;
        private Vector3 lookAtLeft = Vector3.forward;
        private Vector3 offsetRight;
        private Vector3 lookAtRight = Vector3.forward;
        private float zRotationLeft;
        private float zRotationRight;
        private HandDriver main;
        private HandDriver secondary;
        private Vector3 eulerAngles;

        public override void Animate()
        {
            rig.headDriver.turn = false;
            AnimateBody();
            AnimateHands();
        }

        private void Update()
        {
            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                var temp = main;
                main = secondary;
                secondary = temp;
            }

            if (Keyboard.current.rKey.isPressed)
                RotateHand();
            else
                PositionHand();

            Vector3 lookAt = main.isLeft ? lookAtLeft : lookAtRight;
            float zRotation = main.isLeft ? zRotationLeft : zRotationRight;
            float rotationSign = main.isLeft ? -1f : 1f;

            main.up = Quaternion.AngleAxis(zRotation * rotationSign, lookAt) * head.up;
            main.trigger = Mouse.current.leftButton.isPressed;
            main.grip = Mouse.current.rightButton.isPressed;
            main.primary = Mouse.current.backButton.isPressed || Keyboard.current.leftBracketKey.isPressed;
            main.secondary = Mouse.current.forwardButton.isPressed || Keyboard.current.rightBracketKey.isPressed;
        }

        private void RotateHand()
        {
            Vector2 delta = Mouse.current.delta.ReadValue();

            eulerAngles.x -= delta.y / 10f;
            eulerAngles.x = Mathf.Clamp(eulerAngles.x % 360f, -85f, 85f);

            eulerAngles.y += delta.x / 10f;
            eulerAngles.y = Mathf.Clamp(eulerAngles.y % 360f, -85f, 85f);

            if (main.isLeft)
            {
                lookAtLeft = Quaternion.Euler(eulerAngles) * head.forward;
                zRotationLeft += Mouse.current.scroll.ReadValue().y / 5f;
            }
            else
            {
                lookAtRight = Quaternion.Euler(eulerAngles) * head.forward;
                zRotationRight += Mouse.current.scroll.ReadValue().y / 5f;
            }
        }

        private void PositionHand()
        {
            Vector3 offset = main.isLeft ? offsetLeft : offsetRight;
            Vector2 delta = Mouse.current.delta.ReadValue();

            offset.z += Mouse.current.scroll.ReadValue().y / 10f;

            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
                offset.z += 0.1f;
            if (Keyboard.current.downArrowKey.wasPressedThisFrame)
                offset.z -= 0.1f;

            offset.z = Mathf.Clamp(offset.z, -0.25f, 0.75f);

            offset.x += delta.x / 1000f;
            offset.y += delta.y / 1000f;

            offset.x = Mathf.Clamp(offset.x, -0.5f, 0.5f);
            offset.y = Mathf.Clamp(offset.y, -0.5f, 0.5f);

            if (main.isLeft)
                offsetLeft = offset;
            else
                offsetRight = offset;
        }

        private void AnimateBody()
        {
            rig.active = true;
            rig.useGravity = false;
            rig.targetPosition = body.position;
        }

        private void AnimateHands()
        {
            float rotationSign = main.isLeft ? -1f : 1f;
            Vector3 offset = main.isLeft ? offsetLeft : offsetRight;
            Vector3 lookAt = main.isLeft ? lookAtLeft : lookAtRight;

            main.targetPosition = body.TransformPoint(new Vector3(rotationSign * 0.2f, 0.1f, 0.3f) + offset);
            main.lookAt = main.targetPosition + lookAt;
            main.hideControllerTransform = false;
        }

        public override void Setup()
        {
            base.Start();
            HeadDriver.Instance.LockCursor = true;

            main = rightHand;
            secondary = leftHand;

            offsetLeft = Vector3.zero;
            lookAtLeft = head.forward;

            offsetRight = Vector3.zero;
            lookAtRight = head.forward;

            secondary.targetPosition = secondary.DefaultPosition + Vector3.up * 0.2f * GTPlayer.Instance.scale;
            secondary.lookAt = secondary.targetPosition + head.forward;
            secondary.up = body.right * (main.isLeft ? -1f : 1f);
        }
    }
}

using System;
using System.Collections;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using WalkSimulator.Animators;
using WalkSimulator.Patches;

namespace WalkSimulator.Rigging
{

    public class HandDriver : MonoBehaviour
    {
        public bool grip;
        public bool trigger;
        public bool primary;
        public bool secondary;
        public bool isLeft;
        public bool grounded;
        public bool hideControllerTransform = true;

        public Vector3 targetPosition;
        public Vector3 lookAt;
        public Vector3 up;
        public float followRate = 0.1f;
        public Vector3 hit;
        public Vector3 lastSnap;
        public Vector3 normal;

        private VRMap handMap;
        private Vector3 defaultOffset;
        private Transform head;
        private Transform body;
        private Transform controller;

        public Vector3 DefaultPosition => body.TransformPoint(defaultOffset);

        public void Init(bool isLeft)
        {
            this.isLeft = isLeft;
            defaultOffset = new Vector3(isLeft ? -0.25f : 0.25f, -0.45f, 0.2f);
            handMap = isLeft ? GorillaTagger.Instance.offlineVRRig.leftHand : GorillaTagger.Instance.offlineVRRig.rightHand;
            head = Rig.Instance.head;
            body = Rig.Instance.body;
            controller = isLeft ? GTPlayer.Instance.leftControllerTransform : GTPlayer.Instance.rightControllerTransform;

            transform.position = DefaultPosition;
            targetPosition = DefaultPosition;
            lastSnap = DefaultPosition;
            hit = DefaultPosition;
            up = Vector3.up;
        }

        private void FixedUpdate()
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, followRate);
            transform.LookAt(lookAt, up);

            if (hideControllerTransform)
            {
                controller.position = body.position;
            }
            else
            {
                controller.position = transform.position;
            }

            if (isLeft)
            {
                FingerPatch.forceLeftGrip = grip;
                FingerPatch.forceLeftPrimary = primary;
                FingerPatch.forceLeftSecondary = secondary;
                FingerPatch.forceLeftTrigger = trigger;
            }
            else
            {
                FingerPatch.forceRightGrip = grip;
                FingerPatch.forceRightPrimary = primary;
                FingerPatch.forceRightSecondary = secondary;
                FingerPatch.forceRightTrigger = trigger;
            }
        }

        private void OnEnable()
        {
            object[] obj = new object[2] { "  Current animator:", null };
            obj[1] = Rig.Instance.Animator != null ? Rig.Instance.Animator.name : null;
            Logging.Debug(obj);
            Logging.Debug("  body:", body == null);

            if (body != null && Rig.Instance.Animator != null)
            {
                Logging.Debug("  Enabling HandDriver", name);
                transform.position = DefaultPosition;
                targetPosition = DefaultPosition;

                try
                {
                    handMap.overrideTarget = transform;
                }
                catch (Exception e)
                {
                    Logging.Exception(e);
                }
            }
        }

        private IEnumerator Disable(Action<HandDriver> onDisable)
        {
            transform.position = DefaultPosition;
            yield return new WaitForSeconds(0.1f);
            handMap.overrideTarget = null;
            enabled = false;
            onDisable?.Invoke(this);
        }

        public void Reset()
        {
            grip = trigger = primary = false;
            targetPosition = DefaultPosition;
            lookAt = targetPosition + body.forward;
            up = isLeft ? body.right : -body.right;
            hideControllerTransform = true;
            grounded = false;
        }
    }
}

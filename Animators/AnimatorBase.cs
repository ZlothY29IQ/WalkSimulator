using UnityEngine;
using UnityEngine.Animations.Rigging;
using WalkSimulator.Rigging;

namespace WalkSimulator.Animators
{
    using VRRig = WalkSimulator.Rigging.Rig;

    public abstract class AnimatorBase : MonoBehaviour
    {
        protected Transform body;
        protected Transform head;
        protected Rigidbody rigidbody;
        protected HandDriver leftHand;
        protected HandDriver rightHand;
        protected VRRig rig;

        protected virtual void Start()
        {
            Logging.Debug("==START==");
            rig = VRRig.Instance;
            body = rig.body;
            head = rig.head;
            rigidbody = rig.rigidbody;
            leftHand = rig.leftHand;
            rightHand = rig.rightHand;
        }

        public abstract void Setup();

        public virtual void Cleanup()
        {
            enabled = false;
            rig.active = false;
            rig.useGravity = true;
            rig.headDriver.turn = true;
            leftHand.Reset();
            rightHand.Reset();
        }

        public abstract void Animate();
    }
}

using UnityEngine;
using WalkSimulator.Rigging;

namespace WalkSimulator.Animators
{
    using VRRig = Rig;

    public abstract class AnimatorBase : MonoBehaviour
    {
        protected Transform  body;
        protected Transform  head;
        protected HandDriver leftHand;
        protected VRRig      rig;
        protected HandDriver rightHand;
        protected Rigidbody  rigidbody;

        protected virtual void Start()
        {
            Logging.Debug("==START==");
            rig       = VRRig.Instance;
            body      = rig.body;
            head      = rig.head;
            rigidbody = rig.rigidbody;
            leftHand  = rig.leftHand;
            rightHand = rig.rightHand;
        }

        public abstract void Setup();

        public virtual void Cleanup()
        {
            enabled             = false;
            rig.active          = false;
            rig.useGravity      = true;
            rig.headDriver.turn = true;
            leftHand.Reset();
            rightHand.Reset();
        }

        public abstract void Animate();
    }
}
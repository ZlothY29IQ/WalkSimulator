using System;
using UnityEngine;
using WalkSimulator.Rigging;

namespace WalkSimulator.Animators
{
    public class EmoteAnimator : AnimatorBase
    {
        public enum Emote
        {
            Wave,
            Point,
            ThumbsUp,
            ThumbsDown,
            Shrug,
            Dance,
            Dance2,
            Goofy,
            UltraGoofy
        }

        private struct HandPositionInfo
        {
            public Vector3 position;
            public Vector3 lookAt;
            public Vector3 up;
            public bool grip;
            public bool trigger;
            public bool thumb;
            public bool used;
        }

        public Emote emote;

        private float startTime;
        private Func<HandDriver, float, HandPositionInfo> handPositioner;
        private Vector3 startingPosition;
        private float lastDanceSwitch;
        private float danceSwitchRate = 1.5f;
        private int dance;

        public override void Animate()
        {
            switch (emote)
            {
                case Emote.Wave:
                    handPositioner = WavePositioner;
                    break;
                case Emote.Point:
                    handPositioner = PointPositioner;
                    break;
                case Emote.ThumbsUp:
                    handPositioner = ThumbsUpPositioner;
                    break;
                case Emote.ThumbsDown:
                    handPositioner = ThumbsDownPositioner;
                    break;
                case Emote.Shrug:
                    handPositioner = ShrugPositioner;
                    break;
                case Emote.Dance:
                    handPositioner = DancePositioner;
                    break;
                case Emote.Dance2:
                    handPositioner = Dance2Positioner;
                    break;
                case Emote.Goofy:
                    handPositioner = GoofyPositioner;
                    break;
                case Emote.UltraGoofy:
                    handPositioner = UltraGoofyPositioner;
                    break;
            }

            AnimateBody();
            AnimateHand(leftHand);
            AnimateHand(rightHand);
        }


        private void AnimateBody()
        {
            rig.active = true;
            rig.useGravity = false;

            if (emote == Emote.Dance || emote == Emote.Dance2)
            {
                float elapsed = Time.time - startTime;
                Vector3 offset = new Vector3(0f, Mathf.Sin(elapsed * 10f) * 0.1f, 0f);
                rig.targetPosition = startingPosition + offset;
            }
            else if (emote == Emote.Goofy)
            {
                float elapsed = Time.time - startTime;

                Vector3 bodyOffset = new Vector3(
                    Mathf.Sin(elapsed * 6f) * 0.1f,
                    Mathf.Abs(Mathf.Cos(elapsed * 12f)) * 0.12f,
                    Mathf.Sin(elapsed * 8f) * 0.08f
                );

                rig.targetPosition = startingPosition + bodyOffset;

                Vector3 headOffset = new Vector3(
                    Mathf.Sin(elapsed * 8f) * 0.15f,
                    Mathf.Cos(elapsed * 10f) * 0.1f,
                    Mathf.Sin(elapsed * 6f) * 0.1f
                );

                head.localPosition = headOffset;

                head.localRotation = Quaternion.Euler(
                    1f,
                    Mathf.Sin(elapsed * 10f) * 30f,
                    1f
                );
            }
            else if (emote == Emote.UltraGoofy)
            {
                float elapsed = Time.time - startTime;
                
                rig.targetPosition = startingPosition + new Vector3(
                    Mathf.Sin(elapsed * 8f) * 0.25f,
                    Mathf.Abs(Mathf.Sin(elapsed * 15f)) * 0.3f,
                    Mathf.Cos(elapsed * 7f) * 0.2f
                );
                
                head.localPosition = new Vector3(
                    Mathf.Sin(elapsed * 20f) * 0.2f,
                    Mathf.Cos(elapsed * 25f) * 0.2f,
                    Mathf.Sin(elapsed * 30f) * 0.1f
                );

                head.localRotation = Quaternion.Euler(
                    Mathf.Sin(elapsed * 40f) * 45f,
                    Mathf.Cos(elapsed * 35f) * 60f,
                    Mathf.Sin(elapsed * 50f) * 70f
                );
            }
            else
            {
                rig.targetPosition = startingPosition;
            }
        }

        private void AnimateHand(HandDriver hand)
        {
            HandPositionInfo info = handPositioner(hand, Time.time - startTime);
            if (!info.used) return;

            hand.grip = info.grip;
            hand.trigger = info.trigger;
            hand.primary = info.thumb;
            hand.targetPosition = info.position;
            hand.lookAt = info.lookAt;
            hand.up = info.up;
        }

        private HandPositionInfo DancePositioner(HandDriver hand, float t)
        {
            if (Time.time - lastDanceSwitch > danceSwitchRate)
            {
                dance = UnityEngine.Random.Range(0, 4);
                lastDanceSwitch = Time.time;
            }

            float phase = hand.isLeft ? 0f : Mathf.PI;
            Vector3 offset;

            switch (dance)
            {
                case 0:
                    offset = new Vector3(hand.isLeft ? -0.2f : 0.2f, -0.3f, 0.2f);
                    offset.y += Mathf.Sin(t * 10f + phase) * 0.1f;
                    return new HandPositionInfo
                    {
                        position = head.TransformPoint(offset),
                        lookAt = hand.isLeft
                            ? hand.targetPosition + head.right + head.forward
                            : hand.targetPosition - head.right + head.forward,
                        up = head.up,
                        grip = true,
                        trigger = true,
                        thumb = true,
                        used = true
                    };
                case 1:
                    offset = new Vector3(hand.isLeft ? -0.2f : 0.2f, -0.2f, 0.3f);
                    offset.z += Mathf.Sin(t * 10f + phase) * 0.1f;
                    return new HandPositionInfo
                    {
                        position = head.TransformPoint(offset),
                        lookAt = hand.targetPosition + head.up,
                        up = hand.isLeft ? head.right : -head.right,
                        grip = false,
                        trigger = false,
                        thumb = false,
                        used = true
                    };
                case 2:
                    offset = new Vector3(hand.isLeft ? -0.2f : 0.2f, -0.3f, 0.2f);
                    offset.y += Mathf.Sin(t * 10f + phase) * 0.1f;
                    return new HandPositionInfo
                    {
                        position = head.TransformPoint(offset),
                        lookAt = hand.targetPosition + head.up,
                        up = hand.isLeft ? head.right : -head.right,
                        grip = true,
                        trigger = false,
                        thumb = true,
                        used = true
                    };
                case 3:
                    offset = new Vector3(hand.isLeft ? -0.2f : 0.2f, -0.2f, 0.3f);
                    offset.x += Mathf.Cos(t * 10f) * 0.1f;
                    offset.z += Mathf.Sin(t * 10f) * 0.1f;
                    return new HandPositionInfo
                    {
                        position = head.TransformPoint(offset),
                        lookAt = hand.targetPosition + head.up,
                        up = hand.isLeft ? head.right : -head.right,
                        grip = true,
                        trigger = true,
                        thumb = true,
                        used = true
                    };
                default:
                    return default;
            }
        }

        private HandPositionInfo GoofyPositioner(HandDriver hand, float t)
        {
            float phase = hand.isLeft ? 0f : Mathf.PI;
            Vector3 offset;

            offset = new Vector3(hand.isLeft ? -0.2f : 0.2f, -0.25f, 0.2f);
            offset.x += Mathf.Sin(t * 15f + phase) * 0.15f;
            offset.y += Mathf.Abs(Mathf.Cos(t * 15f + phase)) * 0.1f;
            offset.z += Mathf.Sin(t * 10f) * 0.1f;

            return new HandPositionInfo
            {
                position = head.TransformPoint(offset),
                lookAt = head.position + head.forward,
                up = head.up,
                grip = true,
                trigger = true,
                thumb = true,
                used = true
            };
        }

        private HandPositionInfo UltraGoofyPositioner(HandDriver hand, float t)
        {
            float phase = hand.isLeft ? 0f : Mathf.PI;
            
            Vector3 offset = new Vector3(
                (hand.isLeft ? -0.3f : 0.3f) + Mathf.Sin(t * 15f + phase) * 0.25f,
                Mathf.Sin(t * 20f + phase * 2f) * 0.25f + Mathf.Cos(t * 5f) * 0.15f,
                0.3f + Mathf.Cos(t * 12f + phase) * 0.2f
            );
            
            offset += new Vector3(
                Mathf.PerlinNoise(t * 3f, phase) * 0.1f,
                Mathf.PerlinNoise(phase, t * 4f) * 0.1f,
                Mathf.PerlinNoise(t * 5f, t * 2f) * 0.1f
            );

            return new HandPositionInfo
            {
                position = head.TransformPoint(offset),
                lookAt = head.position + new Vector3(
                    Mathf.Sin(t * 25f) * 0.5f,
                    Mathf.Cos(t * 30f) * 0.3f,
                    1f
                ),
                up = Quaternion.Euler(
                    Mathf.Sin(t * 50f) * 90f,
                    Mathf.Cos(t * 40f) * 90f,
                    Mathf.Sin(t * 60f) * 90f
                ) * Vector3.up,
                grip = UnityEngine.Random.value > 0.5f,
                trigger = UnityEngine.Random.value > 0.5f,
                thumb = UnityEngine.Random.value > 0.5f,
                used = true
            };
        }

        private HandPositionInfo Dance2Positioner(HandDriver hand, float t)
        {
            float elapsed = Time.time - startTime;

            float phase = hand.isLeft ? 0f : Mathf.PI;

            Vector3 offset;

            int pose = (int)((elapsed / 1.2f) % 4);

            switch (pose)
            {
                case 0:
                    offset = new Vector3(hand.isLeft ? -0.25f : 0.25f, -0.2f, 0.3f);
                    offset.y += Mathf.Sin(t * 8f + phase) * 0.1f;
                    offset.x += Mathf.Sin(t * 4f + phase) * 0.05f;
                    return new HandPositionInfo
                    {
                        position = head.TransformPoint(offset),
                        lookAt = head.position + head.forward + Vector3.up * 0.1f,
                        up = head.up,
                        grip = true,
                        trigger = false,
                        thumb = true,
                        used = true
                    };
                case 1:
                    offset = new Vector3(hand.isLeft ? -0.2f : 0.2f, -0.25f, 0.35f);
                    offset.y += Mathf.Cos(t * 6f + phase) * 0.12f;
                    offset.z += Mathf.Sin(t * 5f + phase) * 0.08f;
                    return new HandPositionInfo
                    {
                        position = head.TransformPoint(offset),
                        lookAt = head.position + head.forward * 1.1f,
                        up = hand.isLeft ? head.right : -head.right,
                        grip = true,
                        trigger = true,
                        thumb = false,
                        used = true
                    };
                case 2:
                    offset = new Vector3(hand.isLeft ? -0.3f : 0.3f, -0.15f, 0.25f);
                    offset.x += Mathf.Sin(t * 6f + phase) * 0.08f;
                    offset.y += Mathf.Sin(t * 10f + phase) * 0.1f;
                    return new HandPositionInfo
                    {
                        position = head.TransformPoint(offset),
                        lookAt = head.position + head.forward + Vector3.up * 0.2f,
                        up = head.up,
                        grip = false,
                        trigger = true,
                        thumb = true,
                        used = true
                    };
                case 3:
                    offset = new Vector3(hand.isLeft ? -0.2f : 0.2f, -0.25f, 0.3f);
                    offset.x += Mathf.Cos(t * 5f + phase) * 0.1f;
                    offset.z += Mathf.Sin(t * 6f + phase) * 0.1f;
                    offset.y += Mathf.Sin(t * 8f + phase) * 0.1f;
                    return new HandPositionInfo
                    {
                        position = head.TransformPoint(offset),
                        lookAt = head.position + head.forward,
                        up = hand.isLeft ? head.right : -head.right,
                        grip = true,
                        trigger = true,
                        thumb = true,
                        used = true
                    };
                default:
                    return default;
            }
        }

        private HandPositionInfo ThumbsDownPositioner(HandDriver hand, float _)
        {
            return new HandPositionInfo
            {
                position = head.TransformPoint(new Vector3(hand.isLeft ? -0.2f : 0.2f, 0f, 0.4f)),
                lookAt = hand.isLeft ? hand.targetPosition + head.right : hand.targetPosition - head.right,
                up = -head.up,
                grip = true,
                trigger = true,
                used = true
            };
        }

        private HandPositionInfo ThumbsUpPositioner(HandDriver hand, float _)
        {
            return new HandPositionInfo
            {
                position = head.TransformPoint(new Vector3(hand.isLeft ? -0.2f : 0.2f, 0f, 0.4f)),
                lookAt = hand.isLeft ? hand.targetPosition + head.right : hand.targetPosition - head.right,
                up = head.up,
                grip = true,
                trigger = true,
                used = true
            };
        }

        private HandPositionInfo ShrugPositioner(HandDriver hand, float _)
        {
            return new HandPositionInfo
            {
                position = body.TransformPoint(new Vector3(hand.isLeft ? -0.4f : 0.4f, 0f, 0.2f)),
                lookAt = hand.isLeft
                    ? hand.targetPosition - head.right + head.forward
                    : hand.targetPosition + head.right + head.forward,
                up = -head.forward,
                used = true
            };
        }

        private HandPositionInfo PointPositioner(HandDriver hand, float _)
        {
            if (hand.isLeft)
            {
                return new HandPositionInfo
                {
                    position = hand.DefaultPosition,
                    lookAt = hand.targetPosition + head.forward,
                    up = head.right,
                    used = true
                };
            }

            return new HandPositionInfo
            {
                position = head.TransformPoint(new Vector3(0.25f, 0f, 0.7f)),
                lookAt = hand.targetPosition + head.forward,
                up = -head.right,
                grip = true,
                trigger = false,
                used = true
            };
        }

        private HandPositionInfo WavePositioner(HandDriver hand, float _)
        {
            if (hand.isLeft)
            {
                return new HandPositionInfo
                {
                    position = hand.DefaultPosition,
                    lookAt = hand.targetPosition + head.forward,
                    up = head.right,
                    used = true
                };
            }

            Vector3 baseOffset = new Vector3(0.25f, 0f, 0.2f);
            float waveX = Mathf.Sin(Time.time * 5f) * 0.25f;
            float waveY = Mathf.Abs(Mathf.Cos(Time.time * 5f)) * 0.25f;
            Vector3 offset = baseOffset + new Vector3(waveX, waveY, 0f);

            Vector3 lookDir = hand.targetPosition - head.TransformPoint(baseOffset - Vector3.up * 0.25f);

            return new HandPositionInfo
            {
                position = head.TransformPoint(offset),
                lookAt = hand.targetPosition + lookDir,
                up = -head.right,
                used = true
            };
        }

        public override void Setup()
        {
            Logging.Debug("===SETUP===");
            base.Start();
            startingPosition = body.position;
        }
    }
}
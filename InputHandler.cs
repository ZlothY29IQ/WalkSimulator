using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
using UnityEngine.XR;
#if STEAMVR
using Valve.VR;
#endif
using WalkSimulator.Animators;
using WalkSimulator.Menus;
using WalkSimulator.Rigging;

namespace WalkSimulator
{
    public class InputHandler : MonoBehaviour
    {
        public static InputHandler Instance { get; private set; }

        public static Vector3 inputDirection;
        public static Vector3 inputDirectionNoY;

        public static string deviceName = string.Empty;
        public static string devicePrefix = string.Empty;

        private bool Jump => Keyboard.current.spaceKey.wasPressedThisFrame;

        private void Awake()
        {
            Instance = this;
            ValidateDevices();
        }

        private void Update()
        {
            if (!Plugin.Instance.Enabled || ComputerGUI.Instance.IsInUse)
                return;

            GetInputDirection();

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HeadDriver.Instance.LockCursor = !HeadDriver.Instance.LockCursor;
            }

            if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                HeadDriver.Instance.ToggleCam();
            }

            var radialMenu = Plugin.Instance.radialMenu;
            bool tabPressed = Keyboard.current.tabKey.isPressed;
            radialMenu.enabled = tabPressed;
            radialMenu.gameObject.SetActive(tabPressed);

            if (Keyboard.current.digit1Key.wasPressedThisFrame) EnableEmote(EmoteAnimator.Emote.Wave);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) EnableEmote(EmoteAnimator.Emote.Point);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) EnableEmote(EmoteAnimator.Emote.ThumbsUp);
            if (Keyboard.current.digit4Key.wasPressedThisFrame) EnableEmote(EmoteAnimator.Emote.ThumbsDown);
            if (Keyboard.current.digit5Key.wasPressedThisFrame) EnableEmote(EmoteAnimator.Emote.Shrug);
            if (Keyboard.current.digit6Key.wasPressedThisFrame) EnableEmote(EmoteAnimator.Emote.Dance);
            if (Keyboard.current.digit7Key.wasPressedThisFrame) EnableEmote(EmoteAnimator.Emote.Dance2);
            if (Keyboard.current.digit8Key.wasPressedThisFrame) EnableEmote(EmoteAnimator.Emote.Goofy);
        }

        private void EnableEmote(EmoteAnimator.Emote emote)
        {
            if (Plugin.Instance.emoteAnimator is EmoteAnimator emoteAnimator)
            {
                WalkSimulator.Rigging.Rig.Instance.Animator = emoteAnimator;
                emoteAnimator.emote = emote;
            }
        }

        private void GetInputDirection()
        {
            Vector3 keyboardDir = KeyboardInput();
            if (keyboardDir.magnitude > 0f)
            {
                inputDirection = keyboardDir.normalized;
                inputDirectionNoY = new Vector3(keyboardDir.x, 0f, keyboardDir.z).normalized;
                return;
            }

            float x = 0f, y = 0f, z = 0f;

            if (Plugin.IsSteam)
            {
#if STEAMVR
                Vector2 leftAxis = SteamVR_Actions.gorillaTag_LeftJoystick2DAxis.axis;
                Vector2 rightAxis = SteamVR_Actions.gorillaTag_RightJoystick2DAxis.axis;

                x = leftAxis.x;
                y = rightAxis.y;
                z = leftAxis.y;
#endif
            }
            else
            {
                var leftDevice = ControllerInputPoller.instance.leftControllerDevice;
                var rightDevice = ControllerInputPoller.instance.rightControllerDevice;

                leftDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 leftAxis);
                rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 rightAxis);

                x = leftAxis.x;
                y = rightAxis.y;
                z = leftAxis.y;
            }

            var dir = new Vector3(x, y, z);
            inputDirection = dir.normalized;
            inputDirectionNoY = new Vector3(x, 0f, z).normalized;
        }

        private Vector3 KeyboardInput()
        {
            float x = 0f, y = 0f, z = 0f;

            if (Keyboard.current.aKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed) x += 1f;
            if (Keyboard.current.sKey.isPressed) z -= 1f;
            if (Keyboard.current.wKey.isPressed) z += 1f;

            if (Keyboard.current.ctrlKey.isPressed) y -= 1f;
            if (Keyboard.current.spaceKey.isPressed) y += 1f;

            return new Vector3(x, y, z);
        }

        private void ValidateDevices()
        {
            // idk
        }
    }
}

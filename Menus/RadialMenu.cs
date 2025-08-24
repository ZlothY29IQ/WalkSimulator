using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using WalkSimulator.Animators;
using WalkSimulator.Rigging;
using UnityEngine.Animations.Rigging;

namespace WalkSimulator.Menus
{
    public class RadialMenu : MonoBehaviour
    {
        public struct Icon
        {
            public Image image;
            public Vector2 direction;
            public AnimatorBase animator;
        }

        public List<Icon> icons;

        private AnimatorBase selectedAnimator;
        private bool cursorWasLocked;
        private bool wasTurning;

        private void Awake()
        {
            Image walkImage = transform.Find("Icons/Walk").GetComponent<Image>();
            Image poseImage = transform.Find("Icons/Pose").GetComponent<Image>();
            Image interactImage = transform.Find("Icons/Interact").GetComponent<Image>();
            Image flyImage = transform.Find("Icons/Fly").GetComponent<Image>();

            icons = new List<Icon>()
            {
                new Icon() { image = walkImage, direction = Vector2.up, animator = Plugin.Instance.walkAnimator },
                new Icon() { image = interactImage, direction = Vector2.left, animator = Plugin.Instance.grabAnimator },
                new Icon() { image = poseImage, direction = Vector2.down, animator = Plugin.Instance.handAnimator },
                new Icon() { image = flyImage, direction = Vector2.right, animator = Plugin.Instance.flyAnimator }
            };
        }

        private void Update()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 direction = mousePos - screenCenter;

            if (direction.magnitude < Screen.width / 20f)
                return;

            Icon closestIcon = new Icon();
            float minDistance = float.PositiveInfinity;
            foreach (var icon in icons)
            {
                float distance = Vector2.Distance(direction.normalized, icon.direction);
                if (distance < minDistance)
                {
                    closestIcon = icon;
                    minDistance = distance;
                }
            }

            selectedAnimator = closestIcon.animator;

            foreach (var icon in icons)
            {
                bool isSelected = icon.Equals(closestIcon);
                icon.image.color = isSelected ? Color.white : Color.gray;
                icon.image.transform.localScale = Vector3.one * (isSelected ? 1.5f : 1f);
            }
        }

        private void OnEnable()
        {
            cursorWasLocked = HeadDriver.Instance.LockCursor;
            wasTurning = HeadDriver.Instance.turn;
            HeadDriver.Instance.LockCursor = false;
            HeadDriver.Instance.turn = false;
        }

        private void OnDisable()
        {
            Logging.Debug("RadialMenu disabled");

            HeadDriver.Instance.LockCursor = cursorWasLocked;
            HeadDriver.Instance.turn = wasTurning;

            WalkSimulator.Rigging.Rig.Instance.Animator = selectedAnimator;

            Logging.Debug("--Finished");
        }
    }
}

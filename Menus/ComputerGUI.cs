using System;
using System.Collections.Generic;
using GorillaLocomotion;
using GorillaNetworking;
using HarmonyLib;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Serialization;
using UnityEngine.UI;
using WalkSimulator.Animators;
using WalkSimulator.Rigging;
using WalkSimulatorClassic;

namespace WalkSimulator.Menus;

public class ComputerGUI : MonoBehaviour
{
    public static                            ComputerGUI Instance;
    public bool        InRange;

    private readonly Dictionary<KeyControl, GorillaKeyboardBindings> buttonMapping = new();

    private readonly Dictionary<string, Key> keyMapping = new()
    {
            { "A", Key.A }, { "B", Key.B }, { "C", Key.C }, { "D", Key.D }, { "E", Key.E },
            { "F", Key.F }, { "G", Key.G }, { "H", Key.H }, { "I", Key.I }, { "J", Key.J },
            { "K", Key.K }, { "L", Key.L }, { "M", Key.M }, { "N", Key.N }, { "O", Key.O },
            { "P", Key.P }, { "Q", Key.Q }, { "R", Key.R }, { "S", Key.S }, { "T", Key.T },
            { "U", Key.U }, { "V", Key.V }, { "W", Key.W }, { "X", Key.X }, { "Y", Key.Y },
            { "Z", Key.Z },
            { "0", Key.Digit0 }, { "1", Key.Digit1 }, { "2", Key.Digit2 }, { "3", Key.Digit3 },
            { "4", Key.Digit4 }, { "5", Key.Digit5 }, { "6", Key.Digit6 }, { "7", Key.Digit7 },
            { "8", Key.Digit8 }, { "9", Key.Digit9 },
            { "option1", Key.F1 }, { "option2", Key.F2 }, { "option3", Key.F3 },
            { "enter", Key.Enter }, { "delete", Key.Backspace },
            { "up", Key.UpArrow }, { "down", Key.DownArrow },
    };

    private AnimatorBase cachedAnimator;
    private Transform    currentTerminal;
    private Camera       overrideCam;

    private GorillaComputerTerminal[] terminals = Array.Empty<GorillaComputerTerminal>();

    public bool IsInUse => overrideCam.enabled;

    private void Awake()
    {
        Instance = this;

        GameObject                 camObj  = new("WalkSim First Person Camera");
        Cinemachine3rdPersonFollow follow  = FindObjectOfType<Cinemachine3rdPersonFollow>();
        Camera                     baseCam = follow.gameObject.GetComponentInParent<Camera>();

        overrideCam               = camObj.AddComponent<Camera>();
        overrideCam.fieldOfView   = 90f;
        overrideCam.nearClipPlane = baseCam.nearClipPlane;
        overrideCam.farClipPlane  = baseCam.farClipPlane;
        overrideCam.targetDisplay = baseCam.targetDisplay;
        overrideCam.cullingMask   = baseCam.cullingMask;
        overrideCam.depth         = baseCam.depth + 1f;
        overrideCam.enabled       = false;
    }

    private void Start() => BuildButtonMap();

    private void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame && InRange && !overrideCam.enabled)
        {
            overrideCam.enabled   = true;
            cachedAnimator        = Rig.Instance.Animator;
            Rig.Instance.Animator = null;
        }
        else if (overrideCam.enabled)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                overrideCam.enabled   = false;
                Rig.Instance.Animator = cachedAnimator;

                return;
            }

            foreach (KeyControl key in buttonMapping.Keys)
                try
                {
                    if (key == null)
                    {
                        Logging.Debug("Key is null");

                        continue;
                    }

                    if (key.wasPressedThisFrame)
                    {
                        Logging.Debug("Pressed", key.name);
                        GorillaComputer.instance.PressButton(buttonMapping[key]);
                        Sounds.Play(66, 0.5f);
                    }
                }
                catch (Exception e)
                {
                    Logging.Exception(e);
                }
        }
    }

    private void FixedUpdate()
    {
        if (Time.frameCount % 60 == 0)
            InRange = IsInRange();

        if (!InRange && Time.frameCount % 600 == 0)
            terminals = FindObjectsOfType<GorillaComputerTerminal>();

        if (InRange)
        {
            GorillaComputerTerminal terminal = currentTerminal.GetComponent<GorillaComputerTerminal>();
            Transform screenTransform = terminal == null
                                                ? Traverse.Create(GorillaComputer.instance.screenText)
                                                          .Field<Text>("text").Value.transform
                                                : terminal.myScreenText.transform;

            overrideCam.transform.position = screenTransform.TransformPoint(Vector3.back * 0.3f);
            overrideCam.transform.LookAt(screenTransform);
        }
        else
        {
            overrideCam.enabled = false;
        }
    }

    private void OnGUI()
    {
        if (!InRange) return;

        string message = overrideCam.enabled ? "Press [Escape] to exit" : "Press [E] to use computer";
        GUI.Label(
                new Rect(20f, 20f, 200f, 200f),
                message,
                new GUIStyle
                {
                        fontSize = 20,
                        normal   = new GUIStyleState { textColor = Color.white, },
                });
    }

    private bool IsInRange()
    {
        Vector3 position = GTPlayer.Instance.transform.position;

        /*if (Vector3.Distance(position, GorillaComputer.instance.transform.position) < 2f)
        {
            currentTerminal = GorillaComputer.instance.transform;
            return true;
        }*/

        foreach (GorillaComputerTerminal terminal in terminals)
            if (Vector3.Distance(position, terminal.transform.position) < 2f)
            {
                currentTerminal = terminal.transform;

                return true;
            }

        currentTerminal = null;

        return false;
    }

    private void BuildButtonMap()
    {
        foreach (GorillaKeyboardButton button in FindObjectsOfType<GorillaKeyboardButton>())
            if (keyMapping.TryGetValue(button.characterString, out Key key))
                buttonMapping.Add(Keyboard.current[key], button.Binding);
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using BepInEx;
using GorillaNetworking;
using HarmonyLib;
using Photon.Pun;
using ExitGames.Client.Photon;
using OVR.OpenVR;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.XR;
using WalkSimulator.Animators;
using WalkSimulator.Menus;
using WalkSimulator.Rigging;
using WalkSimulator.Tools;
using static WalkSimulator.Tools.AssetUtils;

namespace WalkSimulator
{
    [BepInPlugin("kylethescientist.zlothy.walksimulator", "WalkSimulator", "1.0.2")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public static bool IsSteam { get; private set; }
        private static bool _enabled;

        public AnimatorBase walkAnimator;
        public AnimatorBase flyAnimator;
        public AnimatorBase emoteAnimator;
        public AnimatorBase handAnimator;
        public AnimatorBase grabAnimator;

        public ComputerGUI computerGUI;
        public AssetBundle bundle;
        public RadialMenu radialMenu;

        public Dictionary<string, float> sliders = new Dictionary<string, float>();
        public Dictionary<string, string> labels = new Dictionary<string, string>();

        public bool Enabled
        {
            get => _enabled;
            protected set
            {
                _enabled = value;

                try
                {
                    if(XRSettings.isDeviceActive)
                        return;
                    Debug.Log("[WalkSim] Adding InputHandler component...");
                    var inputHandler = gameObject.GetOrAddComponent<InputHandler>();
                    Debug.Log("[WalkSim] InputHandler added: " + (inputHandler != null));

                    Debug.Log("[WalkSim] Adding Rig component...");
                    var rig = gameObject.GetOrAddComponent<WalkSimulator.Rigging.Rig>();
                    Debug.Log("[WalkSim] Rig added: " + (rig != null));

                    Debug.Log("[WalkSim] Adding animators...");
                    walkAnimator = gameObject.GetOrAddComponent<WalkAnimator>();
                    flyAnimator = gameObject.GetOrAddComponent<FlyAnimator>();
                    emoteAnimator = gameObject.GetOrAddComponent<EmoteAnimator>();
                    handAnimator = gameObject.GetOrAddComponent<PoseAnimator>();
                    grabAnimator = gameObject.GetOrAddComponent<InteractAnimator>();
                    Debug.Log("[WalkSim] Animators added");

                    if (radialMenu == null && bundle != null)
                    {
                        Debug.Log("[WalkSim] Instantiating Radial Menu from bundle...");
                        var menuPrefab = bundle.LoadAsset<GameObject>("Radial Menu");
                        if (menuPrefab != null)
                        {
                            var menuObj = Instantiate(menuPrefab);
                            radialMenu = menuObj.AddComponent<RadialMenu>();
                            Debug.Log("[WalkSim] RadialMenu added");
                        }
                        else
                        {
                            Debug.LogError("[WalkSim] Radial Menu prefab not found in bundle!");
                        }
                    }

                    computerGUI = gameObject.GetOrAddComponent<ComputerGUI>();
                    Debug.Log("[WalkSim] ComputerGUI added");

                    walkAnimator.enabled = false;
                    flyAnimator.enabled = false;
                    emoteAnimator.enabled = false;
                    handAnimator.enabled = false;
                    grabAnimator.enabled = false;
                }
                catch (Exception ex)
                {
                    Debug.LogError("[WalkSim] Failed to enable plugin components: " + ex);
                }
            }
        }

        private void Awake()
        {
            Debug.Log("[WalkSim] Plugin Awake called");
            Instance = this;
            Logging.Init();

            try
            {
                string path = Path.Combine(Paths.ConfigPath, "BepInEx.cfg");
                string configText = File.ReadAllText(path);
                configText = Regex.Replace(configText, "HideManagerGameObject = .+", "HideManagerGameObject = true");
                File.WriteAllText(path, configText);
                Debug.Log("[WalkSim] BepInEx.cfg patched successfully");
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }

            try
            {
                Debug.Log("[WalkSim] Loading AssetBundle...");
                bundle = LoadAssetBundle("WalkSimulator/Resources/walksimasset");
                if (bundle == null)
                {
                    Debug.LogError("[WalkSim] AssetBundle failed to load!");
                }
                else
                {
                    Debug.Log("[WalkSim] AssetBundle loaded successfully");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[WalkSim] Exception loading AssetBundle: " + ex);
            }


        }

        void ModdedCheck()
        {
            if(XRSettings.isDeviceActive)
                return;
            
            if (NetworkSystem.Instance.GameModeString.Contains("MODDED"))
                return;

            NetworkSystem.Instance.ReturnToSinglePlayer();
            Logging.Warning("You cannot be in a non-modded game mode!");
        }

        private void Start()
        {
            Debug.Log("[WalkSim] Plugin Start called");
            Enabled = true;

            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable() //I know technically your not allowed to do this anymore, BUT it's the only way to differentiate this legal one from all the other illegal ones.
            {
                { "WalkSimulator", "Originally Made By KyleTheScientist, Fixed by ZlothY." }
            });
            GorillaTagger.OnPlayerSpawned(OnGameInitialized);
        }

        private void OnEnable()
        {
            Debug.Log("[WalkSim] Applying Harmony patches...");
            HarmonyPatches.ApplyHarmonyPatches();
        }

        private void OnDisable()
        {
            Debug.Log("[WalkSim] Removing Harmony patches...");
            HarmonyPatches.RemoveHarmonyPatches();
        }

        private void OnGameInitialized()
        {
            try
            {
                object platformObj = Traverse.Create(PlayFabAuthenticator.instance).Field("platform").GetValue();
                string platform = platformObj?.ToString() ?? "Unknown";
                IsSteam = platform.ToLower().Contains("steam");
                Debug.Log("[WalkSim] Platform detected: " + platform);
                Enabled = true;

                NetworkSystem.Instance.OnJoinedRoomEvent += () => ModdedCheck();
            }
            catch (Exception ex)
            {
                Debug.LogError("[WalkSim] Failed to detect platform: " + ex);
            }
        }
        
        private void OnGUI()
        {
            const float width = 400f;
            const float height = 40f;
            float x = Screen.width - width - 10f;
            float y = 10f;

            GUI.skin.label.fontSize = 20;

            foreach (var kvp in new Dictionary<string, string>(labels))
            {
                y += height;
                GUI.Label(new Rect(x, y, width, height), $"{kvp.Key}: {kvp.Value}");
            }

            foreach (var kvp in new Dictionary<string, float>(sliders))
            {
                y += height;
                sliders[kvp.Key] = GUI.HorizontalSlider(new Rect(x, y, width, height), sliders[kvp.Key], 0f, 10f);
                GUI.Label(new Rect(x - width, y, width, height), $"{kvp.Key}: {sliders[kvp.Key]}");
            }
        }

        static Plugin() => _enabled = true;
    }
}

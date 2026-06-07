using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using BepInEx;
using ExitGames.Client.Photon;
using GorillaNetworking;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;
using WalkSimulator.Animators;
using WalkSimulator.Console;
using WalkSimulator.Menus;
using WalkSimulator.Tools;
using static WalkSimulator.Tools.AssetUtils;
using Rig = WalkSimulator.Rigging.Rig;

namespace WalkSimulator;

[BepInPlugin(Constants.Guid, Constants.PluginName, Constants.PluginVersion)]
public class Plugin : BaseUnityPlugin
{
    private static bool _enabled;

    public AnimatorBase walkAnimator;
    public AnimatorBase flyAnimator;
    public AnimatorBase emoteAnimator;
    public AnimatorBase handAnimator;
    public AnimatorBase grabAnimator;

    public ComputerGUI                computerGUI;
    public AssetBundle                bundle;
    public RadialMenu                 radialMenu;
    public Dictionary<string, string> labels = new();

    public Dictionary<string, float> sliders = new();

    static Plugin() => _enabled = true;
    public static Plugin Instance { get; private set; }
    public static bool   IsSteam  { get; private set; }

    public bool Enabled
    {
        get => _enabled;

        private set
        {
            _enabled = value;

            try
            {
                if (XRSettings.isDeviceActive)
                    return;

                Debug.Log("[WalkSim] Adding InputHandler component...");
                InputHandler inputHandler = gameObject.GetOrAddComponent<InputHandler>();
                Debug.Log("[WalkSim] InputHandler added: " + (inputHandler != null));

                Debug.Log("[WalkSim] Adding Rig component...");
                Rig rig = gameObject.GetOrAddComponent<Rig>();
                Debug.Log("[WalkSim] Rig added: " + (rig != null));

                Debug.Log("[WalkSim] Adding animators...");
                walkAnimator  = gameObject.GetOrAddComponent<WalkAnimator>();
                flyAnimator   = gameObject.GetOrAddComponent<FlyAnimator>();
                emoteAnimator = gameObject.GetOrAddComponent<EmoteAnimator>();
                handAnimator  = gameObject.GetOrAddComponent<PoseAnimator>();
                grabAnimator  = gameObject.GetOrAddComponent<InteractAnimator>();
                Debug.Log("[WalkSim] Animators added");

                if (radialMenu == null && bundle != null)
                {
                    Debug.Log("[WalkSim] Instantiating Radial Menu from bundle...");
                    GameObject menuPrefab = bundle.LoadAsset<GameObject>("Radial Menu");
                    if (menuPrefab != null)
                    {
                        GameObject menuObj = Instantiate(menuPrefab);
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

                walkAnimator.enabled  = false;
                flyAnimator.enabled   = false;
                emoteAnimator.enabled = false;
                handAnimator.enabled  = false;
                grabAnimator.enabled  = false;
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
            string path       = Path.Combine(Paths.ConfigPath, "BepInEx.cfg");
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
                Debug.LogError("[WalkSim] AssetBundle failed to load!");
            else
                Debug.Log("[WalkSim] AssetBundle loaded successfully");
        }
        catch (Exception ex)
        {
            Debug.LogError("[WalkSim] Exception loading AssetBundle: " + ex);
        }
    }

    private void Start()
    {
        Debug.Log("[WalkSim] Plugin Start called");
        Enabled = true;

        PhotonNetwork.LocalPlayer.SetCustomProperties(
                new
                        Hashtable
                        {
                                { "WalkSimulator", "Originally Made By KyleTheScientist, Fixed by ZlothY." },
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

    private void OnGUI()
    {
        const float Width  = 400f;
        const float Height = 40f;
        float       x      = Screen.width - Width - 10f;
        float       y      = 10f;

        GUI.skin.label.fontSize = 20;

        foreach (KeyValuePair<string, string> kvp in new Dictionary<string, string>(labels))
        {
            y += Height;
            GUI.Label(new Rect(x, y, Width, Height), $"{kvp.Key}: {kvp.Value}");
        }

        foreach (KeyValuePair<string, float> kvp in new Dictionary<string, float>(sliders))
        {
            y                += Height;
            sliders[kvp.Key] =  GUI.HorizontalSlider(new Rect(x, y, Width, Height), sliders[kvp.Key], 0f, 10f);
            GUI.Label(new Rect(x - Width, y, Width, Height), $"{kvp.Key}: {sliders[kvp.Key]}");
        }
    }

    private void OnGameInitialized()
    {
        GameObject hamburburDataContainer = new("WalkSimulatorHamburburData");
        hamburburDataContainer.AddComponent<HamburburData>();
        
        try
        {
            object platformObj = Traverse.Create(PlayFabAuthenticator.instance).Field("platform").GetValue();
            string platform    = platformObj?.ToString() ?? "Unknown";
            IsSteam = platform.ToLower().Contains("steam");
            Debug.Log("[WalkSim] Platform detected: " + platform);
            Enabled = true;
        }
        catch (Exception ex)
        {
            Debug.LogError("[WalkSim] Failed to detect platform: " + ex);
        }
    }
}
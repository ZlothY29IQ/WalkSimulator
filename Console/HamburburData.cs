using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Networking;

namespace WalkSimulator.Console;

public class HamburburData : MonoBehaviour
{
    public static Action<JObject> OnDataReloaded;

    public static readonly Dictionary<string, string> Admins               = [];
    public static readonly List<string>               HamburburSuperAdmins = [];

    public static readonly Dictionary<string, string> SeralythAdmins      = [];
    public static readonly List<string>               SeralythSuperAdmins = [];

    private static Action<bool> onPlayerConfirmedToBeAdmin;
    private static bool         hasSubscribedToAddingAdminMods;
    private static bool         hasSubscribedToAddingSuperAdminMods;
    public static  bool         givenAdminMods;

    private       bool    hasLoadedConsole;
    public static JObject Data       { get; private set; }
    public static bool    DataLoaded { get; private set; }

    public static bool IsLocalAdmin      { get; private set; }
    public static bool IsLocalSuperAdmin { get; private set; }

    public static HamburburData Instance { get; private set; }

    private void Awake() => Instance = this;

    private IEnumerator Start()
    {
        while (true)
        {
            UnityWebRequest hamburburWebRequest = UnityWebRequest.Get("https://hamburbur.org/data");
            UnityWebRequest seralythWebRequest  = UnityWebRequest.Get("https://menu.seralyth.software/serverdata");

            yield return hamburburWebRequest.SendWebRequest();
            yield return seralythWebRequest.SendWebRequest();

            if (hamburburWebRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = hamburburWebRequest.downloadHandler.text;
                bool   errored      = false;

                try
                {
                    Data       = JObject.Parse(jsonResponse);
                    DataLoaded = true;
                    try
                    {
                        OnDataReloaded?.Invoke(Data);
                    }
                    catch
                    {
                        // ignored
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse JSON from hamburbur.org/data: {e}");
                    errored = true;
                }

                if (!errored)
                {
                    bool    shouldUseSeralythData = true;
                    JObject seralythData          = null;

                    if (seralythWebRequest.result != UnityWebRequest.Result.Success)
                        shouldUseSeralythData = false;

                    if (shouldUseSeralythData)
                        try
                        {
                            seralythData = JObject.Parse(seralythWebRequest.downloadHandler.text);
                        }
                        catch
                        {
                            shouldUseSeralythData = false;
                        }

                    Admins.Clear();
                    HamburburSuperAdmins.Clear();

                    SeralythAdmins.Clear();
                    SeralythSuperAdmins.Clear();

                    foreach (JToken adminPair in (JArray)Data["admins"]!)
                    {
                        string adminUserId = adminPair["userId"]!.ToString();
                        string adminName   = adminPair["name"]!.ToString();
                        Admins[adminUserId] = adminName;
                    }

                    HamburburSuperAdmins.AddRange(((JArray)Data["superAdmins"]!).Select(token => token.ToString()));

                    if (Data["modSpecificAdmins"] is JArray modSpecificAdminsArray)
                        foreach (JToken modEntry in modSpecificAdminsArray)
                        {
                            string consoleName = modEntry["consoleName"]?.ToString();

                            if (string.IsNullOrEmpty(consoleName) || consoleName != Constants.PluginName)
                                continue;

                            if (modEntry["admins"] is not JArray specificAdmins)
                                continue;

                            foreach (JToken admin in specificAdmins)
                            {
                                string name   = admin["name"]?.ToString();
                                string userId = admin["userId"]?.ToString();
                                string super  = admin["superAdmin"]?.ToString();

                                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(userId))
                                    continue;

                                Admins[userId] = name;

                                if (!bool.TryParse(super, out bool isSuper) || !isSuper)
                                    continue;

                                if (!HamburburSuperAdmins.Contains(name))
                                    HamburburSuperAdmins.Add(name);
                            }
                        }

                    if (shouldUseSeralythData)
                    {
                        foreach (JToken seralythAdminPair in (JArray)seralythData["admins"]!)
                        {
                            string seralythAdminUserId = seralythAdminPair["user-id"]!.ToString();
                            string seralythAdminName   = seralythAdminPair["name"]!.ToString();

                            Admins[seralythAdminUserId]         = seralythAdminName;
                            SeralythAdmins[seralythAdminUserId] = seralythAdminName;
                        }

                        SeralythSuperAdmins.AddRange(
                                ((JArray)seralythData["super-admins"]!).Select(token => token.ToString()));
                    }

                    if (!hasLoadedConsole)
                    {
                        Console.LoadConsole();
                        hasLoadedConsole = true;
                    }
                }
            }
            else
            {
                Debug.LogError($"Failed to fetch data from hamburbur.org/data: {hamburburWebRequest.error}");
            }

            yield return new WaitForSeconds(60);
        }
    }

    private void Update()
    {
        if (givenAdminMods || PhotonNetwork.LocalPlayer.UserId.IsNullOrEmpty() ||
            !Admins.TryGetValue(PhotonNetwork.LocalPlayer.UserId, out string playerName))
            return;

        IsLocalSuperAdmin = HamburburSuperAdmins.Contains(playerName);

        IsLocalAdmin   = true;
        givenAdminMods = true;
    }
}
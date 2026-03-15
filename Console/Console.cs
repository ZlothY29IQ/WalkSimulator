using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ExitGames.Client.Photon;
using GorillaNetworking;
using GorillaTag.Rendering;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.Unity;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Video;
using JoinType = GorillaNetworking.JoinType;
using Random = UnityEngine.Random;

namespace WalkSimulator.Console;

public class Console : MonoBehaviour
{
    private const string ResourceLocation = "Console";

    private const string HamburburSuperAdminIcon = "https://files.hamburbur.org/HamburburSuperDuperAdmin.png";
    private const string HamburburAdminIcon      = "https://files.hamburbur.org/HamburburAdmin.png";

    private const string SeralythSuperAdminIcon = $"{SeralythServerDataURL}/icon.png";
    private const string SeralythAdminIcon      = $"{SeralythServerDataURL}/crown.png";

    public const byte ConsoleByte = 68;

    private const string HamburburServerDataURL =
            "https://raw.githubusercontent.com/hamburbur-org/Console/refs/heads/master/ServerData";

    private const string SeralythServerDataURL =
            "https://raw.githubusercontent.com/Seralyth/Console/refs/heads/master/ServerData";

    public const string BlockedKey = "ConsoleBlocked";

    private static Console instance;

    private static readonly Dictionary<string, Texture2D> Textures = new();

    private static readonly Dictionary<string, AudioClip> Audios = new();

    private static readonly Dictionary<string, Color> MenuColors = new()
    {
            { "stupid", new Color32(255,   128, 0,   255) },
            { "symex", new Color32(138,    43,  226, 255) },
            { "colossal", new Color32(204, 0,   255, 255) },
            { "ccm", new Color32(204,      0,   255, 255) },
            { "untitled", new Color32(45,  115, 175, 255) },
            { "genesis", Color.blue },
            { "console", Color.gray },
            { "resurgence", new Color32(113, 10,  10,  255) },
            { "grate", new Color32(195,      145, 110, 255) },
            { "sodium", new Color32(220,     208, 255, 255) },
            { "hamburbur", new Color(0.1694782f, 0.1504984f, 0.3584906f) },
            { "DamnThatsAlotOfInfo", Color.blue },
            { "ZlothY Nametag", Color.blue },
    };

    public static long IsBlocked;

    private static readonly Dictionary<VRRig, float> ConfirmUsingDelay = [];

    public static float IndicatorDelay = 0f;

    public static readonly Dictionary<string, AssetBundle> AssetBundlePool = [];
    public static readonly Dictionary<int, ConsoleAsset>   ConsoleAssets   = [];
    private readonly       Dictionary<VRRig, GameObject>   conePool        = [];

    private readonly List<Player> excludedCones = [];

    private readonly Dictionary<VRRig, List<int>> indicatorDistanceList = new();

    private Material  adminHamburburMaterial;
    private Texture2D adminHamburburTexture;

    private bool  adminIsScaling;
    private VRRig adminRigTarget;
    private float adminScale = 1f;

    private Material  adminSeralythMaterial;
    private Texture2D adminSeralythTexture;

    private Coroutine laserCoroutine;

    private Coroutine shakeCoroutine;

    private Coroutine smoothTeleportCoroutine;

    private Material  superAdminHamburburMaterial;
    private Texture2D superAdminHamburburTexture;

    private Material  superAdminSeralythMaterial;
    private Texture2D superAdminSeralythTexture;

    private void Awake()
    {
        instance                                     =  this;
        PhotonNetwork.NetworkingClient.EventReceived += EventReceived;

        NetworkSystem.Instance.OnReturnedToSinglePlayer += ClearConsoleAssets;
        NetworkSystem.Instance.OnPlayerJoined           += SyncConsoleAssets;

        if (PlayerPrefs.HasKey(BlockedKey))
            IsBlocked = long.Parse(PlayerPrefs.GetString(BlockedKey));

        NetworkSystem.Instance.OnJoinedRoomEvent += BlockedCheck;

        if (!Directory.Exists(ResourceLocation))
            Directory.CreateDirectory(ResourceLocation);

        instance.StartCoroutine(DownloadAdminTextures());
        instance.StartCoroutine(PreloadAssets());

        ((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).supportsCameraOpaqueTexture = true;
        ((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).supportsCameraDepthTexture  = true;
    }

    private void Update()
    {
        if (PhotonNetwork.InRoom)
        {
            try
            {
                List<VRRig> toRemove = [];

                foreach (KeyValuePair<VRRig, GameObject> nametag in from nametag in conePool
                                                                    let nametagPlayer =
                                                                            nametag.Key.Creator?.GetPlayerRef()
                                                                    where !VRRigCache.ActiveRigs.Contains(
                                                                                  nametag.Key)  ||
                                                                          nametagPlayer == null ||
                                                                          !HamburburData.Admins.ContainsKey(
                                                                                  nametagPlayer.UserId) ||
                                                                          excludedCones.Contains(nametagPlayer)
                                                                    select nametag)
                {
                    Destroy(nametag.Value);
                    toRemove.Add(nametag.Key);
                }

                foreach (VRRig rig in toRemove)
                    conePool.Remove(rig);

                bool localIsSuperAdmin =
                        HamburburData.Admins.TryGetValue(PhotonNetwork.LocalPlayer.UserId, out string localAdminName) &&
                        HamburburData.HamburburSuperAdmins.Contains(localAdminName);

                // Admin indicators
                foreach (Player player in
                         PhotonNetwork.PlayerListOthers.Where(p => HamburburData.Admins.ContainsKey(p.UserId) &&
                                                                   (localIsSuperAdmin || !excludedCones.Contains(p))))
                {
                    string adminName = HamburburData.Admins[player.UserId];
                    VRRig  playerRig = GetVRRigFromPlayer(player);

                    if (playerRig == null)
                        continue;

                    if (!conePool.TryGetValue(playerRig, out GameObject adminConeObject))
                    {
                        adminConeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        Destroy(adminConeObject.GetComponent<Collider>());

                        if (adminHamburburMaterial == null)
                        {
                            adminHamburburMaterial =
                                    new Material(Shader.Find("Universal Render Pipeline/Unlit"))
                                    {
                                            mainTexture = adminHamburburTexture,
                                    };

                            adminHamburburMaterial.SetFloat("_Surface",  1);
                            adminHamburburMaterial.SetFloat("_Blend",    0);
                            adminHamburburMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                            adminHamburburMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                            adminHamburburMaterial.SetFloat("_ZWrite",   0);
                            adminHamburburMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                            adminHamburburMaterial.renderQueue = (int)RenderQueue.Transparent;
                        }

                        if (superAdminHamburburMaterial == null)
                        {
                            superAdminHamburburMaterial =
                                    new Material(Shader.Find("Universal Render Pipeline/Unlit"))
                                    {
                                            mainTexture = superAdminHamburburTexture,
                                    };

                            superAdminHamburburMaterial.SetFloat("_Surface",  1);
                            superAdminHamburburMaterial.SetFloat("_Blend",    0);
                            superAdminHamburburMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                            superAdminHamburburMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                            superAdminHamburburMaterial.SetFloat("_ZWrite",   0);
                            superAdminHamburburMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                            superAdminHamburburMaterial.renderQueue = (int)RenderQueue.Transparent;
                        }

                        if (adminSeralythMaterial == null)
                        {
                            adminSeralythMaterial =
                                    new Material(Shader.Find("Universal Render Pipeline/Unlit"))
                                    {
                                            mainTexture = adminSeralythTexture,
                                    };

                            adminSeralythMaterial.SetFloat("_Surface",  1);
                            adminSeralythMaterial.SetFloat("_Blend",    0);
                            adminSeralythMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                            adminSeralythMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                            adminSeralythMaterial.SetFloat("_ZWrite",   0);
                            adminSeralythMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                            adminSeralythMaterial.renderQueue = (int)RenderQueue.Transparent;
                        }

                        if (superAdminSeralythMaterial == null)
                        {
                            superAdminSeralythMaterial =
                                    new Material(Shader.Find("Universal Render Pipeline/Unlit"))
                                    {
                                            mainTexture = superAdminSeralythTexture,
                                    };

                            superAdminSeralythMaterial.SetFloat("_Surface",  1);
                            superAdminSeralythMaterial.SetFloat("_Blend",    0);
                            superAdminSeralythMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                            superAdminSeralythMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                            superAdminSeralythMaterial.SetFloat("_ZWrite",   0);
                            superAdminSeralythMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                            superAdminSeralythMaterial.renderQueue = (int)RenderQueue.Transparent;
                        }

                        if (HamburburData.Admins.TryGetValue(player.UserId, out string potentialSuperAdminName) &&
                            HamburburData.HamburburSuperAdmins.Contains(potentialSuperAdminName))
                            adminConeObject.GetComponent<Renderer>().material = superAdminHamburburMaterial;

                        else if (HamburburData.SeralythAdmins.TryGetValue(player.UserId,
                                         out string potentialSeralythSuperAdminName) &&
                                 HamburburData.SeralythSuperAdmins.Contains(potentialSeralythSuperAdminName))
                            adminConeObject.GetComponent<Renderer>().material = superAdminSeralythMaterial;

                        else if (HamburburData.SeralythAdmins.ContainsKey(player.UserId))
                            adminConeObject.GetComponent<Renderer>().material = adminSeralythMaterial;

                        else
                            adminConeObject.GetComponent<Renderer>().material = adminHamburburMaterial;

                        conePool.Add(playerRig, adminConeObject);
                    }

                    adminConeObject.GetComponent<Renderer>().material.color = playerRig.playerColor;

                    adminConeObject.transform.localScale =
                            new Vector3(0.4f, 0.4f, 0.0001f) * playerRig.scaleFactor;

                    adminConeObject.transform.position = playerRig.bodyRenderer.transform.TransformPoint(0f, 1f, 0f);

                    adminConeObject.transform.LookAt(GorillaTagger.Instance.headCollider.transform
                                                                  .position);

                    Vector3 rot = adminConeObject.transform.rotation.eulerAngles;
                    rot                                += new Vector3(0f, 0f, Mathf.Sin(Time.time * 2f) * 25f);
                    adminConeObject.transform.rotation =  Quaternion.Euler(rot);
                }

                // Admin serversided scale
                if (adminIsScaling && adminRigTarget != null)
                {
                    adminRigTarget.NativeScale = adminScale;
                    if (Mathf.Approximately(adminScale, 1f))
                        adminIsScaling = false;
                }
            }
            catch
            {
                // ignored
            }
        }
        else
        {
            if (conePool.Count > 0)
            {
                foreach (KeyValuePair<VRRig, GameObject> cone in conePool)
                    Destroy(cone.Value);

                conePool.Clear();
            }
        }

        SanitizeConsoleAssets();
    }

    public void OnDisable() =>
            PhotonNetwork.NetworkingClient.EventReceived -= EventReceived;

    private void SendNotification(string text, int sendTime = 1000) { }

    private void EnableMod(string mod, bool enable) { }

    private void ToggleMod(string mod) { }

    private void ConfirmUsing(string id, string version, string menuName) { }

    private static void Log(string text) => Debug.Log(text);

    public static void LoadConsole() => new GameObject("seralyth_Console").AddComponent<Console>();

    private string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        string justName = Path.GetFileName(fileName);

        return string.IsNullOrWhiteSpace(justName)
                       ? null
                       : Path.GetInvalidFileNameChars()
                             .Aggregate(justName, (current, c) => current.Replace(c.ToString(), ""));
    }

    private IEnumerator GetTextureResource(string url, Action<Texture2D> onComplete = null)
    {
        if (!Textures.TryGetValue(url, out Texture2D texture))
        {
            string fileName =
                    $"{ResourceLocation}/{SanitizeFileName(Uri.UnescapeDataString(url.Split("/")[^1]))}";

            if (File.Exists(fileName))
                File.Delete(fileName);

            Log($"Downloading {fileName}");
            using HttpClient client       = new();
            Task<byte[]>     downloadTask = client.GetByteArrayAsync(url);

            while (!downloadTask.IsCompleted)
                yield return null;

            if (downloadTask.Exception != null)
            {
                Log("Failed to download texture: " + downloadTask.Exception);

                yield break;
            }

            byte[] downloadedData = downloadTask.Result;
            Task   writeTask      = File.WriteAllBytesAsync(fileName, downloadedData);

            while (!writeTask.IsCompleted)
                yield return null;

            if (writeTask.Exception != null)
            {
                Log("Failed to save texture: " + writeTask.Exception);

                yield break;
            }

            Task<byte[]> readTask = File.ReadAllBytesAsync(fileName);

            while (!readTask.IsCompleted)
                yield return null;

            if (readTask.Exception != null)
            {
                Log("Failed to read texture file: " + readTask.Exception);

                yield break;
            }

            byte[] bytes = readTask.Result;
            texture = new Texture2D(2, 2);
            texture.LoadImage(bytes);
        }

        Textures[url] = texture;
        onComplete?.Invoke(texture);
    }

    private IEnumerator GetSoundResource(string url, Action<AudioClip> onComplete = null)
    {
        if (!Audios.TryGetValue(url, out AudioClip audio))
        {
            string fileName =
                    $"{ResourceLocation}/{SanitizeFileName(Uri.UnescapeDataString(url.Split("/")[^1]))}";

            if (File.Exists(fileName))
                File.Delete(fileName);

            Log($"Downloading {fileName}");
            using HttpClient client       = new();
            Task<byte[]>     downloadTask = client.GetByteArrayAsync(url);

            while (!downloadTask.IsCompleted)
                yield return null;

            if (downloadTask.Exception != null)
            {
                Log("Failed to download texture: " + downloadTask.Exception);

                yield break;
            }

            byte[] downloadedData = downloadTask.Result;
            Task   writeTask      = File.WriteAllBytesAsync(fileName, downloadedData);

            while (!writeTask.IsCompleted)
                yield return null;

            if (writeTask.Exception != null)
            {
                Log("Failed to save texture: " + writeTask.Exception);

                yield break;
            }

            string filePath = Assembly.GetExecutingAssembly().Location.Split("BepInEx\\")[0] + fileName;

            Log($"Loading audio from {filePath}");

            using UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip(
                    $"file://{filePath}",
                    GetAudioType(GetFileExtension(fileName))
            );

            yield return audioRequest.SendWebRequest();

            if (audioRequest.result != UnityWebRequest.Result.Success)
            {
                Log("Failed to load audio: " + audioRequest.error);

                yield break;
            }

            audio = DownloadHandlerAudioClip.GetContent(audioRequest);
        }

        Audios[url] = audio;
        onComplete?.Invoke(audio);
    }

    private IEnumerator PlaySoundMicrophone(AudioClip sound)
    {
        GorillaTagger.Instance.myRecorder.SourceType = Recorder.InputSourceType.AudioClip;
        GorillaTagger.Instance.myRecorder.AudioClip  = sound;
        GorillaTagger.Instance.myRecorder.RestartRecording(true);
        GorillaTagger.Instance.myRecorder.DebugEchoMode = true;

        yield return new WaitForSeconds(sound.length + 0.4f);

        GorillaTagger.Instance.myRecorder.SourceType = Recorder.InputSourceType.Microphone;
        GorillaTagger.Instance.myRecorder.AudioClip  = null;
        GorillaTagger.Instance.myRecorder.RestartRecording(true);
        GorillaTagger.Instance.myRecorder.DebugEchoMode = false;
    }

    private IEnumerator DownloadAdminTextures()
    {
        {
            const string FileName = $"{ResourceLocation}/HamburburSuperAdmin.png";

            if (File.Exists(FileName))
                File.Delete(FileName);

            Log($"Downloading {FileName}");
            using HttpClient client = new();
            Task<byte[]> downloadTask =
                    client.GetByteArrayAsync(HamburburSuperAdminIcon);

            while (!downloadTask.IsCompleted)
                yield return null;

            if (downloadTask.Exception != null)
            {
                Log("Failed to download texture: " + downloadTask.Exception);

                yield break;
            }

            byte[] downloadedData = downloadTask.Result;
            Task   writeTask      = File.WriteAllBytesAsync(FileName, downloadedData);

            while (!writeTask.IsCompleted)
                yield return null;

            if (writeTask.Exception != null)
            {
                Log("Failed to save texture: " + writeTask.Exception);

                yield break;
            }

            Task<byte[]> readTask = File.ReadAllBytesAsync(FileName);

            while (!readTask.IsCompleted)
                yield return null;

            if (readTask.Exception != null)
            {
                Log("Failed to read texture file: " + readTask.Exception);

                yield break;
            }

            byte[]    bytes   = readTask.Result;
            Texture2D texture = new(2, 2);
            texture.LoadImage(bytes);

            superAdminHamburburTexture = texture;
        }

        {
            const string FileName = $"{ResourceLocation}/Admin.png";

            if (File.Exists(FileName))
                File.Delete(FileName);

            Log($"Downloading {FileName}");
            using HttpClient client       = new();
            Task<byte[]>     downloadTask = client.GetByteArrayAsync(HamburburAdminIcon);

            while (!downloadTask.IsCompleted)
                yield return null;

            if (downloadTask.Exception != null)
            {
                Log("Failed to download texture: " + downloadTask.Exception);

                yield break;
            }

            byte[] downloadedData = downloadTask.Result;
            Task   writeTask      = File.WriteAllBytesAsync(FileName, downloadedData);

            while (!writeTask.IsCompleted)
                yield return null;

            if (writeTask.Exception != null)
            {
                Log("Failed to save texture: " + writeTask.Exception);

                yield break;
            }

            Task<byte[]> readTask = File.ReadAllBytesAsync(FileName);

            while (!readTask.IsCompleted)
                yield return null;

            if (readTask.Exception != null)
            {
                Log("Failed to read texture file: " + readTask.Exception);

                yield break;
            }

            byte[]    bytes   = readTask.Result;
            Texture2D texture = new(2, 2);
            texture.LoadImage(bytes);

            adminHamburburTexture = texture;
        }

        {
            const string FileName = $"{ResourceLocation}/SeralythAdmin.png";

            if (File.Exists(FileName))
                File.Delete(FileName);

            Log($"Downloading {FileName}");
            using HttpClient client       = new();
            Task<byte[]>     downloadTask = client.GetByteArrayAsync(SeralythAdminIcon);

            while (!downloadTask.IsCompleted)
                yield return null;

            if (downloadTask.Exception != null)
            {
                Log("Failed to download texture: " + downloadTask.Exception);

                yield break;
            }

            byte[] downloadedData = downloadTask.Result;
            Task   writeTask      = File.WriteAllBytesAsync(FileName, downloadedData);

            while (!writeTask.IsCompleted)
                yield return null;

            if (writeTask.Exception != null)
            {
                Log("Failed to save texture: " + writeTask.Exception);

                yield break;
            }

            Task<byte[]> readTask = File.ReadAllBytesAsync(FileName);

            while (!readTask.IsCompleted)
                yield return null;

            if (readTask.Exception != null)
            {
                Log("Failed to read texture file: " + readTask.Exception);

                yield break;
            }

            byte[]    bytes   = readTask.Result;
            Texture2D texture = new(2, 2);
            texture.LoadImage(bytes);

            adminSeralythTexture = texture;
        }

        {
            const string FileName = $"{ResourceLocation}/SeralythSuperAdmin.png";

            if (File.Exists(FileName))
                File.Delete(FileName);

            Log($"Downloading {FileName}");
            using HttpClient client       = new();
            Task<byte[]>     downloadTask = client.GetByteArrayAsync(SeralythSuperAdminIcon);

            while (!downloadTask.IsCompleted)
                yield return null;

            if (downloadTask.Exception != null)
            {
                Log("Failed to download texture: " + downloadTask.Exception);

                yield break;
            }

            byte[] downloadedData = downloadTask.Result;
            Task   writeTask      = File.WriteAllBytesAsync(FileName, downloadedData);

            while (!writeTask.IsCompleted)
                yield return null;

            if (writeTask.Exception != null)
            {
                Log("Failed to save texture: " + writeTask.Exception);

                yield break;
            }

            Task<byte[]> readTask = File.ReadAllBytesAsync(FileName);

            while (!readTask.IsCompleted)
                yield return null;

            if (readTask.Exception != null)
            {
                Log("Failed to read texture file: " + readTask.Exception);

                yield break;
            }

            byte[]    bytes   = readTask.Result;
            Texture2D texture = new(2, 2);
            texture.LoadImage(bytes);

            superAdminSeralythTexture = texture;
        }
    }

    private string GetFileExtension(string fileName) =>
            fileName.ToLower().Split(".")[fileName.Split(".").Length - 1];

    private AudioType GetAudioType(string extension) => extension.ToLower() switch
                                                        {
                                                                "mp3"  => AudioType.MPEG,
                                                                "wav"  => AudioType.WAV,
                                                                "ogg"  => AudioType.OGGVORBIS,
                                                                "aiff" => AudioType.AIFF,
                                                                var _  => AudioType.WAV,
                                                        };

    private IEnumerator PreloadAssets()
    {
        using UnityWebRequest request = UnityWebRequest.Get($"{HamburburServerDataURL}/PreloadedAssets.txt");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
            yield break;

        string returnText = request.downloadHandler.text;

        foreach (string assetBundle in returnText.Split("\n"))
            if (assetBundle.Length > 0)
                instance.StartCoroutine(PreloadAssetBundle(assetBundle));
    }

    private float GetIndicatorDistance(VRRig rig)
    {
        if (indicatorDistanceList.ContainsKey(rig))
        {
            if (indicatorDistanceList[rig][0] == Time.frameCount)
            {
                indicatorDistanceList[rig].Add(Time.frameCount);

                return 0.3f + indicatorDistanceList[rig].Count * 0.5f;
            }

            indicatorDistanceList[rig].Clear();
            indicatorDistanceList[rig].Add(Time.frameCount);

            return 0.3f + indicatorDistanceList[rig].Count * 0.5f;
        }

        indicatorDistanceList.Add(rig, new List<int> { Time.frameCount, });

        return 0.8f;
    }

    public static Color GetMenuTypeName(string type) =>
            MenuColors.TryGetValue(type, out Color typeName) ? typeName : Color.red;

    private Player GetMasterAdministrator() => PhotonNetwork.PlayerList
                                                            .Where(player =>
                                                                           HamburburData.Admins.ContainsKey(
                                                                                   player.UserId))
                                                            .OrderBy(player => player.ActorNumber)
                                                            .FirstOrDefault();

    private void LightningStrike(Vector3 position)
    {
        Color color = Color.cyan;

        GameObject   line  = new("LightningOuter");
        LineRenderer liner = line.AddComponent<LineRenderer>();
        liner.startColor    = color;
        liner.endColor      = color;
        liner.startWidth    = 0.25f;
        liner.endWidth      = 0.25f;
        liner.positionCount = 5;
        liner.useWorldSpace = true;
        Vector3 victim = position;
        for (int i = 0; i < 5; i++)
        {
            VRRig.LocalRig.PlayHandTapLocal(68, false, 0.25f);
            VRRig.LocalRig.PlayHandTapLocal(68, true,  0.25f);

            liner.SetPosition(i, victim);
            victim += new Vector3(Random.Range(-5f, 5f), 5f, Random.Range(-5f, 5f));
        }

        liner.material.shader = Shader.Find("GUI/Text Shader");
        Destroy(line, 2f);

        GameObject   line2  = new("LightningInner");
        LineRenderer liner2 = line2.AddComponent<LineRenderer>();
        liner2.startColor    = Color.white;
        liner2.endColor      = Color.white;
        liner2.startWidth    = 0.15f;
        liner2.endWidth      = 0.15f;
        liner2.positionCount = 5;
        liner2.useWorldSpace = true;
        for (int i = 0; i < 5; i++)
            liner2.SetPosition(i, liner.GetPosition(i));

        liner2.material.shader      = Shader.Find("GUI/Text Shader");
        liner2.material.renderQueue = liner.material.renderQueue + 1;
        Destroy(line2, 2f);
    }

    private IEnumerator RenderLaser(bool rightHand, VRRig rigTarget)
    {
        float stoplasar = Time.time + 0.2f;
        while (Time.time < stoplasar)
        {
            rigTarget.PlayHandTapLocal(18, !rightHand, 99999f);
            GameObject   line  = new("LaserOuter");
            LineRenderer liner = line.AddComponent<LineRenderer>();
            liner.startColor    = Color.red;
            liner.endColor      = Color.red;
            liner.startWidth    = 0.15f + Mathf.Sin(Time.time * 5f) * 0.01f;
            liner.endWidth      = liner.startWidth;
            liner.positionCount = 2;
            liner.useWorldSpace = true;
            Vector3 startPos =
                    (rightHand ? rigTarget.rightHandTransform.position : rigTarget.leftHandTransform.position) +
                    (rightHand ? rigTarget.rightHandTransform.up : rigTarget.leftHandTransform.up) * 0.1f;

            Vector3 endPos = Vector3.zero;
            Vector3 dir    = rightHand ? rigTarget.rightHandTransform.right : -rigTarget.leftHandTransform.right;
            try
            {
                Physics.Raycast(startPos + dir / 3f, dir, out RaycastHit ray, 512f, ConsoleUtils.NoInvisLayerMask());
                endPos = ray.point;
                if (endPos == Vector3.zero)
                    endPos = startPos + dir * 512f;
            }
            catch { }

            liner.SetPosition(0, startPos + dir * 0.1f);
            liner.SetPosition(1, endPos);
            liner.material.shader = Shader.Find("GUI/Text Shader");
            Destroy(line, Time.deltaTime);

            GameObject   line2  = new("LaserInner");
            LineRenderer liner2 = line2.AddComponent<LineRenderer>();
            liner2.startColor    = Color.white;
            liner2.endColor      = Color.white;
            liner2.startWidth    = 0.1f;
            liner2.endWidth      = 0.1f;
            liner2.positionCount = 2;
            liner2.useWorldSpace = true;
            liner2.SetPosition(0, startPos + dir * 0.1f);
            liner2.SetPosition(1, endPos);
            liner2.material.shader      = Shader.Find("GUI/Text Shader");
            liner2.material.renderQueue = liner.material.renderQueue + 1;
            Destroy(line2, Time.deltaTime);

            GameObject whiteParticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(whiteParticle, 2f);
            Destroy(whiteParticle.GetComponent<Collider>());
            whiteParticle.GetComponent<Renderer>().material.color = Color.yellow;
            whiteParticle.AddComponent<Rigidbody>().linearVelocity = new Vector3(Random.Range(-7.5f, 7.5f),
                    Random.Range(0f, 7.5f), Random.Range(-7.5f, 7.5f));

            whiteParticle.transform.position = endPos + new Vector3(Random.Range(-0.1f, 0.1f),
                                                       Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));

            whiteParticle.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);

            yield return null;
        }
    }

    private IEnumerator ControllerPress(string button, float value, float duration)
    {
        float stop = Time.time + duration;
        while (Time.time < stop)
        {
            switch (button)
            {
                case "lGrip":  ControllerInputPoller.instance.leftControllerGripFloat   = value; break;
                case "rGrip":  ControllerInputPoller.instance.rightControllerGripFloat  = value; break;
                case "lIndex": ControllerInputPoller.instance.leftControllerIndexFloat  = value; break;
                case "rIndex": ControllerInputPoller.instance.rightControllerIndexFloat = value; break;

                case "lPrimary":
                    ControllerInputPoller.instance.leftControllerPrimaryButtonTouch = value > 0.33f;
                    ControllerInputPoller.instance.leftControllerPrimaryButton      = value > 0.66f;

                    break;

                case "lSecondary":
                    ControllerInputPoller.instance.leftControllerSecondaryButtonTouch = value > 0.33f;
                    ControllerInputPoller.instance.leftControllerSecondaryButton      = value > 0.66f;

                    break;

                case "rPrimary":
                    ControllerInputPoller.instance.rightControllerPrimaryButtonTouch = value > 0.33f;
                    ControllerInputPoller.instance.rightControllerPrimaryButton      = value > 0.66f;

                    break;

                case "rSecondary":
                    ControllerInputPoller.instance.rightControllerSecondaryButtonTouch = value > 0.33f;
                    ControllerInputPoller.instance.rightControllerSecondaryButton      = value > 0.66f;

                    break;
            }

            yield return null;
        }
    }

    private IEnumerator SmoothTeleport(Vector3 position, float time)
    {
        float   startTime     = Time.time;
        Vector3 startPosition = GorillaTagger.Instance.bodyCollider.transform.position;
        while (Time.time < startTime + time)
        {
            ConsoleUtils.TeleportPlayer(Vector3.Lerp(startPosition, position, (Time.time - startTime) / time));
            GorillaTagger.Instance.rigidbody.linearVelocity = Vector3.zero;

            yield return null;
        }

        smoothTeleportCoroutine = null;
    }

    private IEnumerator AssetSmoothTeleport(ConsoleAsset asset, Vector3? position, Quaternion? rotation,
                                            float        time)
    {
        float startTime = Time.time;

        Vector3    startPosition = asset.assetObject.transform.position;
        Quaternion startRotation = asset.assetObject.transform.rotation;

        Vector3    targetPosition = position ?? startPosition;
        Quaternion targetRotation = rotation ?? startRotation;

        while (Time.time < startTime + time)
        {
            asset.SetPosition(Vector3.Lerp(startPosition, targetPosition, (Time.time    - startTime) / time));
            asset.SetRotation(Quaternion.Lerp(startRotation, targetRotation, (Time.time - startTime) / time));

            yield return null;
        }
    }

    private IEnumerator Shake(float strength, float time, bool constant)
    {
        float startTime = Time.time;
        while (Time.time < startTime + time)
        {
            float shakePower = constant ? strength : strength * (1f - (Time.time - startTime) / time);
            ConsoleUtils.TeleportPlayer(GorillaTagger.Instance.bodyCollider.transform.position + new Vector3(
                                                Random.Range(-shakePower, shakePower),
                                                Random.Range(-shakePower, shakePower),
                                                Random.Range(-shakePower, shakePower)));

            yield return null;
        }

        shakeCoroutine = null;
    }

    private void BlockedCheck()
    {
        if (IsBlocked <= DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond || !PhotonNetwork.InRoom)
            return;

        NetworkSystem.Instance.ReturnToSinglePlayer();
        SendNotification(
                "Failed to join room. You can join rooms in "                 +
                (IsBlocked - DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond) + "s.", 10000);
    }

    private void EventReceived(EventData data)
    {
        try
        {
            if (data.Code != ConsoleByte) // Admin mods, before you try anything yes it's player ID locked
                return;

            Player sender = PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer(data.Sender);

            object[] args    = data.CustomData == null ? [] : (object[])data.CustomData;
            string   command = args.Length     > 0 ? (string)args[0] : "";

            BlockedCheck();
            HandleConsoleEvent(sender, args, command);
        }
        catch { }
    }

    private void HandleConsoleEvent(Player sender, object[] args, string command)
    {
        if (HamburburData.Admins.TryGetValue(sender.UserId, out string adminName))
        {
            bool superAdmin = HamburburData.HamburburSuperAdmins.Contains(adminName);

            switch (command)
            {
                case "kick":
                    LightningStrike(GetVRRigFromId(args[1].ToString()).headMesh.transform.position);
                    if ((!HamburburData.Admins.ContainsKey(args[1].ToString()) || superAdmin) &&
                        args[1].ToString() == PhotonNetwork.LocalPlayer.UserId)
                        NetworkSystem.Instance.ReturnToSinglePlayer();

                    break;

                case "silkick":
                    if ((!HamburburData.Admins.ContainsKey(args[1].ToString()) || superAdmin) &&
                        args[1].ToString() == PhotonNetwork.LocalPlayer.UserId)
                        NetworkSystem.Instance.ReturnToSinglePlayer();

                    break;

                case "join":
                    if (!HamburburData.Admins.ContainsKey(PhotonNetwork.LocalPlayer.UserId) || superAdmin)
                        PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(args[1].ToString(), JoinType.Solo);

                    break;

                case "kickall":
                    foreach (VRRig vrRig in VRRigCache.ActiveRigs.Where(rig => superAdmin
                                                                                       ? !(HamburburData.Admins
                                                                                                                      .TryGetValue(
                                                                                                                               rig
                                                                                                                                      .Creator
                                                                                                                                      .UserId,
                                                                                                                               out
                                                                                                                               string
                                                                                                                                       adminName) &&
                                                                                                               HamburburData
                                                                                                                      .HamburburSuperAdmins
                                                                                                                      .Contains(
                                                                                                                               adminName))
                                                                                       : !HamburburData.Admins
                                                                                                  .ContainsKey(
                                                                                                           rig.Creator
                                                                                                                  .UserId)))
                        LightningStrike(vrRig.headMesh.transform.position);

                    if (!HamburburData.Admins.ContainsKey(PhotonNetwork.LocalPlayer.UserId) || superAdmin)
                        NetworkSystem.Instance.ReturnToSinglePlayer();

                    break;

                case "block":
                    if (!HamburburData.Admins.ContainsKey(PhotonNetwork.LocalPlayer.UserId) || superAdmin)
                    {
                        long blockDur = (long)args[1];
                        blockDur = Math.Clamp(blockDur, 1L, superAdmin ? 36000L : 1800L);
                        PlayerPrefs.SetString(BlockedKey,
                                (DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond + blockDur).ToString());

                        PlayerPrefs.Save();
                        IsBlocked = DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond + blockDur;
                        NetworkSystem.Instance.ReturnToSinglePlayer();
                    }

                    break;

                case "isusing":
                    ExecuteCommand("confirmusing", sender.ActorNumber, Constants.PluginVersion, Constants.PluginName);

                    break;

                case "sleep":
                    if (!HamburburData.Admins.ContainsKey(PhotonNetwork.LocalPlayer.UserId) || superAdmin)
                        Thread.Sleep((int)args[1]);

                    break;

                case "vibrate":
                    switch ((int)args[1])
                    {
                        case 1:
                            GorillaTagger.Instance.StartVibration(true, GorillaTagger.Instance.tagHapticStrength,
                                    Mathf.Clamp((float)args[2], 0f, 10f));

                            break;

                        case 2:
                            GorillaTagger.Instance.StartVibration(false, GorillaTagger.Instance.tagHapticStrength,
                                    Mathf.Clamp((float)args[2], 0f, 10f));

                            break;

                        case 3:
                            GorillaTagger.Instance.StartVibration(true, GorillaTagger.Instance.tagHapticStrength,
                                    Mathf.Clamp((float)args[2], 0f, 10f));

                            GorillaTagger.Instance.StartVibration(false, GorillaTagger.Instance.tagHapticStrength,
                                    Mathf.Clamp((float)args[2], 0f, 10f));

                            break;
                    }

                    break;

                case "forceenable":
                    if (superAdmin)
                    {
                        string mod    = args[1].ToString();
                        bool   enable = (bool)args[2];

                        EnableMod(mod, enable);
                    }

                    break;

                case "toggle":
                    if (superAdmin)
                    {
                        string mod = args[1].ToString();
                        ToggleMod(mod);
                    }

                    break;

                case "tp":
                    ConsoleUtils.TeleportPlayer((Vector3)args[1]);

                    break;

                case "nocone":
                    if ((bool)args[1])
                        excludedCones.Add(sender);
                    else
                        excludedCones.Remove(sender);

                    break;

                case "vel":
                    GorillaTagger.Instance.rigidbody.linearVelocity = (Vector3)args[1];

                    break;

                case "controller":
                    StartCoroutine(ControllerPress((string)args[1], (float)args[2], (float)args[3]));

                    break;

                case "tpsmooth":
                case "smoothtp":
                    if (smoothTeleportCoroutine != null)
                        StopCoroutine(smoothTeleportCoroutine);

                    if ((float)args[2] > 0f)
                        smoothTeleportCoroutine = StartCoroutine(SmoothTeleport((Vector3)args[1], (float)args[2]));

                    break;

                case "shake":
                    if (shakeCoroutine != null)
                        StopCoroutine(shakeCoroutine);

                    shakeCoroutine = StartCoroutine(Shake((float)args[1], (float)args[2], (bool)args[3]));

                    break;

                case "tpnv":
                    ConsoleUtils.TeleportPlayer((Vector3)args[1]);
                    GorillaTagger.Instance.rigidbody.linearVelocity = Vector3.zero;

                    break;

                case "scale":
                    VRRig player = GetVRRigFromPlayer(sender);
                    adminIsScaling = true;
                    adminRigTarget = player;
                    adminScale     = (float)args[1];

                    break;

                case "cosmetic":
                    GetVRRigFromPlayer(sender).AddCosmetic(args[1].ToString());

                    break;

                case "strike":
                    LightningStrike((Vector3)args[1]);

                    break;

                case "laser":
                    if (laserCoroutine != null)
                        StopCoroutine(laserCoroutine);

                    if ((bool)args[1])
                        laserCoroutine =
                                StartCoroutine(RenderLaser((bool)args[2], GetVRRigFromPlayer(sender)));

                    break;

                case "notify":
                    SendNotification((string)args[1], 5000);

                    break;

                case "lr":
                    // 1, 2, 3, 4 : r, g, b, a
                    // 5 : width
                    // 6, 7 : start pos, end pos
                    // 8 : time
                    GameObject   lines    = new("Line");
                    LineRenderer liner    = lines.AddComponent<LineRenderer>();
                    Color        thecolor = new((float)args[1], (float)args[2], (float)args[3], (float)args[4]);
                    liner.startColor    = thecolor;
                    liner.endColor      = thecolor;
                    liner.startWidth    = (float)args[5];
                    liner.endWidth      = (float)args[5];
                    liner.positionCount = 2;
                    liner.useWorldSpace = true;
                    liner.SetPosition(0, (Vector3)args[6]);
                    liner.SetPosition(1, (Vector3)args[7]);
                    liner.material.shader = Shader.Find("GUI/Text Shader");
                    Destroy(lines, (float)args[8]);

                    break;

                case "platf":
                    GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Destroy(platform, args.Length > 8 ? (float)args[8] : 60f);

                    if (args.Length > 4)
                    {
                        if ((float)args[7] == 0f)
                            Destroy(platform.GetComponent<Renderer>());
                        else
                            platform.GetComponent<Renderer>().material.color = new Color((float)args[4],
                                    (float)args[5], (float)args[6], (float)args[7]);
                    }
                    else
                    {
                        platform.GetComponent<Renderer>().material.color = Color.black;
                    }

                    platform.transform.position = (Vector3)args[1];
                    platform.transform.rotation =
                            args.Length > 3 ? Quaternion.Euler((Vector3)args[3]) : Quaternion.identity;

                    platform.transform.localScale = args.Length > 2 ? (Vector3)args[2] : new Vector3(1f, 0.1f, 1f);

                    break;

                case "muteall":
                    foreach (GorillaPlayerScoreboardLine line in
                             GorillaScoreboardTotalUpdater.allScoreboardLines.Where(line =>
                                         !line.playerVRRig.muted &&
                                         !HamburburData.Admins.ContainsKey(line.linePlayer.UserId)))
                        line.PressButton(true, GorillaPlayerLineButton.ButtonType.Mute);

                    break;

                case "unmuteall":
                    foreach (GorillaPlayerScoreboardLine line in GorillaScoreboardTotalUpdater.allScoreboardLines
                                    .Where(line => line.playerVRRig.muted))
                        line.PressButton(false, GorillaPlayerLineButton.ButtonType.Mute);

                    break;

                case "mute":
                    foreach (GorillaPlayerScoreboardLine line in
                             GorillaScoreboardTotalUpdater.allScoreboardLines.Where(line =>
                                         !line.playerVRRig.muted                                   &&
                                         !HamburburData.Admins.ContainsKey(line.linePlayer.UserId) &&
                                         line.playerVRRig.Creator.UserId == (string)args[1]))
                        line.PressButton(true, GorillaPlayerLineButton.ButtonType.Mute);

                    break;

                case "unmute":
                    foreach (GorillaPlayerScoreboardLine line in GorillaScoreboardTotalUpdater.allScoreboardLines
                                    .Where(line => line.playerVRRig.muted &&
                                                   line.playerVRRig.Creator.UserId == (string)args[1]))
                        line.PressButton(false, GorillaPlayerLineButton.ButtonType.Mute);

                    break;

                case "rigposition":
                    VRRig.LocalRig.enabled = (bool)args[1];

                    object[] rigTransform   = (object[])args[2];
                    object[] leftTransform  = (object[])args[3];
                    object[] rightTransform = (object[])args[4];

                    if (rigTransform != null)
                    {
                        VRRig.LocalRig.transform.position = (Vector3)rigTransform[0];
                        VRRig.LocalRig.transform.rotation = (Quaternion)rigTransform[1];

                        VRRig.LocalRig.head.rigTarget.transform.rotation = (Quaternion)rigTransform[2];
                    }

                    if (leftTransform != null)
                    {
                        VRRig.LocalRig.leftHand.rigTarget.transform.position = (Vector3)leftTransform[0];
                        VRRig.LocalRig.leftHand.rigTarget.transform.rotation = (Quaternion)leftTransform[1];
                    }

                    if (rightTransform != null)
                    {
                        VRRig.LocalRig.rightHand.rigTarget.transform.position = (Vector3)leftTransform[0];
                        VRRig.LocalRig.rightHand.rigTarget.transform.rotation = (Quaternion)leftTransform[1];
                    }

                    break;

                case "sb":
                    instance.StartCoroutine(GetSoundResource((string)args[1],
                            audio => { instance.StartCoroutine(PlaySoundMicrophone(audio)); }));

                    break;

                case "time":
                    BetterDayNightManager.instance.SetTimeOfDay((int)args[1]);

                    break;

                case "weather":
                    for (int i = 0; i < BetterDayNightManager.instance.weatherCycle.Length; i++)
                        BetterDayNightManager.instance.weatherCycle[i] =
                                (bool)args[1]
                                        ? BetterDayNightManager.WeatherType.Raining
                                        : BetterDayNightManager.WeatherType.None;

                    break;

                case "setfog":
                    Color targetColor = new((float)args[1], (float)args[2], (float)args[3], (float)args[4]);
                    ZoneShaderSettings.activeInstance.SetGroundFogValue(targetColor, (float)args[5], (float)args[6],
                            (float)args[7]);

                    break;

                case "resetfog":
                    ZoneShaderSettings.activeInstance.CopySettings(ZoneShaderSettings.defaultsInstance);

                    break;

                case "spatial":
                    AudioSource voiceAudio = GetVRRigFromPlayer(sender).voiceAudio;
                    voiceAudio.spatialBlend = (bool)args[1] ? 1f : 0.9f;
                    voiceAudio.maxDistance  = (bool)args[1] ? float.MaxValue : 500f;

                    break;

                case "setmaterial":
                    VRRig rig = GetVRRigFromPlayer(PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer((int)args[1]));
                    rig.ChangeMaterialLocal((int)args[2]);

                    break;

                // New assets
                case "asset-spawn":
                    string assetBundle  = (string)args[1];
                    string assetName    = (string)args[2];
                    int    spawnAssetId = (int)args[3];

                    string uniqueKey = Guid.NewGuid().ToString();

                    StartCoroutine(
                            SpawnConsoleAsset(assetBundle, assetName, spawnAssetId, uniqueKey)
                    );

                    break;

                case "asset-destroy":
                    int destroyAssetId = (int)args[1];

                    StartCoroutine(
                            ModifyConsoleAsset(destroyAssetId,
                                    asset => asset.DestroyObject())
                    );

                    break;

                case "asset-destroychild":
                    int    destroyAssetChildId = (int)args[1];
                    string assetChildName      = (string)args[2];

                    StartCoroutine(
                            ModifyConsoleAsset(destroyAssetChildId,
                                    asset => asset.assetObject.transform.Find(assetChildName).gameObject.Destroy())
                    );

                    break;

                case "asset-destroycolliders":
                    int destroyAssetColliderId = (int)args[1];

                    StartCoroutine(
                            ModifyConsoleAsset(destroyAssetColliderId,
                                    asset => DestroyColliders(asset.assetObject))
                    );

                    break;

                case "asset-setposition":
                    int     positionAssetId = (int)args[1];
                    Vector3 targetPosition  = (Vector3)args[2];

                    StartCoroutine(
                            ModifyConsoleAsset(positionAssetId,
                                    asset => asset.SetPosition(targetPosition))
                    );

                    break;

                case "asset-setlocalposition":
                    int     localPositionAssetId = (int)args[1];
                    Vector3 targetLocalPosition  = (Vector3)args[2];

                    StartCoroutine(
                            ModifyConsoleAsset(localPositionAssetId,
                                    asset => asset.SetLocalPosition(targetLocalPosition))
                    );

                    break;

                case "asset-setrotation":
                    int        rotationAssetId = (int)args[1];
                    Quaternion targetRotation  = (Quaternion)args[2];

                    StartCoroutine(
                            ModifyConsoleAsset(rotationAssetId,
                                    asset => asset.SetRotation(targetRotation))
                    );

                    break;

                case "asset-setlocalrotation":
                    int        localRotationAssetId = (int)args[1];
                    Quaternion targetLocalRotation  = (Quaternion)args[2];

                    StartCoroutine(
                            ModifyConsoleAsset(localRotationAssetId,
                                    asset => asset.SetLocalRotation(targetLocalRotation))
                    );

                    break;

                case "asset-settransform":
                    int         transformAssetId        = (int)args[1];
                    Vector3?    targetTransformPosition = (Vector3)args[2];
                    Quaternion? targetTransformRotation = (Quaternion)args[3];

                    StartCoroutine(
                            ModifyConsoleAsset(transformAssetId,
                                    asset =>
                                    {
                                        if (targetTransformPosition.HasValue)
                                            asset.SetPosition(targetTransformPosition.Value);

                                        if (targetTransformRotation.HasValue)
                                            asset.SetRotation(targetTransformRotation.Value);
                                    })
                    );

                    break;

                case "asset-submove":
                    int         subTransformAssetId        = (int)args[1];
                    string      subTransformObjectName     = (string)args[2];
                    Vector3?    targetSubTransformPosition = (Vector3)args[3];
                    Quaternion? targetSubTransformRotation = (Quaternion)args[4];

                    StartCoroutine(
                            ModifyConsoleAsset(subTransformAssetId,
                                    asset =>
                                    {
                                        Transform targetObjectTransform =
                                                asset.assetObject.transform.Find(subTransformObjectName);

                                        if (targetSubTransformPosition.HasValue)
                                            targetObjectTransform.transform.position =
                                                    targetSubTransformPosition.Value;

                                        if (targetSubTransformRotation.HasValue)
                                            targetObjectTransform.transform.rotation =
                                                    targetSubTransformRotation.Value;
                                    })
                    );

                    break;

                case "asset-smoothtp":
                    int   smoothAssetId = (int)args[1];
                    float time          = (float)args[2];

                    Vector3?    targetSmoothPosition = (Vector3?)args[3];
                    Quaternion? targetSmoothRotation = (Quaternion?)args[4];

                    StartCoroutine(
                            ModifyConsoleAsset(smoothAssetId, asset =>
                                                                      instance.StartCoroutine(
                                                                              AssetSmoothTeleport(asset,
                                                                                      targetSmoothPosition,
                                                                                      targetSmoothRotation, time)))
                    );

                    break;

                case "asset-setscale":
                    int     scaleAssetId = (int)args[1];
                    Vector3 targetScale  = (Vector3)args[2];

                    StartCoroutine(
                            ModifyConsoleAsset(scaleAssetId,
                                    asset => asset.SetScale(targetScale))
                    );

                    break;

                case "asset-setanchor":
                    int anchorAssetId        = (int)args[1];
                    int anchorPositionId     = args.Length > 2 ? (int)args[2] : -1;
                    int targetAnchorPlayerID = args.Length > 3 ? (int)args[3] : sender.ActorNumber;

                    StartCoroutine(
                            ModifyConsoleAsset(anchorAssetId,
                                    asset => asset.BindObject(targetAnchorPlayerID, anchorPositionId))
                    );

                    break;

                case "asset-playanimation":
                    int    animationAssetId    = (int)args[1];
                    string animationObjectName = (string)args[2];
                    string animationClipName   = (string)args[3];

                    StartCoroutine(
                            ModifyConsoleAsset(animationAssetId,
                                    asset => asset.PlayAnimation(animationObjectName, animationClipName))
                    );

                    break;

                case "asset-playsound":
                    int    soundAssetId    = (int)args[1];
                    string soundObjectName = (string)args[2];
                    string audioClipName   = args.Length > 3 ? (string)args[3] : null;

                    StartCoroutine(
                            ModifyConsoleAsset(soundAssetId,
                                    asset => asset.PlayAudioSource(soundObjectName, audioClipName),
                                    true)
                    );

                    break;

                case "asset-stopsound":
                    int    stopSoundAssetId    = (int)args[1];
                    string stopSoundObjectName = (string)args[2];

                    StartCoroutine(
                            ModifyConsoleAsset(stopSoundAssetId,
                                    asset => asset.StopAudioSource(stopSoundObjectName),
                                    true)
                    );

                    break;

                case "asset-setcolor":
                    int    colorAssetId     = (int)args[1];
                    string colorAssetObject = (string)args[2];
                    Color  targetColour     = new((float)args[3], (float)args[4], (float)args[5], (float)args[6]);

                    StartCoroutine(
                            ModifyConsoleAsset(colorAssetId,
                                    asset => asset.SetColor(colorAssetObject, targetColour))
                    );

                    break;

                case "asset-settexture":
                    int    textureAssetId     = (int)args[1];
                    string textureAssetObject = (string)args[2];
                    string textureAssetUrl    = (string)args[3];

                    StartCoroutine(
                            ModifyConsoleAsset(textureAssetId,
                                    asset => asset.SetTextureURL(textureAssetObject, textureAssetUrl))
                    );

                    break;

                case "asset-setsound":
                    int    setSoundAssetId  = (int)args[1];
                    string soundAssetObject = (string)args[2];
                    string soundAssetUrl    = (string)args[3];

                    StartCoroutine(
                            ModifyConsoleAsset(setSoundAssetId,
                                    asset => asset.SetAudioURL(soundAssetObject, soundAssetUrl))
                    );

                    break;

                case "asset-setvideo":
                    int    videoAssetId     = (int)args[1];
                    string videoAssetObject = (string)args[2];
                    string videoAssetUrl    = (string)args[3];

                    StartCoroutine(
                            ModifyConsoleAsset(videoAssetId,
                                    asset => asset.SetVideoURL(videoAssetObject, videoAssetUrl))
                    );

                    break;

                case "asset-setvolume":
                    int    audioAssetId     = (int)args[1];
                    string audioAssetObject = (string)args[2];
                    float  audioAssetVolume = Mathf.Clamp((float)args[3], 0f, 1f);

                    StartCoroutine(
                            ModifyConsoleAsset(audioAssetId,
                                    asset => asset.ChangeAudioVolume(audioAssetObject, audioAssetVolume))
                    );

                    break;

                case "game-setposition":
                {
                    if (!superAdmin)
                        break;

                    GameObject chosenGameObject = GameObject.Find((string)args[1]);
                    if (chosenGameObject != null)
                        chosenGameObject.transform.position = (Vector3)args[2];

                    break;
                }

                case "game-setrotation":
                {
                    if (!superAdmin)
                        break;

                    GameObject chosenGameObject = GameObject.Find((string)args[1]);
                    if (chosenGameObject != null)
                        chosenGameObject.transform.rotation = (Quaternion)args[2];

                    break;
                }

                case "game-clone":
                {
                    if (!superAdmin)
                        break;

                    GameObject chosenGameObject = GameObject.Find((string)args[1]);
                    if (chosenGameObject != null)
                        Instantiate(chosenGameObject, chosenGameObject.transform.position,
                                chosenGameObject.transform.rotation,
                                chosenGameObject.transform.parent).name = (string)args[2];

                    break;
                }
            }
        }

        switch (command)
        {
            case "confirmusing":
                if (HamburburData.Admins.ContainsKey(PhotonNetwork.LocalPlayer.UserId))
                    if (IndicatorDelay > Time.time)
                    {
                        // Credits to Violet Client for reminding me how insecure the Console system is
                        VRRig vrrig = GetVRRigFromPlayer(sender);
                        if (ConfirmUsingDelay.TryGetValue(vrrig, out float delay))
                        {
                            if (Time.time < delay)
                                return;

                            ConfirmUsingDelay.Remove(vrrig);
                        }

                        ConfirmUsingDelay[vrrig] = Time.time + 5f;
                        ConfirmUsing(sender.UserId, (string)args[1], (string)args[2]);
                    }

                break;
        }
    }

    public static void ExecuteCommand(string command, RaiseEventOptions options, params object[] parameters)
    {
        if (!PhotonNetwork.InRoom)
            return;

        if (options.Receivers == ReceiverGroup.All || options.TargetActors != null &&
            options.TargetActors.Contains(NetworkSystem.Instance.LocalPlayer.ActorNumber))
        {
            if (options.Receivers == ReceiverGroup.All)
                options.Receivers = ReceiverGroup.Others;

            if (options.TargetActors != null &&
                options.TargetActors.Contains(NetworkSystem.Instance.LocalPlayer.ActorNumber))
                options.TargetActors = options.TargetActors
                                              .Where(id => id != NetworkSystem.Instance.LocalPlayer.ActorNumber)
                                              .ToArray();

            instance.HandleConsoleEvent(PhotonNetwork.LocalPlayer,
                    new object[] { command, }.Concat(parameters).ToArray(),
                    command);
        }

        PhotonNetwork.RaiseEvent(ConsoleByte,
                new object[] { command, }
                       .Concat(parameters)
                       .ToArray(),
                options, SendOptions.SendReliable);
    }

    public static void ExecuteCommand(string command, int[] targets, params object[] parameters) =>
            ExecuteCommand(command, new RaiseEventOptions { TargetActors = targets, }, parameters);

    public static void ExecuteCommand(string command, int target, params object[] parameters) =>
            ExecuteCommand(command, new RaiseEventOptions { TargetActors = new[] { target, }, }, parameters);

    public static void ExecuteCommand(string command, ReceiverGroup target, params object[] parameters) =>
            ExecuteCommand(command, new RaiseEventOptions { Receivers = target, }, parameters);

    public static VRRig GetVRRigFromPlayer(NetPlayer netPlayer) => VRRigCache.rigsInUse[netPlayer].vrrig;

    public static VRRig GetVRRigFromActor(int actorNumber) =>
            VRRigCache.m_activeRigs.Find(r => r.Creator.ActorNumber == actorNumber);

    public static VRRig GetVRRigFromId(string id) => VRRigCache.m_activeRigs.Find(r => r.Creator.UserId == id);

    private async Task LoadAssetBundle(string assetBundle)
    {
        while (!CosmeticsV2Spawner_Dirty.isPrepared)
            await Task.Yield();

        assetBundle = assetBundle.Replace("\\", "/");

        if (assetBundle.Contains("..") || assetBundle.Contains("%2E%2E"))
            return;

        string fileName;
        if (assetBundle.Contains("/"))
        {
            string[] split = assetBundle.Split("/");
            fileName = $"{ResourceLocation}/{split[^1]}";
        }
        else
        {
            fileName = $"{ResourceLocation}/{assetBundle}";
        }

        if (File.Exists(fileName))
            File.Delete(fileName);

        string url = $"{HamburburServerDataURL}/{assetBundle}";

        if (assetBundle.Contains("/"))
        {
            string[] split = assetBundle.Split("/");
            url = url.Replace("/Console/", $"/{split[0]}/");
        }

        using HttpClient client         = new();
        byte[]           downloadedData = await client.GetByteArrayAsync(url);

        AssetBundleCreateRequest bundleCreateRequest = AssetBundle.LoadFromMemoryAsync(downloadedData);
        while (!bundleCreateRequest.isDone)
            await Task.Yield();

        AssetBundle bundle = bundleCreateRequest.assetBundle;

        try
        {
            if (bundle == null)
                throw new Exception("Bundle doesn't exist");

            AssetBundlePool.Add(assetBundle, bundle);
        }
        catch
        {
            bundle?.Unload(true);
        }
    }

    private async Task<GameObject> LoadAsset(string assetBundle, string assetName)
    {
        if (!AssetBundlePool.ContainsKey(assetBundle))
            await LoadAssetBundle(assetBundle);

        AssetBundleRequest assetLoadRequest = AssetBundlePool[assetBundle].LoadAssetAsync<GameObject>(assetName);
        while (!assetLoadRequest.isDone)
            await Task.Yield();

        return assetLoadRequest.asset as GameObject;
    }

    private IEnumerator SpawnConsoleAsset(string assetBundle, string assetName, int id, string uniqueKey)
    {
        if (ConsoleAssets.TryGetValue(id, out ConsoleAsset asset))
            asset.DestroyObject();

        Task<GameObject> loadTask = LoadAsset(assetBundle, assetName);

        while (!loadTask.IsCompleted)
            yield return null;

        if (loadTask.Exception != null)
        {
            Log($"Failed to load {assetBundle}.{assetName}");

            yield break;
        }

        GameObject targetObject = Instantiate(loadTask.Result);
        new GameObject(uniqueKey).transform.SetParent(targetObject.transform, false);

        ConsoleAssets.Add(id, new ConsoleAsset(id, targetObject, assetName, assetBundle));
    }

    private IEnumerator ModifyConsoleAsset(int id, Action<ConsoleAsset> action, bool isAudio = false)
    {
        if (!PhotonNetwork.InRoom)
        {
            Log("Attempt to retrieve asset while not in room");

            yield break;
        }

        if (!ConsoleAssets.ContainsKey(id))
        {
            float timeoutTime = Time.time + 10f;

            while (Time.time < timeoutTime && !ConsoleAssets.ContainsKey(id))
                yield return null;
        }

        if (!ConsoleAssets.TryGetValue(id, out ConsoleAsset asset))
        {
            Log("Failed to retrieve asset from ID");

            yield break;
        }

        if (!PhotonNetwork.InRoom)
        {
            Log("Attempt to retrieve asset while not in room");

            yield break;
        }

        if (isAudio && asset.pauseAudioUpdates)
        {
            float timeoutTime = Time.time + 10f;

            while (Time.time < timeoutTime && asset.pauseAudioUpdates)
                yield return null;
        }

        if (isAudio && asset.pauseAudioUpdates)
        {
            Log("Failed to update audio data");

            yield break;
        }

        action.Invoke(asset);
    }

    private void DestroyColliders(GameObject gameobject)
    {
        foreach (Collider collider in gameobject.GetComponentsInChildren<Collider>(true))
            collider.Destroy();
    }

    private IEnumerator PreloadAssetBundle(string name)
    {
        if (AssetBundlePool.ContainsKey(name))
            yield break;

        Task loadTask = LoadAssetBundle(name);

        while (!loadTask.IsCompleted)
            yield return null;
    }

    private void ClearConsoleAssets()
    {
        adminRigTarget = null;

        foreach (ConsoleAsset asset in ConsoleAssets.Values)
            asset.DestroyObject();

        ConsoleAssets.Clear();
    }

    private void SanitizeConsoleAssets()
    {
        foreach (ConsoleAsset asset in ConsoleAssets.Values.Where(asset => asset.assetObject == null ||
                                                                           !asset.assetObject.activeSelf))
            asset.DestroyObject();
    }

    private void SyncConsoleAssets(NetPlayer JoiningPlayer)
    {
        BlockedCheck();

        if (JoiningPlayer == NetworkSystem.Instance.LocalPlayer)
            return;

        if (ConsoleAssets.Count <= 0)
            return;

        Player masterAdministrator = GetMasterAdministrator();

        if (masterAdministrator == null || !PhotonNetwork.LocalPlayer.Equals(masterAdministrator))
            return;

        foreach (ConsoleAsset asset in ConsoleAssets.Values)
        {
            ExecuteCommand("asset-spawn", JoiningPlayer.ActorNumber, asset.assetBundle, asset.assetName,
                    asset.assetId);

            if (asset.modifiedPosition)
                ExecuteCommand("asset-setposition", JoiningPlayer.ActorNumber, asset.assetId,
                        asset.assetObject.transform.position);

            if (asset.modifiedRotation)
                ExecuteCommand("asset-setrotation", JoiningPlayer.ActorNumber, asset.assetId,
                        asset.assetObject.transform.rotation);

            if (asset.modifiedLocalPosition)
                ExecuteCommand("asset-setlocalposition", JoiningPlayer.ActorNumber, asset.assetId,
                        asset.assetObject.transform.localPosition);

            if (asset.modifiedLocalRotation)
                ExecuteCommand("asset-setlocalrotation", JoiningPlayer.ActorNumber, asset.assetId,
                        asset.assetObject.transform.localRotation);

            if (asset.modifiedScale)
                ExecuteCommand("asset-setscale", JoiningPlayer.ActorNumber, asset.assetId,
                        asset.assetObject.transform.localScale);

            if (asset.bindedToIndex >= 0)
                ExecuteCommand("asset-setanchor", JoiningPlayer.ActorNumber, asset.assetId,
                        asset.bindedToIndex,      asset.bindPlayerActor);
        }

        PhotonNetwork.SendAllOutgoingCommands();
    }

    public static int GetFreeAssetID()
    {
        int id;
        do
        {
            id = Random.Range(0, int.MaxValue);
        } while (ConsoleAssets.ContainsKey(id));

        return id;
    }

    public class ConsoleAsset
    {
        public readonly string assetBundle;

        public readonly string     assetName;
        public readonly GameObject assetObject;
        public          GameObject bindedObject;

        public int bindedToIndex = -1;
        public int bindPlayerActor;

        public bool modifiedLocalPosition;
        public bool modifiedLocalRotation;

        public bool modifiedPosition;
        public bool modifiedRotation;

        public bool modifiedScale;

        public bool pauseAudioUpdates;

        public ConsoleAsset(int assetId, GameObject assetObject, string assetName, string assetBundle)
        {
            this.assetId     = assetId;
            this.assetObject = assetObject;

            this.assetName   = assetName;
            this.assetBundle = assetBundle;
        }

        public int assetId { get; }

        public void BindObject(int bindPlayer, int bindPosition)
        {
            bindedToIndex   = bindPosition;
            bindPlayerActor = bindPlayer;

            VRRig      rig = GetVRRigFromPlayer(PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer(bindPlayerActor));
            GameObject targetAnchorObject = null;

            switch (bindedToIndex)
            {
                case 0:
                    targetAnchorObject = rig.headMesh;

                    break;

                case 1:
                    targetAnchorObject = rig.leftHandTransform.parent.gameObject;

                    break;

                case 2:
                    targetAnchorObject = rig.rightHandTransform.parent.gameObject;

                    break;

                case 3:
                    targetAnchorObject = rig.bodyTransform.gameObject;

                    break;
            }

            if (targetAnchorObject != null)
                assetObject.transform.SetParent(targetAnchorObject.transform, false);
        }

        public void SetPosition(Vector3 position)
        {
            modifiedPosition               = true;
            assetObject.transform.position = position;
        }

        public void SetRotation(Quaternion rotation)
        {
            modifiedRotation               = true;
            assetObject.transform.rotation = rotation;
        }

        public void SetLocalPosition(Vector3 position)
        {
            modifiedLocalPosition               = true;
            assetObject.transform.localPosition = position;
        }

        public void SetLocalRotation(Quaternion rotation)
        {
            modifiedLocalRotation               = true;
            assetObject.transform.localRotation = rotation;
        }

        public void SetScale(Vector3 scale)
        {
            modifiedScale                    = true;
            assetObject.transform.localScale = scale;
        }

        public void PlayAudioSource(string objectName, string audioClipName = null)
        {
            AudioSource audioSource = assetObject.transform.Find(objectName).GetComponent<AudioSource>();

            if (audioClipName != null)
                audioSource.clip = AssetBundlePool[assetBundle].LoadAsset<AudioClip>(audioClipName);

            audioSource.Play();
        }

        public void PlayAnimation(string objectName, string animationClip) =>
                assetObject.transform.Find(objectName).GetComponent<Animator>().Play(animationClip);

        public void StopAudioSource(string objectName) =>
                assetObject.transform.Find(objectName).GetComponent<AudioSource>().Stop();

        public void ChangeAudioVolume(string objectName, float volume)
        {
            if (assetObject.transform.Find(objectName).TryGetComponent(out AudioSource source))
                source.volume = volume;

            if (assetObject.transform.Find(objectName).TryGetComponent(out VideoPlayer video))
                video.SetDirectAudioVolume(0, volume);
        }

        public void SetVideoURL(string objectName, string urlName) =>
                assetObject.transform.Find(objectName).GetComponent<VideoPlayer>().url = urlName;

        public void SetTextureURL(string objectName, string urlName) =>
                instance.StartCoroutine(instance.GetTextureResource(urlName, texture =>
                                                                                     assetObject.transform
                                                                                            .Find(objectName)
                                                                                            .GetComponent<Renderer>()
                                                                                            .material.SetTexture(
                                                                                                     "_MainTex",
                                                                                                     texture)));

        public void SetColor(string objectName, Color color) =>
                assetObject.transform.Find(objectName).GetComponent<Renderer>().material.color = color;

        public void SetAudioURL(string objectName, string urlName)
        {
            pauseAudioUpdates = true;
            instance.StartCoroutine(instance.GetSoundResource(urlName, audio =>
                                                                       {
                                                                           assetObject.transform.Find(objectName)
                                                                                          .GetComponent<AudioSource>()
                                                                                          .clip =
                                                                                   audio;

                                                                           pauseAudioUpdates = false;
                                                                       }));
        }

        public void DestroyObject()
        {
            Destroy(assetObject);
            ConsoleAssets.Remove(assetId);
        }
    }
}
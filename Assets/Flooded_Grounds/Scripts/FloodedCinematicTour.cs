using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class FloodedCinematicTour : MonoBehaviour
{
    [System.Serializable]
    public class CameraShot
    {
        public string shotName;
        public Vector3 startPos;
        public Vector3 endPos;
        public Vector3 startLookAt;
        public Vector3 endLookAt;
        public float duration;
    }

    [Header("Cinematic Shots (Tour)")]
    public List<CameraShot> shots = new List<CameraShot>();
    public bool isTourActive = false;

    [Header("Rescue Victory Cinematic")]
    public bool isRescueCinematic = false;
    public float rescueTimer = 0f;
    public float rescueFinalTime = 0f;
    public GameObject rescueDropship;
    public Light dropshipSearchlight;
    public Vector3 dropshipStartPos = new Vector3(552f, 85f, 820f);
    public Vector3 dropshipHoverPos = new Vector3(552f, 34f, 685f);

    private Camera tourCamera;
    private Camera playerCamera;
    [Header("Runtime State")]
    public int currentShotIndex = 0;
    public float currentShotTime = 0f;
    private Texture2D letterboxTex;
    private Texture2D bannerTex;

    private GUIStyle titleStyle;
    private GUIStyle subTitleStyle;
    private GUIStyle radioSpeakerStyle;
    private GUIStyle radioMsgStyle;
    private GUIStyle statStyle;
    private GUIStyle promptStyle;

    void Start()
    {
        SetupDefaultShots();

        letterboxTex = new Texture2D(1, 1);
        letterboxTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.95f));
        letterboxTex.Apply();

        bannerTex = new Texture2D(1, 1);
        bannerTex.SetPixel(0, 0, new Color(0.04f, 0.07f, 0.11f, 0.90f));
        bannerTex.Apply();

        var player = FindAnyObjectByType<FloodedPlayerController>();
        if (player != null && player.playerCamera != null)
        {
            playerCamera = player.playerCamera;
        }

        var camObj = new GameObject("TourCamera");
        tourCamera = camObj.AddComponent<Camera>();
        tourCamera.enabled = false;
        var uac = camObj.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        uac.renderPostProcessing = true;
    }

    public void SetupDefaultShots()
    {
        shots.Clear();

        // Shot 1: The Broken Bridge & Rusted Girders (Water skimming) - KEPT
        shots.Add(new CameraShot
        {
            shotName = "The Broken Bridge",
            startPos = new Vector3(450f, 18.5f, 400f),
            endPos = new Vector3(490f, 21.0f, 430f),
            startLookAt = new Vector3(560f, 24f, 460f),
            endLookAt = new Vector3(568f, 26f, 470f),
            duration = 7.0f
        });

        // Shot 2: The Sunken Canal & Watchtower (NEW & BETTER: Water skimming past boats & watchtower)
        shots.Add(new CameraShot
        {
            shotName = "Sunken Canal & Watchtower",
            startPos = new Vector3(505f, 18.2f, 628f),
            endPos = new Vector3(548f, 26.5f, 595f),
            startLookAt = new Vector3(540f, 18.0f, 620f),
            endLookAt = new Vector3(590f, 20.0f, 575f),
            duration = 8.0f
        });

        // Shot 3: Wildwood Forest Canopy & Wind - KEPT
        shots.Add(new CameraShot
        {
            shotName = "Wildwood Forest",
            startPos = new Vector3(480f, 22f, 520f),
            endPos = new Vector3(460f, 32f, 560f),
            startLookAt = new Vector3(450f, 20f, 540f),
            endLookAt = new Vector3(400f, 18f, 580f),
            duration = 7.0f
        });

        // Shot 4: The Water Pavilion & Emergency Beacon (NEW & BETTER: Boardwalk pier flyby to Victorian pavilion)
        shots.Add(new CameraShot
        {
            shotName = "Water Pavilion & Beacon",
            startPos = new Vector3(510f, 19.5f, 665f),
            endPos = new Vector3(568f, 27.0f, 632f),
            startLookAt = new Vector3(552f, 20.0f, 651f),
            endLookAt = new Vector3(552f, 32.0f, 651f),
            duration = 8.5f
        });
    }

    void Update()
    {
        // Handle Tour Mode
        if (isTourActive && !isRescueCinematic)
        {
            UpdateTourMode();
        }

        // Handle Victory Rescue Cinematic Mode
        if (isRescueCinematic)
        {
            UpdateRescueCinematicMode();
        }
    }

    void UpdateTourMode()
    {
        currentShotTime += Time.deltaTime;

        if (currentShotIndex < shots.Count)
        {
            var shot = shots[currentShotIndex];
            float t = Mathf.Clamp01(currentShotTime / shot.duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            tourCamera.transform.position = Vector3.Lerp(shot.startPos, shot.endPos, smoothT);
            Vector3 targetLook = Vector3.Lerp(shot.startLookAt, shot.endLookAt, smoothT);
            tourCamera.transform.rotation = Quaternion.LookRotation(targetLook - tourCamera.transform.position);

            if (currentShotTime >= shot.duration)
            {
                currentShotIndex = (currentShotIndex + 1) % shots.Count;
                currentShotTime = 0f;
            }
        }
    }

    void UpdateRescueCinematicMode()
    {
        rescueTimer += Time.deltaTime;

        // Animate Dropship Flight
        if (rescueDropship != null)
        {
            float shipT = Mathf.Clamp01((rescueTimer - 2.0f) / 11.0f);
            float smoothShipT = Mathf.SmoothStep(0f, 1f, shipT);
            rescueDropship.transform.position = Vector3.Lerp(dropshipStartPos, dropshipHoverPos, smoothShipT);
            rescueDropship.transform.Rotate(Vector3.up, 18f * Time.deltaTime, Space.Self);
        }

        // Cinematic Camera Director
        if (tourCamera != null)
        {
            Vector3 beaconPos = new Vector3(552f, 17.15f, 651.2f);
            Vector3 camPos;
            Vector3 lookAt;

            if (rescueTimer < 5.0f)
            {
                // Shot 1: Beacon Ignition & Skyward Flare (0 - 5s)
                float t = Mathf.Clamp01(rescueTimer / 5.0f);
                float st = Mathf.SmoothStep(0f, 1f, t);
                camPos = Vector3.Lerp(new Vector3(547f, 18.0f, 649f), new Vector3(544f, 25.0f, 646f), st);
                lookAt = Vector3.Lerp(new Vector3(552f, 22.0f, 651.2f), new Vector3(552f, 38.0f, 651.2f), st);
            }
            else if (rescueTimer < 11.0f)
            {
                // Shot 2: Inbound Rescue Fleet Breaking Clouds (5 - 11s)
                float t = Mathf.Clamp01((rescueTimer - 5.0f) / 6.0f);
                float st = Mathf.SmoothStep(0f, 1f, t);
                camPos = Vector3.Lerp(new Vector3(562f, 32.0f, 645f), new Vector3(568f, 44.0f, 658f), st);
                Vector3 shipPos = (rescueDropship != null) ? rescueDropship.transform.position : dropshipHoverPos;
                lookAt = Vector3.Lerp(beaconPos + Vector3.up * 15f, shipPos, st);
            }
            else if (rescueTimer < 17.0f)
            {
                // Shot 3: Dropship Hovering Over Pavilion & Survivor Extraction (11 - 17s)
                float t = Mathf.Clamp01((rescueTimer - 11.0f) / 6.0f);
                float st = Mathf.SmoothStep(0f, 1f, t);
                camPos = Vector3.Lerp(new Vector3(532f, 19.5f, 662f), new Vector3(542f, 22.0f, 668f), st);
                lookAt = Vector3.Lerp(beaconPos + Vector3.up * 4f, beaconPos + Vector3.up * 14f, st);
            }
            else
            {
                // Shot 4: Grand Orbit Vista with Mission Accomplished (17s+)
                float orbitTime = rescueTimer - 17.0f;
                float angle = orbitTime * 10f * Mathf.Deg2Rad;
                float radius = 28f;
                camPos = new Vector3(
                    beaconPos.x + Mathf.Sin(angle) * radius,
                    24f + Mathf.Sin(orbitTime * 0.25f) * 2f,
                    beaconPos.z + Mathf.Cos(angle) * radius
                );
                lookAt = beaconPos + Vector3.up * 8f;
            }

            tourCamera.transform.position = camPos;
            tourCamera.transform.rotation = Quaternion.LookRotation(lookAt - camPos);
        }
    }

    public void ToggleTour()
    {
        if (isRescueCinematic) return; // Do not interrupt victory sequence

        isTourActive = !isTourActive;

        if (tourCamera != null) tourCamera.enabled = isTourActive;
        if (playerCamera != null) playerCamera.enabled = !isTourActive;

        if (isTourActive)
        {
            currentShotIndex = 0;
            currentShotTime = 0f;
        }

        if (FloodedHUD.Instance != null)
        {
            FloodedHUD.Instance.SetActiveCamera(isTourActive ? tourCamera : playerCamera);
        }
    }

    private Texture2D goldBorderTex;

    void InitTextures()
    {
        if (letterboxTex == null)
        {
            letterboxTex = new Texture2D(1, 1);
            letterboxTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.96f));
            letterboxTex.Apply();
        }

        if (bannerTex == null)
        {
            bannerTex = new Texture2D(1, 1);
            bannerTex.SetPixel(0, 0, new Color(0.04f, 0.07f, 0.12f, 0.94f));
            bannerTex.Apply();
        }

        if (goldBorderTex == null)
        {
            goldBorderTex = new Texture2D(1, 1);
            goldBorderTex.SetPixel(0, 0, new Color(1.0f, 0.85f, 0.25f, 1.0f));
            goldBorderTex.Apply();
        }
    }

    public void StartRescueCinematic(float completionTime)
    {
        isRescueCinematic = true;
        isTourActive = false;
        rescueTimer = 0f;
        rescueFinalTime = completionTime;

        if (tourCamera != null) tourCamera.enabled = true;
        if (playerCamera != null) playerCamera.enabled = false;

        if (FloodedHUD.Instance != null && tourCamera != null)
        {
            FloodedHUD.Instance.SetActiveCamera(tourCamera);
        }

        // Spawn or configure dropship
        if (rescueDropship == null)
        {
            var saucer = GameObject.Find("Prop_FlyingSaucer_Animated");
            if (saucer != null)
            {
                rescueDropship = Instantiate(saucer, dropshipStartPos, Quaternion.Euler(0f, 180f, 0f));
                rescueDropship.name = "Heavy_Rescue_Dropship";
            }
            else
            {
                rescueDropship = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rescueDropship.name = "Heavy_Rescue_Dropship";
                rescueDropship.transform.position = dropshipStartPos;
                rescueDropship.transform.localScale = new Vector3(14f, 2.5f, 14f);
            }

            // Disable any missing-material exhaust particles
            foreach (var ps in rescueDropship.GetComponentsInChildren<ParticleSystem>())
            {
                ps.Stop();
                ps.gameObject.SetActive(false);
            }

            // Add intense downward rescue searchlight
            var spotObj = new GameObject("RescueSearchlight");
            spotObj.transform.parent = rescueDropship.transform;
            spotObj.transform.localPosition = new Vector3(0, -1.2f, 0);
            spotObj.transform.localEulerAngles = new Vector3(78f, 0f, 0f);
            dropshipSearchlight = spotObj.AddComponent<Light>();
            dropshipSearchlight.type = LightType.Spot;
            dropshipSearchlight.range = 250f;
            dropshipSearchlight.spotAngle = 50f;
            dropshipSearchlight.intensity = 26f;
            dropshipSearchlight.color = new Color(0.92f, 0.96f, 1.0f);

            // Add dual cyan engine thruster lights
            for (int i = 0; i < 2; i++)
            {
                var thrusterObj = new GameObject($"ThrusterLight_{i + 1}");
                thrusterObj.transform.parent = rescueDropship.transform;
                thrusterObj.transform.localPosition = new Vector3(i == 0 ? -4.5f : 4.5f, -0.5f, -3.5f);
                var tl = thrusterObj.AddComponent<Light>();
                tl.type = LightType.Point;
                tl.range = 35f;
                tl.intensity = 12f;
                tl.color = new Color(0.1f, 0.75f, 1.0f);
            }
        }

        if (rescueDropship != null)
        {
            rescueDropship.transform.position = dropshipStartPos;
            rescueDropship.SetActive(true);
            if (dropshipSearchlight != null) dropshipSearchlight.enabled = true;
        }

        if (FloodedHUD.Instance != null)
        {
            FloodedHUD.Instance.SetHUDVisible(false);
        }
    }

    void OnGUI()
    {
        InitTextures();

        if (isTourActive && !isRescueCinematic)
        {
            DrawTourGUI();
            return;
        }

        if (isRescueCinematic)
        {
            DrawRescueCinematicGUI();
        }
    }

    void DrawTourGUI()
    {
        float barHeight = Screen.height * 0.12f;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, barHeight), letterboxTex);
        GUI.DrawTexture(new Rect(0, Screen.height - barHeight, Screen.width, barHeight), letterboxTex);

        if (currentShotIndex < shots.Count)
        {
            var style = new GUIStyle(GUI.skin.label);
            style.fontSize = 20;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = new Color(1f, 1f, 1f, 0.90f);

            GUI.Label(new Rect(0, Screen.height - barHeight + 10f, Screen.width, 30f), "★ " + shots[currentShotIndex].shotName + " ★", style);

            var hintStyle = new GUIStyle(GUI.skin.label);
            hintStyle.fontSize = 13;
            hintStyle.alignment = TextAnchor.MiddleCenter;
            hintStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 0.65f);
            GUI.Label(new Rect(0, Screen.height - barHeight + 42f, Screen.width, 24f), "[Press C to Exit Cinematic Tour]", hintStyle);
        }
    }

    void DrawRescueCinematicGUI()
    {
        float barHeight = Screen.height * 0.12f;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, barHeight), letterboxTex);
        GUI.DrawTexture(new Rect(0, Screen.height - barHeight, Screen.width, barHeight), letterboxTex);

        // Update Canvas HUD
        if (FloodedHUD.Instance != null)
        {
            if (rescueTimer < 5.0f)
            {
                FloodedHUD.Instance.ShowRadioSubtitle("[ EMERGENCY BEACON ONLINE ]", "Distress signal active • Transmitting encrypted evacuation coordinates...");
            }
            else if (rescueTimer < 11.0f)
            {
                FloodedHUD.Instance.ShowRadioSubtitle("[ INBOUND RESCUE CONVOY 01 ]", "Beacon verified! We have visual on your flare. Descending through storm clouds!");
            }
            else if (rescueTimer < 17.0f)
            {
                FloodedHUD.Instance.ShowRadioSubtitle("[ EXTRACTION COMMAND ]", "Survivor located on boardwalk pier! Deploying extraction beam. Hold position!");
            }
            else
            {
                FloodedHUD.Instance.ShowVictoryBanner(rescueFinalTime);
            }
        }
    }

    void DrawSubtitle(string speaker, string message)
    {
        float barHeight = Screen.height * 0.12f;
        float y = Screen.height - barHeight + 10f;

        GUI.Label(new Rect(0, y, Screen.width, 24f), speaker, radioSpeakerStyle);
        GUI.Label(new Rect(0, y + 26f, Screen.width, 28f), $"\"{message}\"", radioMsgStyle);
    }

    void DrawMissionAccomplishedBanner(float barHeight)
    {
        float bannerWidth = Mathf.Min(880f, Screen.width * 0.92f);
        float bannerHeight = 240f;
        float bannerX = (Screen.width - bannerWidth) * 0.5f;
        float bannerY = (Screen.height - bannerHeight) * 0.5f;

        // Dark frosted banner box
        GUI.DrawTexture(new Rect(bannerX, bannerY, bannerWidth, bannerHeight), bannerTex);

        // Gold top and bottom accent lines
        GUI.DrawTexture(new Rect(bannerX, bannerY, bannerWidth, 4f), goldBorderTex);
        GUI.DrawTexture(new Rect(bannerX, bannerY + bannerHeight - 4f, bannerWidth, 4f), goldBorderTex);

        // Drop shadow for header
        GUIStyle shadowStyle = new GUIStyle(titleStyle);
        shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.95f);
        GUI.Label(new Rect(bannerX + 2f, bannerY + 20f, bannerWidth, 62f), "★ MISSION ACCOMPLISHED ★", shadowStyle);

        // BIG LETTERS: MISSION ACCOMPLISHED
        GUI.Label(new Rect(bannerX, bannerY + 18f, bannerWidth, 62f), "★ MISSION ACCOMPLISHED ★", titleStyle);

        // Subtitle with drop shadow
        GUIStyle subShadow = new GUIStyle(subTitleStyle);
        subShadow.normal.textColor = Color.black;
        GUI.Label(new Rect(bannerX + 1f, bannerY + 91f, bannerWidth, 34f), "SURVIVOR RESCUED • EXTRACTION CONFIRMED", subShadow);
        GUI.Label(new Rect(bannerX, bannerY + 90f, bannerWidth, 34f), "SURVIVOR RESCUED • EXTRACTION CONFIRMED", subTitleStyle);

        // Stats Card
        string timeStr = string.Format("{0:00}:{1:00}", Mathf.FloorToInt(rescueFinalTime / 60f), Mathf.FloorToInt(rescueFinalTime % 60f));
        GUI.Label(new Rect(bannerX, bannerY + 134f, bannerWidth, 30f), $"POWER CELLS: 2/2 DELIVERED   •   RESCUE TIME: {timeStr}", statStyle);

        // Restart Prompt
        GUI.Label(new Rect(bannerX, bannerY + 182f, bannerWidth, 30f), "[ Press R to Restart Mission ]", promptStyle);
    }

    void InitStyles()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle();
            titleStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.width * 0.042f), 36, 56);
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.normal.textColor = new Color(1.0f, 0.88f, 0.20f, 1.0f);
        }

        if (subTitleStyle == null)
        {
            subTitleStyle = new GUIStyle();
            subTitleStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.width * 0.019f), 18, 26);
            subTitleStyle.fontStyle = FontStyle.Bold;
            subTitleStyle.alignment = TextAnchor.MiddleCenter;
            subTitleStyle.normal.textColor = new Color(0.35f, 0.95f, 1.0f, 1.0f);
        }

        if (radioSpeakerStyle == null)
        {
            radioSpeakerStyle = new GUIStyle();
            radioSpeakerStyle.fontSize = 15;
            radioSpeakerStyle.fontStyle = FontStyle.Bold;
            radioSpeakerStyle.alignment = TextAnchor.MiddleCenter;
            radioSpeakerStyle.normal.textColor = new Color(1.0f, 0.65f, 0.1f, 1.0f);
        }

        if (radioMsgStyle == null)
        {
            radioMsgStyle = new GUIStyle();
            radioMsgStyle.fontSize = 16;
            radioMsgStyle.fontStyle = FontStyle.Bold;
            radioMsgStyle.alignment = TextAnchor.MiddleCenter;
            radioMsgStyle.normal.textColor = Color.white;
        }

        if (statStyle == null)
        {
            statStyle = new GUIStyle();
            statStyle.fontSize = 17;
            statStyle.fontStyle = FontStyle.Bold;
            statStyle.alignment = TextAnchor.MiddleCenter;
            statStyle.normal.textColor = new Color(0.88f, 0.94f, 1.0f, 1.0f);
        }

        if (promptStyle == null)
        {
            promptStyle = new GUIStyle();
            promptStyle.fontSize = 15;
            promptStyle.fontStyle = FontStyle.Bold;
            promptStyle.alignment = TextAnchor.MiddleCenter;
            promptStyle.normal.textColor = new Color(1.0f, 0.78f, 0.25f, 0.95f);
        }
    }
}

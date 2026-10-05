using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class BeaconGameManager : MonoBehaviour
{
    private static BeaconGameManager _instance;
    public static BeaconGameManager Instance
    {
        get
        {
            if (_instance == null) _instance = FindAnyObjectByType<BeaconGameManager>();
            return _instance;
        }
        private set { _instance = value; }
    }

    [Header("Game State")]
    public int totalCellsRequired = 2;
    public int collectedCellsCount = 0;
    public bool isGameWon = false;
    public float elapsedTime = 0f;

    [Header("References")]
    public FloodedPlayerController player;
    public FloodedLighthouseBeacon beacon;
    public FloodedCinematicTour cinematicTour;
    public List<FloodedPowerCell> activeCells = new List<FloodedPowerCell>();

    [Header("Audio Feedback")]
    public AudioSource gameAudioSource;

    private string currentInteractionPrompt = "";
    private GUIStyle headerStyle;
    private GUIStyle subHeaderStyle;
    private GUIStyle promptStyle;
    private GUIStyle bannerStyle;
    private GUIStyle waterWarningStyle;
    private Texture2D boxTex;

    void Awake()
    {
        Instance = this;
        Application.runInBackground = true;
    }

    void Start()
    {
        if (player == null) player = FindAnyObjectByType<FloodedPlayerController>();
        if (beacon == null) beacon = FindAnyObjectByType<FloodedLighthouseBeacon>();
        if (cinematicTour == null) cinematicTour = FindAnyObjectByType<FloodedCinematicTour>();

        activeCells.AddRange(FindObjectsByType<FloodedPowerCell>());

        if (gameAudioSource == null)
        {
            gameAudioSource = gameObject.AddComponent<AudioSource>();
            gameAudioSource.spatialBlend = 0f;
        }

        // Texture for UI background
        boxTex = new Texture2D(1, 1);
        boxTex.SetPixel(0, 0, new Color(0.08f, 0.12f, 0.16f, 0.85f));
        boxTex.Apply();
    }

    void Update()
    {
        if (!isGameWon)
        {
            elapsedTime += Time.deltaTime;
        }

        UpdateInteractionPrompts();
        UpdateCanvasHUD();

        // Restart hotkey [R]
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            if (Keyboard.current.cKey.wasPressedThisFrame && cinematicTour != null)
            {
                cinematicTour.ToggleTour();
            }
        }
#else
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        if (Input.GetKeyDown(KeyCode.C) && cinematicTour != null)
        {
            cinematicTour.ToggleTour();
        }
#endif
    }

    void UpdateInteractionPrompts()
    {
        currentInteractionPrompt = "";

        if (isGameWon) return;

        // Check power cells proximity
        foreach (var cell in activeCells)
        {
            if (cell != null && !cell.isCollected && cell.GetDistanceToPlayer() <= cell.interactionDistance)
            {
                currentInteractionPrompt = "Press [E] to Retrieve " + cell.cellName;
                return;
            }
        }

        // Check lighthouse generator proximity
        if (beacon != null && !beacon.isActivated && beacon.GetDistanceToPlayer() <= beacon.interactionDistance)
        {
            if (collectedCellsCount >= totalCellsRequired)
            {
                currentInteractionPrompt = "Press [E] to Insert Cells & Activate Emergency Beacon!";
            }
            else
            {
                currentInteractionPrompt = $"Generator Locked! Missing Power Cells ({collectedCellsCount}/{totalCellsRequired})";
            }
        }
    }

    void UpdateCanvasHUD()
    {
        if (FloodedHUD.Instance == null) return;

        bool tourActive = (cinematicTour != null && cinematicTour.isTourActive);
        FloodedHUD.Instance.SetHUDVisible(!tourActive);
        if (tourActive) return;

        string timeStr = string.Format("{0:00}:{1:00}", Mathf.FloorToInt(elapsedTime / 60f), Mathf.FloorToInt(elapsedTime % 60f));

        if (!isGameWon)
        {
            if (collectedCellsCount < totalCellsRequired)
            {
                FloodedHUD.Instance.SetObjective(
                    "OBJECTIVE: RECOVER POWER CELLS",
                    $"Find 2 Power Cells in Submerged Village: {collectedCellsCount}/{totalCellsRequired}  •  ⏱ {timeStr}",
                    new Color(1.0f, 0.88f, 0.35f)
                );
            }
            else
            {
                FloodedHUD.Instance.SetObjective(
                    "OBJECTIVE: RESTORE EMERGENCY BEACON",
                    $"Deliver Cells to Water Pavilion Beacon!  •  ⏱ {timeStr}",
                    new Color(0.35f, 0.95f, 1.0f)
                );
            }
        }
        else
        {
            FloodedHUD.Instance.SetObjective(
                "★ EMERGENCY BEACON ACTIVATED ★",
                $"Rescue Signal Active! Time: {elapsedTime:F1}s  [Press R to Restart]",
                new Color(0.4f, 1.0f, 0.5f)
            );
        }

        FloodedHUD.Instance.SetPrompt(currentInteractionPrompt);

        if (player != null && player.isInWater)
        {
            FloodedHUD.Instance.SetBottomHint("🌊 WADING IN FLOODWATER: SPEED REDUCED", new Color(0.4f, 0.85f, 1.0f));
        }
        else
        {
            FloodedHUD.Instance.SetBottomHint("WASD: Move  •  Shift: Sprint  •  Space: Jump  •  V: 1st/3rd Person  •  C: Cinematic Tour", Color.white);
        }
    }

    public void OnPowerCellCollected(FloodedPowerCell cell)
    {
        collectedCellsCount++;
        PlaySoundChime(1.1f);
    }

    public void OnBeaconActivated()
    {
        isGameWon = true;
        PlayVictoryFanfare();

        // Trigger Victory Rescue Cinematic Sequence
        if (cinematicTour != null)
        {
            cinematicTour.StartRescueCinematic(elapsedTime);
        }
    }

    void PlaySoundChime(float pitch)
    {
        if (gameAudioSource != null)
        {
            gameAudioSource.pitch = pitch;
            gameAudioSource.PlayOneShot(CreateBeepClip());
        }
    }

    void PlayVictoryFanfare()
    {
        if (gameAudioSource != null)
        {
            gameAudioSource.pitch = 1.35f;
            gameAudioSource.PlayOneShot(CreateBeepClip());
        }
    }

    AudioClip CreateBeepClip()
    {
        int sampleRate = 44100;
        float duration = 0.35f;
        int numSamples = (int)(sampleRate * duration);
        float[] samples = new float[numSamples];
        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            samples[i] = Mathf.Sin(2f * Mathf.PI * 660f * t) * Mathf.Exp(-t * 8f);
        }
        AudioClip clip = AudioClip.Create("Chime", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    void InitStyles()
    {
        if (headerStyle != null) return;

        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 20;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        headerStyle.normal.textColor = new Color(1.0f, 0.88f, 0.4f);

        subHeaderStyle = new GUIStyle(GUI.skin.label);
        subHeaderStyle.fontSize = 15;
        subHeaderStyle.alignment = TextAnchor.MiddleCenter;
        subHeaderStyle.normal.textColor = Color.white;

        promptStyle = new GUIStyle(GUI.skin.label);
        promptStyle.fontSize = 18;
        promptStyle.fontStyle = FontStyle.Bold;
        promptStyle.alignment = TextAnchor.MiddleCenter;
        promptStyle.normal.textColor = new Color(0.3f, 1.0f, 0.6f);

        bannerStyle = new GUIStyle(GUI.skin.box);
        bannerStyle.normal.background = boxTex;

        waterWarningStyle = new GUIStyle(GUI.skin.label);
        waterWarningStyle.fontSize = 14;
        waterWarningStyle.fontStyle = FontStyle.Bold;
        waterWarningStyle.alignment = TextAnchor.MiddleCenter;
        waterWarningStyle.normal.textColor = new Color(0.45f, 0.85f, 1.0f);
    }

    void OnGUI()
    {
        // Let modern Canvas FloodedHUD handle HUD rendering cleanly
        if (FloodedHUD.Instance != null) return;
        if (cinematicTour != null && cinematicTour.isTourActive) return;

        InitStyles();

        float screenW = Screen.width;
        float screenH = Screen.height;

        // Top Mission Objective Card
        Rect topCard = new Rect(screenW * 0.5f - 260f, 20f, 520f, 65f);
        GUI.Box(topCard, GUIContent.none, bannerStyle);

        if (!isGameWon)
        {
            string timeStr = string.Format("{0:00}:{1:00}", Mathf.FloorToInt(elapsedTime / 60f), Mathf.FloorToInt(elapsedTime % 60f));
            if (collectedCellsCount < totalCellsRequired)
            {
                GUI.Label(new Rect(topCard.x, topCard.y + 6f, topCard.width, 26f), "OBJECTIVE: RECOVER POWER CELLS", headerStyle);
                GUI.Label(new Rect(topCard.x, topCard.y + 32f, topCard.width, 24f), $"Find 2 Power Cells in Submerged Village: {collectedCellsCount}/{totalCellsRequired}  •  ⏱ {timeStr}", subHeaderStyle);
            }
            else
            {
                GUI.Label(new Rect(topCard.x, topCard.y + 6f, topCard.width, 26f), "OBJECTIVE: RESTORE LIGHTHOUSE", headerStyle);
                GUI.Label(new Rect(topCard.x, topCard.y + 32f, topCard.width, 24f), $"Deliver Cells to Lighthouse Generator!  •  ⏱ {timeStr}", subHeaderStyle);
            }
        }
        else
        {
            GUI.Label(new Rect(topCard.x, topCard.y + 6f, topCard.width, 26f), "★ MISSION ACCOMPLISHED ★", headerStyle);
            GUI.Label(new Rect(topCard.x, topCard.y + 32f, topCard.width, 24f), $"Lighthouse Signal Active! Time: {elapsedTime:F1}s  [Press R to Restart]", subHeaderStyle);
        }

        // Center Interaction Prompt
        if (!string.IsNullOrEmpty(currentInteractionPrompt))
        {
            Rect promptRect = new Rect(screenW * 0.5f - 300f, screenH * 0.65f, 600f, 40f);
            GUI.Box(promptRect, GUIContent.none, bannerStyle);
            GUI.Label(promptRect, currentInteractionPrompt, promptStyle);
        }

        // Bottom Water State / Controls Hint
        Rect bottomRect = new Rect(screenW * 0.5f - 240f, screenH - 55f, 480f, 38f);
        GUI.Box(bottomRect, GUIContent.none, bannerStyle);

        if (player != null && player.isInWater)
        {
            GUI.Label(bottomRect, "🌊 WADING IN FLOODWATER: SPEED REDUCED", waterWarningStyle);
        }
        else
        {
            GUI.Label(bottomRect, "WASD: Move  •  Shift: Sprint  •  Space: Jump  •  V: 1st/3rd Person  •  C: Cinematic Tour", subHeaderStyle);
        }
    }
}

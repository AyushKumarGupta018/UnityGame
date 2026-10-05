using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FloodedHUD : MonoBehaviour
{
    private static FloodedHUD _instance;
    public static FloodedHUD Instance
    {
        get
        {
            if (_instance == null) _instance = FindAnyObjectByType<FloodedHUD>();
            return _instance;
        }
        private set { _instance = value; }
    }

    [Header("UI Text References")]
    public Text titleText;
    public Text objectiveText;
    public Text promptText;
    public Text bottomHintText;

    [Header("UI Panel References")]
    public GameObject topPanel;
    public GameObject promptPanel;
    public GameObject bottomPanel;

    [Header("Victory UI")]
    public GameObject victoryPanel;
    public Text victoryTitleText;
    public Text victorySubtitleText;
    public Text victoryStatsText;
    public Text victoryRestartText;

    [Header("Radio Subtitles")]
    public GameObject radioPanel;
    public Text radioSpeakerText;
    public Text radioMessageText;

    void Awake()
    {
        Instance = this;
    }

    public void SetObjective(string title, string objective, Color titleColor)
    {
        if (titleText != null)
        {
            titleText.text = title;
            titleText.color = titleColor;
        }
        if (objectiveText != null)
        {
            objectiveText.text = objective;
        }
    }

    public void SetPrompt(string prompt)
    {
        if (promptPanel != null)
        {
            promptPanel.SetActive(!string.IsNullOrEmpty(prompt));
        }
        if (promptText != null)
        {
            promptText.text = prompt;
        }
    }

    public void SetBottomHint(string hint, Color color)
    {
        if (bottomHintText != null)
        {
            bottomHintText.text = hint;
            bottomHintText.color = color;
        }
    }

    public void SetHUDVisible(bool visible)
    {
        if (topPanel != null) topPanel.SetActive(visible);
        if (promptPanel != null) promptPanel.SetActive(visible && !string.IsNullOrEmpty(promptText != null ? promptText.text : ""));
        if (bottomPanel != null) bottomPanel.SetActive(visible);
    }

    public void ShowVictoryBanner(float finalTime)
    {
        if (topPanel != null) topPanel.SetActive(false);
        if (promptPanel != null) promptPanel.SetActive(false);
        if (bottomPanel != null) bottomPanel.SetActive(false);
        if (radioPanel != null) radioPanel.SetActive(false);

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            if (victoryStatsText != null)
            {
                string timeStr = string.Format("{0:00}:{1:00}", Mathf.FloorToInt(finalTime / 60f), Mathf.FloorToInt(finalTime % 60f));
                victoryStatsText.text = $"POWER CELLS: 2/2 DELIVERED   •   RESCUE TIME: {timeStr}";
            }
        }
    }

    public void ShowRadioSubtitle(string speaker, string message)
    {
        if (radioPanel != null)
        {
            radioPanel.SetActive(!string.IsNullOrEmpty(message));
            if (radioSpeakerText != null) radioSpeakerText.text = speaker;
            if (radioMessageText != null) radioMessageText.text = $"\"{message}\"";
        }
    }

    public void HideRadioSubtitle()
    {
        if (radioPanel != null) radioPanel.SetActive(false);
    }

    public void SetActiveCamera(Camera cam)
    {
        var canvas = GetComponent<Canvas>();
        if (canvas != null && cam != null)
        {
            canvas.worldCamera = cam;
        }
    }
}
